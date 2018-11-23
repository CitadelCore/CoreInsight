using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Reflection;
using CoreDynamic.Providers;
using System.Net.Http;
using CoreDRXLibrary;
using CoreDynamic.Interfaces;
using CoreDRXLibrary.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using CoreDRXLibrary.Controllers;
using log4net;
using PCLStorage;
using CoreDynamic.Utils;
using CoreDRXLibrary.Models;

namespace CoreDRXLibrary.Providers
{
    /// <summary>
    /// Class for manipulating a DRX Database.
    /// </summary>
    public class DatabaseProvider : IDatabaseProvider
    {
        private XmlDocument Database;

        private XmlNamespaceManager Namespace;

        private bool ErrorsOccured = false;

        protected IServiceProvider serviceProvider;
        protected IApplicationProvider Application;
        protected ILog Logger;

        public bool IsValid { get => IsValidInt; set { IsValidInt = value; } }
        private bool IsValidInt = false;

        public DatabaseLocation Location { get => LocationInt; set { LocationInt = value; } }
        private DatabaseLocation LocationInt;

        public event DatabaseLoadedEventHandler DatabaseLoaded;

        public virtual async Task LoadDatabase(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            Application = serviceProvider.GetRequiredService<IApplicationProvider>();
            Logger = serviceProvider.GetRequiredService<ILog>();

            //provider.logProvider.Debug("Initializing the DRX database.");

            Database = new XmlDocument()
            {
                PreserveWhitespace = true
            };

            Logger.Debug("Loading the database.");

            // Attempt to retrieve the database from CoreServer.
            try
            {
                await LoadFromCoreServer();
            }
            catch (Exception e)
            {
                Logger.Info("Couldn't load the database from CoreServer: " + e.Message + " Will try to load the database from the filesystem instead.");

                // Loading has failed.
                await LoadFromFilesystem();
            }

            Logger.Debug("Validating database XML.");
            Database.Validate(Callback);

            if (ErrorsOccured == true)
            {
                IsValid = false;
            }
            else
            {
                Logger.Debug("DRX database has passed validation.");

                Namespace = new XmlNamespaceManager(Database.NameTable);
                Namespace.AddNamespace("drx", "core://drx.schema.core/schemas/DatabaseSchema.xsd");
                IsValid = true;

                if (Location == DatabaseLocation.Filesystem || Location == DatabaseLocation.None)
                {
                    // Immediately attempt to upload to CoreServer.
                    Task.Run(async () => { await SaveDatabase(); }).Wait();
                }
            }

            // Save the database, so it has a local copy in the event that
            // the computer loses connectivity to CoreServer.
            await SaveDatabase(true);
            DatabaseLoaded?.Invoke(this, EventArgs.Empty);
        }

        protected virtual async Task SaveDatabase(bool skipCoreServer = false)
        {
            // We always attempt to save the database in BOTH the local filesystem and CoreServer.
            CoreServerRequest csr = new CoreServerRequest(null, Application.AdfsProvider, "DRXStore");

            try
            {
                if (Application.ConnectedToCoreServer)
                {
                    if (!skipCoreServer)
                    {
                        List<string> files = await csr.ListFilesAsync();

                        MemoryStream str = new MemoryStream();
                        Database.Save(str);
                        await str.FlushAsync();
                        str.Position = 0;

                        if (files.Contains("Database.xml"))
                        {
                            await csr.UpdateFileAsync("Database.xml", str);
                        }
                        else
                        {
                            await csr.CreateFileAsync("Database.xml", str);
                        }
                    }

                    // If we were able to save to CoreServer, may as well switch to it.
                    Location = DatabaseLocation.CoreServer;
                }
                else
                {
                    Location = DatabaseLocation.Filesystem;
                }
            }
            catch (Exception) { Location = DatabaseLocation.Filesystem; }

            // Save the document to the filesystem as well,
            // so it can be loaded from there when we don't have a connection to CoreServer.
            IFolder ApplicationFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("DRX", CreationCollisionOption.OpenIfExists);
            IFile DatabaseFile = await ApplicationFolder.CreateFileAsync("Database.xml", CreationCollisionOption.OpenIfExists);

            StringBuilder DatabaseString = new StringBuilder();
            XmlWriter DatabaseWriter = XmlWriter.Create(new StringWriterUTF8(DatabaseString), new XmlWriterSettings() { Encoding = System.Text.Encoding.UTF8 });
            Database.Save(DatabaseWriter);

            await DatabaseFile.WriteAllTextAsync(DatabaseString.ToString());
        }

        private async Task LoadFromCoreServer()
        {
            XmlSchema DatabaseSchema = XmlSchema.Read(new MemoryStream(Encoding.UTF8.GetBytes(schemas.DatabaseSchema)), Callback);

            if (!Application.ConnectedToCoreServer)
                throw new InvalidOperationException("TOWER network is unavailable.");

            CoreServerRequest csr = new CoreServerRequest(null, Application.AdfsProvider, "DRXStore");
            Stream downloadStream = null;

            downloadStream = await csr.DownloadFileAsync("Database.xml");

            XmlReader DatabaseReader = XmlReader.Create(downloadStream);
            Database.Schemas.Add(DatabaseSchema);
            Database.Load(DatabaseReader);
            DatabaseReader.Close();

            Logger.Debug("Loaded the database from CoreServer.");
            Location = DatabaseLocation.CoreServer;
        }

        private async Task LoadFromFilesystem()
        {
            XmlSchema DatabaseSchema = XmlSchema.Read(new MemoryStream(Encoding.UTF8.GetBytes(schemas.DatabaseSchema)), Callback);
            IFolder ApplicationFolder = await FileSystem.Current.LocalStorage.CreateFolderAsync("DRX", CreationCollisionOption.OpenIfExists);

            // Can't load the database from CoreServer.
            if (await ApplicationFolder.CheckExistsAsync("Database.xml") == ExistenceCheckResult.FileExists)
            {
                IFile DatabaseFile = await ApplicationFolder.GetFileAsync("Database.xml");

                // Open the database file for reading and writing, and feed the stream into our XmlReader.
                Stream DatabaseFileStream = await DatabaseFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite);
                XmlReader DatabaseReader = XmlReader.Create(DatabaseFileStream);

                // Load the document and the schema, and close the reader & stream.
                Database.Schemas.Add(DatabaseSchema);
                Database.Load(DatabaseReader);
                DatabaseReader.Close();
                DatabaseFileStream.Close();

                Location = DatabaseLocation.Filesystem;
                Logger.Debug("Loaded the database from the filesystem cache.");

                return;
            }

            Logger.Debug("Database not found in filesystem. Creating a new one from scratch.");

            Database.Schemas.Add(DatabaseSchema);
            XmlDeclaration W_Declaration = Database.CreateXmlDeclaration("1.0", "UTF-8", null);
            Database.AppendChild(W_Declaration);

            Namespace = new XmlNamespaceManager(Database.NameTable);
            Namespace.AddNamespace("drx", "core://drx.schema.core/schemas/DatabaseSchema.xsd");

            XmlElement W_RootElement = Database.CreateElement("database", "core://drx.schema.core/schemas/DatabaseSchema.xsd");
            W_RootElement.SetAttribute("revision", "1");

            XmlElement W_PayloadElement = Database.CreateElement("payload", "core://drx.schema.core/schemas/DatabaseSchema.xsd");
            XmlElement W_DRXBaseElement = Database.CreateElement("drxbase", "core://drx.schema.core/schemas/DatabaseSchema.xsd");

            for (int i = 0; i < 10; i++)
            {
                XmlElement seriesTemp = Database.CreateElement("series", "core://drx.schema.core/schemas/DatabaseSchema.xsd");
                seriesTemp.SetAttribute("number", Convert.ToString(i));
                SaveNewNode(W_DRXBaseElement, seriesTemp);
            }

            SaveNewNode(W_DRXBaseElement, Database.CreateElement("flags", "core://drx.schema.core/schemas/DatabaseSchema.xsd"));

            SaveNewNode(W_PayloadElement, W_DRXBaseElement);
            SaveNewNode(W_RootElement, W_PayloadElement);
            Database.AppendChild(W_RootElement);

            // Set the location to None - the location will be set to either CoreServer or Filesystem when the database is next saved.
            Location = DatabaseLocation.None;

            Logger.Debug("Successfully created and initialized a new database.");
            await SaveDatabase();
        }

        public virtual bool GetDRXExists(int id)
        {
            string SeriesString = id.ToString().ToCharArray()[0].ToString();
            XmlNode DRXNode = this.XmlQuery("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + SeriesString + "']/drx:file[@id = '" + id.ToString() + "']");

            string QS = "//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + SeriesString + "']/drx:file[id = '" + id.ToString() + "']";

            if (DRXNode != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public virtual string GetDRXFilePath(int id)
        {
            XmlNodeList Series = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + Convert.ToString(id.ToString().ToCharArray()[0]) + "']/drx:file", this.Namespace);
            foreach (XmlNode DRX in Series)
            {
                if (DRX.Attributes["id"].InnerText == id.ToString())
                {
                    string fileName = DRX.Attributes["filename"].InnerText;
                    if (File.Exists(fileName))
                      return fileName;

                    Logger.Warn("The file path of DRX " + id.ToString() + " is invalid. Defaulting to the local appdata location. This is a serious problem!");
                    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\DRX\Payload\DRX_" + id.ToString() + ".drx";
                }
            }

            return null;
        }

        public static int GetDRXIDFromFilename(string filename)
        {
            return Convert.ToInt32(Path.GetFileNameWithoutExtension(filename).Replace("DRX_", String.Empty));
        }

        public virtual void CreateDRXEntry(int id, string filename, string type, string title)
        {
            string Series = Convert.ToString(id.ToString().ToCharArray()[0]);
            XmlNode SeriesNode = this.XmlQuery("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + Series + "']");

            XmlElement NewDRX = Database.CreateElement("file", "core://drx.schema.core/schemas/DatabaseSchema.xsd");
            NewDRX.SetAttribute("id", id.ToString());
            NewDRX.SetAttribute("filename", filename);
            NewDRX.SetAttribute("type", type);
            NewDRX.SetAttribute("title", title);

            this.SaveNewNode(SeriesNode, NewDRX);
        }

        public virtual void DeleteDRXEntry(int id)
        {
            string Series = Convert.ToString(id.ToString().ToCharArray()[0]);
            XmlNode SeriesNode = this.XmlQuery("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + Series + "']");

            XmlNode DRXNode = this.XmlQuery("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + Series + "']/drx:file[@id = '" + id.ToString() + "']");

            if (DRXNode != null)
            {
                SeriesNode.RemoveChild(DRXNode);
            }
        }

        /// <summary>
        /// Function for returning the next free ID of the DRX in a specific series.
        /// </summary>
        /// <returns>The ID of the next free DRX.</returns>
        public virtual int GetNextID(int Series)
        {
            int numID = 0;

            string SeriesString = Series.ToString().ToCharArray()[0].ToString();

            XmlNodeList ThisSeries = this.Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + SeriesString + "']/drx:file", this.Namespace);
            if (ThisSeries != null)
            {
                foreach (XmlNode DRX in ThisSeries)
                {
                    numID++;
                }

                numID++;

                if (numID > Series)
                    throw new Exception("The series is full!");
                return Series + numID;
            }
            else
            {
                return 0;
            }
        }

        public virtual IList<int> GetAllSeriesNumbers()
        {
            XmlNodeList SeriesList = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:series", this.Namespace);
            IList<int> SeriesNumbers = new List<int>();

            foreach (XmlElement Series in SeriesList)
            {
                SeriesNumbers.Add(Convert.ToInt32(Series.GetAttribute("number")));
            }

            return SeriesNumbers;
        }

        /// <summary>
        /// Callback for the XML validation error/warning event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="a">The passed event parameters.</param>
        private void Callback(object sender, ValidationEventArgs a)
        {
            if (a.Severity == XmlSeverityType.Warning)
            {
                Logger.Warn("DRX database schema warning: " + a.Message);
            }
            else if (a.Severity == XmlSeverityType.Error)
            {
                Logger.Warn("DRX database schema critical load error: " + a.Message);
                throw new Exception(a.Message);
            }
        }

        /// <summary>
        /// Apply some nice formatting to the node. It's not the best, but at least everything's not on one line...
        /// </summary>
        /// <param name="ParentNode">The node that is the direct parent of the child node.</param>
        /// <param name="ChildNode">The node you'd like to save.</param>
        private void SaveNewNode(XmlNode ParentNode, XmlElement ChildNode)
        {
            ParentNode.AppendChild(ChildNode);
            ParentNode.InnerXml = ParentNode.InnerXml.Replace(ChildNode.OuterXml, ChildNode.OuterXml + Environment.NewLine);
        }

        /// <summary>
        /// Helper method for saving the database with nice formatting.
        /// </summary>
        /// <param name="DatabasePath">The file path to the database XML to save.</param>
        public virtual async Task UpdateAndSaveDatabase()
        {
            XmlElement rootElement = XmlQuery("//drx:database") as XmlElement;
            rootElement.SetAttribute("revision", Convert.ToString(Convert.ToInt32(rootElement.GetAttribute("revision")) + 1));

            await SaveDatabase();
        }

        /// <summary>
        /// Method for querying the database with XPath.
        /// </summary>
        /// <param name="query">The XPath query string you'd like to use.</param>
        /// <returns>An XmlNode object with the query result.</returns>
        private XmlNode XmlQuery(string query)
        {
            return this.Database.DocumentElement.SelectSingleNode(query, this.Namespace);
        }

        public virtual async Task<FileEditorProvider> OpenDRXFromIDAsync(int id, FileLoadingStatusCallback callback = null)
        {
            //string filename = GetDRXFilePath(id);

            //if (filename != null)
            //{
            try
            {
                RemoteDRXController Controller = new RemoteDRXController(serviceProvider);
                IFileProvider file = await Controller.GetFile(id);

                return new FileEditorProvider(serviceProvider, file);

                // Id sanity check
                //if (id != file.Id) throw new Exception("Document does not match downloaded document ID! Requested ID was " + id.ToString() + " while the ID retrieved was " + file.Id.ToString() + "!");
            }
            catch (Exception e)
            {
                await callback(e.Message);
            }

            return null;
                
            //}

            //throw new Exception("DRX filename was null.");
        }

        /// <summary>
        /// Queries the DRX database and returns a dictionary with DRX names and ID codes.
        /// </summary>
        /// <returns>Dictionary with DRX names and ID codes.</returns>
        public virtual Dictionary<int, string> GetDRXTitles()
        {
            Dictionary<int, string> MasterDict = new Dictionary<int, string>();

            XmlNodeList DRXList = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:series/drx:file", this.Namespace);
            foreach (XmlNode DRX in DRXList)
            {
                MasterDict.Add(Convert.ToInt32(DRX.Attributes["id"].Value), DRX.Attributes["title"].Value);
            }

            return MasterDict;
        }

        public virtual string GetDRXBodyHash(int id)
        {
            XmlNodeList Series = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + Convert.ToString(id.ToString().ToCharArray()[0]) + "']/drx:file", Namespace);
            foreach (XmlNode DRX in Series)
            {
                if (DRX.Attributes["id"].InnerText == id.ToString())
                {
                    if (DRX.Attributes["hash"] != null)
                    {
                        return DRX.Attributes["hash"].InnerText;
                    }
                }
            }

            return null;
        }

        public virtual void SetDRXBodyHash(int id, string hash)
        {
            XmlNodeList Series = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:series[@number = '" + Convert.ToString(id.ToString().ToCharArray()[0]) + "']/drx:file", Namespace);
            foreach (XmlElement DRX in Series)
            {
                if (DRX.Attributes["id"].InnerText == id.ToString())
                {
                    DRX.SetAttribute("hash", hash);
                }
            }
        }

        public IList<DocumentFlag> GetAllFlags()
        {
            XmlNodeList Flags = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:flags/drx:flag", Namespace);
            IList<DocumentFlag> flagList = new List<DocumentFlag>();

            foreach (XmlElement flag in Flags)
            {
                DocumentFlag tempFlag = new DocumentFlag()
                {
                    FlagId = flag.GetAttribute("flagId"),
                    Description = flag.GetAttribute("description")
                };

                if (flag.HasAttribute("minSecLevel"))
                    tempFlag.SecurityLevel = Classification.ParseFromId(flag.GetAttribute("minSecLevel"));

                if (flag.HasAttribute("colour"))
                    tempFlag.FlagColour = Xamarin.Forms.Color.FromHex(flag.GetAttribute("colour"));

                flagList.Add(tempFlag);
            }

            return flagList;
        }

        public DocumentFlag GetFlagFromId(string id)
        {
            XmlNodeList Flags = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:flags/drx:flag[@flagId = '" + id + "']", Namespace);

            if (Flags.Count == 1)
            {
                XmlElement flag = Flags[0] as XmlElement;

                DocumentFlag tempFlag = new DocumentFlag()
                {
                    FlagId = flag.GetAttribute("flagId"),
                    Description = flag.GetAttribute("description")
                };

                if (flag.HasAttribute("minSecLevel"))
                    tempFlag.SecurityLevel = Classification.ParseFromId(flag.GetAttribute("minSecLevel"));

                if (flag.HasAttribute("colour"))
                    tempFlag.FlagColour = Xamarin.Forms.Color.FromHex(flag.GetAttribute("colour"));

                return tempFlag;
            }
            else
            {
                return null;
            }
        }

        public void SaveFlag(DocumentFlag flag)
        {
            bool CreatingNode = false;

            XmlNode FlagsNode = Database.SelectSingleNode("//drx:database/drx:payload/drx:drxbase/drx:flags", Namespace);
            
            if (FlagsNode == null)
            {
                FlagsNode = Database.CreateElement("flags");
                CreatingNode = true;
            } 

            XmlNodeList Flags = Database.SelectNodes("//drx:database/drx:payload/drx:drxbase/drx:flags/drx:flag[@flagId = '" + flag.FlagId + "']", Namespace);
            XmlElement xmlFlag;

            if (Flags.Count == 1)
            {
                xmlFlag = Flags[0] as XmlElement;
            }
            else
            {
                xmlFlag = Database.CreateElement("flag");
            }
            
            xmlFlag.SetAttribute("flagId", flag.FlagId);
            xmlFlag.SetAttribute("description", flag.Description);

            xmlFlag.SetAttribute("minSecLevel", flag.SecurityLevel.ClassificationId);
            xmlFlag.SetAttribute("colour", String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", (int)(flag.FlagColour.A * 255), (int)(flag.FlagColour.R * 255), (int)(flag.FlagColour.G * 255), (int)(flag.FlagColour.B * 255)));

            if (Flags.Count < 1)
                SaveNewNode(FlagsNode, xmlFlag);

            if (CreatingNode)
            {
                XmlNode BaseNode = Database.SelectSingleNode("//drx:database/drx:payload/drx:drxbase", Namespace);
                SaveNewNode(BaseNode, (XmlElement) FlagsNode);
            }
                
        }
    }
}
