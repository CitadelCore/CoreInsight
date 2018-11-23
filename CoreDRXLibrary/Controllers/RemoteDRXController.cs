using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Providers;
using CoreDynamic.Interfaces;
using CoreDynamic.Providers;
using CoreDynamic.Utils;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PCLStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CoreDRXLibrary.Controllers
{
    public class RemoteDRXController
    {
        IServiceProvider serviceProvider;
        IApplicationProvider ApplicationProvider;

        CoreServerRequest FileRequest;
        List<string> RemoteFiles;

        public RemoteDRXController(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            ApplicationProvider = serviceProvider.GetRequiredService<IApplicationProvider>();

            FileRequest = new CoreServerRequest(null, ApplicationProvider.AdfsProvider, "DRXStore");
        }
        public async Task<IFileProvider> GetFile(int id, FileLoadingStatusCallback callback = null)
        {
            IFileProvider file = null;

            if (ApplicationProvider.ConnectedToCoreServer)
            {
                try
                {
                    await RefreshRemoteDRXList();

                    file = await GetFileFromServer(id);
                } catch (Exception) {
                    if (callback != null)
                        await callback("The application was started with a connection to CoreServer but the document selected could not be retrieved. Perhaps connection has been lost, or the document does not exist on the server?");
                } 
            }
            
            if (file != null)
            {
                return file;
            }
            else
            {
                IDatabaseProvider databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
                IFolder cacheFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("DRX", CreationCollisionOption.OpenIfExists);

                if (await cacheFolder.CheckExistsAsync("DRX_" + id.ToString() + ".drx") != ExistenceCheckResult.FileExists)
                    throw new Exception("The document could not be loaded from CoreServer, and is NOT in the local cache. Cannot load it!");

                IFile documentFile = await cacheFolder.GetFileAsync("DRX_" + id.ToString() + ".drx");

                return new FileProvider(serviceProvider, documentFile);
            }
        }

        /// <summary>
        /// Saves a file to both the server and the filesystem.
        /// </summary>
        /// <param name="provider">The application provider.</param>
        /// <param name="doc">The core XmlDocument.</param>
        /// <param name="id">The DRX ID.</param>
        /// <param name="filePath">The (optional) file path to use.</param>
        /// <returns></returns>
        public async Task SaveFile(FileProvider file, int id, bool skipCoreServer = false)
        {
            string fileName = "DRX_" + Convert.ToString(id) + ".drx";

            // Deal with uploading the file to CoreServer.
            if (!skipCoreServer && ApplicationProvider.ConnectedToCoreServer)
            {
                try
                {
                    CoreServerRequest csr = new CoreServerRequest(null, ApplicationProvider.AdfsProvider, "DRXStore");
                    List<string> files = await csr.ListFilesAsync();

                    MemoryStream str = new MemoryStream();
                    file.activeDocument.PreserveWhitespace = true;
                    file.activeDocument.Save(str);
                    await str.FlushAsync();
                    str.Position = 0;

                    if (files.Contains(fileName))
                    {
                        await csr.UpdateFileAsync(fileName, str);
                    }
                    else
                    {
                        await csr.CreateFileAsync(fileName, str);
                    }
                }
                catch (Exception) { }
            }

            // Save the document to the filesystem as well,
            // so it can be loaded from there when we don't have a connection to CoreServer.
            IFolder ApplicationFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("DRX", CreationCollisionOption.OpenIfExists);
            IFile DocumentFile = await ApplicationFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);

            StringBuilder DocumentString = new StringBuilder();
            XmlWriter DatabaseWriter = XmlWriter.Create(new StringWriterUTF8(DocumentString), new XmlWriterSettings() { Encoding = Encoding.UTF8 });
            file.activeDocument.Save(DatabaseWriter);

            await DocumentFile.WriteAllTextAsync(DocumentString.ToString());
        }

        /// <summary>
        /// Deletes a document from the server.
        /// </summary>
        /// <param name="file">File provider instance.</param>
        /// <returns>Task result.</returns>
        public async Task DeleteFile(FileProvider file)
        {
            await DeleteFile(file.Id);
        }

        /// <summary>
        /// Deletes a document from the server.
        /// </summary>
        /// <param name="id">ID of the document.</param>
        /// <returns>Task result.</returns>
        public async Task DeleteFile(int id)
        {
            string fileName = "DRX_" + Convert.ToString(id) + ".drx";

            if (!ApplicationProvider.ConnectedToCoreServer)
                throw new Exception("Cannot connect to the CoreServer repository to delete the document on the server.");

            try
            {
                CoreServerRequest csr = new CoreServerRequest(null, ApplicationProvider.AdfsProvider, "DRXStore");
                await csr.DeleteFileAsync(fileName);
            }
            catch (Exception e) { throw new Exception("An exception occurred while contacting CoreServer to delete the serverside copy. Exception: " + e.Message, e); }

            // Delete the local copy after it has been deleted from the server
            IFolder ApplicationFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("DRX", CreationCollisionOption.OpenIfExists);
            IFile DocumentFile = await ApplicationFolder.GetFileAsync(fileName);
            await DocumentFile.DeleteAsync();
        }

        public async Task RefreshRemoteDRXList()
        {
            RemoteFiles = await FileRequest.ListFilesAsync();
        }

        /// <summary>
        /// Retrieves a DRX from the remote server.
        /// </summary>
        /// <param name="drxId">ID of the remote DRX to retrieve.</param>
        /// <returns>A IFileProvider representing the editable DRX. Returns null if the server could not be contacted, the file dosen't exist, or the list hasn't been populated.</returns>
        public async Task<IFileProvider> GetFileFromServer(int drxId)
        {
            if (RemoteFiles != null)
            {
                string drxFullName = "DRX_" + Convert.ToString(drxId) + ".drx";

                Stream responseStream = await FileRequest.DownloadFileAsync(drxFullName);

                return new FileProvider(serviceProvider, responseStream);
            }

            return null;
        }

        /// <summary>
        /// Retrieves metadata in bulk for the specified DRX files.
        /// This is so that we don't need to download every single DRX just to get metadata.
        /// </summary>
        /// <param name="files">List of DRX IDs to query.</param>
        /// <returns>A Dictionary containing IDs and the metadata.</returns>
        public async Task<IDictionary<int, IDictionary<string, dynamic>>> GetMetadata(ICollection<int> files)
        {
            CoreServerRequest csr = new CoreServerRequest("DRXFileMetadata", ApplicationProvider.AdfsProvider);

            IDictionary<int, IDictionary<string, dynamic>> metadata = JsonConvert.DeserializeObject<IDictionary<int, IDictionary<string, dynamic>>>(await csr.SendRequestAsync(JsonConvert.SerializeObject(files)));

            IDictionary<int, IDictionary<string, dynamic>> filteredMetadata = new Dictionary<int, IDictionary<string, dynamic>>(metadata);

            foreach (KeyValuePair<int, IDictionary<string, dynamic>> entry in metadata)
            {
                if (entry.Value == null)
                {
                    try
                    {
                        IDatabaseProvider databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();
                        IFolder cacheFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("DRX", CreationCollisionOption.OpenIfExists);

                        if (await cacheFolder.CheckExistsAsync("DRX_" + entry.Key.ToString() + ".drx") != ExistenceCheckResult.FileExists)
                            throw new Exception("Cannot find the DRX required in the filesystem.");

                        IFile documentFile = await cacheFolder.GetFileAsync("DRX_" + entry.Key.ToString() + ".drx");
                        IFileProvider fp = new FileProvider(serviceProvider, documentFile);

                        filteredMetadata.Remove(entry.Key);
                        filteredMetadata.Add(entry.Key, fp.GetMetadata());
                        fp.Dispose();
                    }
                    catch (Exception)
                    {
                        // no-op
                    }
                }
            }

            return filteredMetadata;
        }
    }
}
