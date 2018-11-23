using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CoreServer.Services
{
    public class FileStorageService
    {
        private static Regex invalidPathCharacters = new Regex("[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]");
        private static Regex invalidFileNameCharacters = new Regex("[" + Regex.Escape(new string(Path.GetInvalidFileNameChars())) + "]");
        private string storageNamespace;

        /// <summary>
        /// Class which handles saving files to the server data store.
        /// </summary>
        /// <param name="storageNamespace">The server base storage namespace.</param>
        /// <param name="userIdentifier">The user identifier to store user-specific files.</param>
        public FileStorageService(string userIdentifier, string storageNamespace = "MiscFiles")
        {
            this.storageNamespace = Path.Combine(HttpContext.Current.Server.MapPath("~/DataStore"), userIdentifier, storageNamespace);

            if (invalidPathCharacters.IsMatch(this.storageNamespace))
                throw new InvalidDataException("The filename contains invalid characters.");

            if (!Directory.Exists(this.storageNamespace))
                Directory.CreateDirectory(this.storageNamespace);
        }

        /// <summary>
        /// Saves a posted file to the specified file path.
        /// </summary>
        /// <param name="fileStream">The file stream to get data from.</param>
        /// <param name="fileName">The file name.</param>
        public async Task SaveFileAsync(Stream fileStream, string fileName)
        {
            string shortFileName = Path.GetFileName(fileName);

            if (invalidFileNameCharacters.IsMatch(shortFileName))
                throw new InvalidDataException("The filename contains invalid characters.");

            if (File.Exists(Path.Combine(storageNamespace, shortFileName)))
                throw new Exception("The file already exists.");

            string filePath = Path.Combine(storageNamespace, shortFileName);
            using (FileStream writeFileStream = File.Create(filePath))
            {
                await fileStream.CopyToAsync(writeFileStream);
            }

            fileStream.Close();
        }

        /// <summary>
        /// Updates an existing file with new changes.
        /// </summary>
        /// <param name="fileStream">The file stream to get data from.</param>
        /// <param name="fileName">The file name.</param>
        /// <returns>A Task instance.</returns>
        public async Task UpdateFileAsync(Stream fileStream, string fileName)
        {
            string shortFileName = Path.GetFileName(fileName);
            string filePath = Path.Combine(storageNamespace, shortFileName);

            if (!File.Exists(filePath))
                throw new Exception("The file does not exist.");

            File.Delete(filePath);

            await SaveFileAsync(fileStream, fileName);
        }

        /// <summary>
        /// Retrieves a FileStreamResult from a file path.
        /// </summary>
        /// <param name="fileName">The path of the file relative to the server storage directory.</param>
        /// <returns>The FileStreamResult containing the file, or null if an error occured or the file dosen't exist.</returns>
        public FileStream RetrieveFile(string fileName)
        {
            if (invalidFileNameCharacters.IsMatch(fileName))
                throw new InvalidDataException("The filename contains invalid characters.");

            string filePath = Path.Combine(storageNamespace, Path.GetFileName(fileName));
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The file could not be found in the namespace.");
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        /// <summary>
        /// Deletes a file.
        /// </summary>
        /// <param name="fileName">The path of the file relative to the server storage directory.</param>
        public void DeleteFile(string fileName)
        {
            if (invalidFileNameCharacters.IsMatch(fileName))
                throw new InvalidDataException("The filename contains invalid characters.");

            string filePath = Path.Combine(storageNamespace, Path.GetFileName(fileName));

            File.Delete(filePath);
        }

        /// <summary>
        /// Returns a list of files.
        /// </summary>
        /// <returns>A list of files.</returns>
        public List<string> ListFiles()
        {
            IEnumerable<string> FileListObject = Directory.EnumerateFiles(storageNamespace);

            List<string> FileList = new List<string>();
            foreach (string entry in FileListObject)
            {
                FileList.Add(Path.GetFileName(entry));
            }

            return FileList;
        }
    }
}