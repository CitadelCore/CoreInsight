using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using CoreDynamic.Providers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Drawing;
using CoreDRXLibrary;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using CoreDynamic.Interfaces;
using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Controllers;
using PCLStorage;
using CoreDRXLibrary.Models;

namespace CoreDRXLibrary.Providers
{
    public class FileProvider : IFileProvider
    {
        /// <summary>
        /// The XmlDocument to modify.
        /// </summary>
        public XmlDocument activeDocument;

        /// <summary>
        /// Whether errors have occured while loading the file.
        /// </summary>
        private bool errorsOccured = false;

        /// <summary>
        /// The reader that stores the DRX xml document.
        /// </summary>
        private XmlReader fileReader;
        

        /// <summary>
        /// The decrypted decryption key that decrypts the DRX body (unwrapped).
        /// </summary>
        protected byte[] encryptionDecryptedKey;

        /// <summary>
        /// The body of the DRX, encoded in RTF format.
        /// </summary>
        protected string bodyText;

        /// <summary>
        /// The XmlNamespaceManager holding namespace information.
        /// </summary>
        private XmlNamespaceManager activeNamespace;

        public string CurrentFilePath { get => CurrentFilePathInt; set { CurrentFilePathInt = value; } }
        private string CurrentFilePathInt;
        public int CreationID { get => CreationIDInt; set { CreationIDInt = value; } }
        private int CreationIDInt;
        public bool IsValid { get => IsValidInt; set { IsValidInt = value; } }
        private bool IsValidInt = false;
        public int Id { get => IdInt; set { IdInt = value; } }
        private int IdInt;
        public double FileRevision { get => FileRevisionInt; set { FileRevisionInt = value; } }
        private double FileRevisionInt;
        public int Series { get => SeriesInt; set { SeriesInt = value; } }
        private int SeriesInt = 200;
        public bool EncryptionEnabled { get => EncryptionEnabledInt; set { EncryptionEnabledInt = value; } }
        private bool EncryptionEnabledInt;
        public double EncryptionVersion { get => EncryptionVersionInt; set { EncryptionVersionInt = value; } }
        private double EncryptionVersionInt;
        public string BodyHash { get => BodyHashInt; set { BodyHashInt = value; } }
        private string BodyHashInt;
        public EncryptionType FileEncryptionType { get => FileEncryptionTypeInt; set { FileEncryptionTypeInt = value; } }
        private EncryptionType FileEncryptionTypeInt;
        public string EncryptionSerial { get => EncryptionSerialInt; set { EncryptionSerialInt = value; } }
        private string EncryptionSerialInt;
        public X509Certificate2 EncryptionCertificate { get => EncryptionCertificateInt; set { EncryptionCertificateInt = value; } }
        private X509Certificate2 EncryptionCertificateInt;
        public string EncryptionCertificateKey { get => EncryptionCertificateKeyInt; set { EncryptionCertificateKeyInt = value; } }
        private string EncryptionCertificateKeyInt;
        public string EncryptionKey { get => EncryptionKeyInt; set { EncryptionKeyInt = value; } }
        private string EncryptionKeyInt;
        public bool EncryptionProvisioning { get => EncryptionProvisioningInt; set { EncryptionProvisioningInt = value; } }
        private bool EncryptionProvisioningInt;
        public string EncryptionPassOverride { get => EncryptionPassOverrideInt; set { EncryptionPassOverrideInt = value; } }
        private string EncryptionPassOverrideInt;
        public bool HasBeenDecrypted { get => HasBeenDecryptedInt; set { HasBeenDecryptedInt = value; } }
        private bool HasBeenDecryptedInt;
        public string FriendlyName { get => FriendlyNameInt; set { FriendlyNameInt = value; } }
        private string FriendlyNameInt = String.Empty;
        public Classification SecurityLevel { get => SecurityLevelInt; set { SecurityLevelInt = value; } }
        private Classification SecurityLevelInt = Classification.Public;
        public bool Redactions { get => RedactionsInt; set { RedactionsInt = value; } }
        private bool RedactionsInt;
        public List<DocumentFlag> Flags { get => FlagsInt; set { FlagsInt = value; } }
        private List<DocumentFlag> FlagsInt = new List<DocumentFlag>();
        public string Setting { get => SettingInt; set { SettingInt = value; } }
        private string SettingInt;
        public string Date { get => DateInt; set { DateInt = value; } }
        private string DateInt;
        public string Status { get => StatusInt; set { StatusInt = value; } }
        private string StatusInt;
        public int Vividity { get => VividityInt; set { VividityInt = value; } }
        private int VividityInt = -1;
        public int Remembrance { get => RemembranceInt; set { RemembranceInt = value; } }
        private int RemembranceInt = -1;
        public int Emotion { get => EmotionInt; set { EmotionInt = value; } }
        private int EmotionInt = -1;
        public int Length { get => LengthInt; set { LengthInt = value; } }
        private int LengthInt = -1;

        public string EncryptionPassword { get; set; }

        private IServiceProvider serviceProvider;
        private IDatabaseProvider databaseProvider;
        private IApplicationProvider applicationProvider;

        private Stream documentFileStream = null;

        /// <summary>
        /// Class constructor for the DRXFile class.
        /// </summary>
        /// <param name="serviceProvider">Service provider to use for DI.</param>
        /// <param name="documentFile">File instance in the local filesystem to load from.</param>
        public FileProvider(IServiceProvider serviceProvider, IFile documentFile)
        {
            Initialize(serviceProvider);

            Task.Run(async () =>
            {
                await LoadFromFile(documentFile);
            }).Wait();
        }

        public FileProvider(IServiceProvider serviceProvider)
        {
            Initialize(serviceProvider);
        }

        public FileProvider(Stream fileStream)
        {
            documentFileStream = fileStream;
            fileReader = XmlReader.Create(documentFileStream);

            Initialize();
        }

        public async Task LoadFromFile(IFile documentFile)
        {
            // Open the database file for reading and writing, and feed the stream into our XmlReader.
            documentFileStream = await documentFile.OpenAsync(PCLStorage.FileAccess.ReadAndWrite);
            fileReader = XmlReader.Create(documentFileStream);
        }

        /// <summary>
        /// Class constructor for the DRXFile class.
        /// </summary>
        /// <param name="serviceProvider">Service collection to use for DI.</param>
        /// <param name="fileStream">A file stream containing the file.</param>
        public FileProvider(IServiceProvider serviceProvider, Stream fileStream)
        {
            documentFileStream = fileStream;
            fileReader = XmlReader.Create(documentFileStream);

            Initialize(serviceProvider);
        }

        private void Initialize(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;

            applicationProvider = serviceProvider.GetRequiredService<IApplicationProvider>();
            databaseProvider = serviceProvider.GetRequiredService<IDatabaseProvider>();

            Initialize();

            // Process document initialization from scratch
            if (fileReader == null)
            {
                Id = databaseProvider.GetNextID(200);
                FriendlyName = "Unnamed DRX";
                bodyText = String.Empty;
                Setting = String.Empty;
                FileRevision = 1;
                Date = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                Status = "default";
                IsValid = true;
            }
        }

        private void Initialize()
        {
            // Create a new XmlDocument to hold XML information, and set options on it.
            activeDocument = new XmlDocument()
            {
                PreserveWhitespace = true
            };

            // Attempt to load the DRX schema.
            try
            {
                // Load the schema.
                activeDocument.Schemas.Add(XmlSchema.Read(new MemoryStream(Encoding.UTF8.GetBytes(schemas.DRXSchema)), ValidationCallback));

                // Create a new namespace manager for the schema.
                activeNamespace = new XmlNamespaceManager(activeDocument.NameTable);

                // Add the schema namespace to the namespace manager.
                activeNamespace.AddNamespace("xns", "core://drx.schema.core/schemas/DRXSchema.xsd");
            }

            // Something went wrong while loading the schema.
            catch (Exception e)
            {
                throw new DRXLoadException(e.Message, e);
            }
        }

        public Dictionary<string, dynamic> GetMetadata()
        {
            try
            {
                if (fileReader.ReadState == ReadState.Initial)
                    activeDocument.Load(fileReader);

                activeDocument.Validate(ValidationCallback);

                string friendlyName = QuerySingleNode("//xns:drx/xns:header/xns:friendlyName").InnerText;
                string securityLevel = QuerySingleNode("//xns:drx/xns:header/xns:secLevel").InnerText;
                string redactions = QuerySingleNode("//xns:drx/xns:header/xns:redactions").InnerText;
                string setting = QuerySingleNode("//xns:drx/xns:header/xns:setting").InnerText;
                string date = QuerySingleNode("//xns:drx/xns:header/xns:date").InnerText;
                string status = QuerySingleNode("//xns:drx/xns:header/xns:status").InnerText;

                XmlNode bodyHash_node = activeDocument.DocumentElement.SelectSingleNode("//xns:drx/@bodyHash", activeNamespace);
                XmlNode encryptionWrapper_node = activeDocument.DocumentElement.SelectSingleNode("//xns:drx/@encryptionWrapper", activeNamespace);
                XmlNode vividity_node = QuerySingleNode("//xns:drx/@vividity");
                XmlNode remembrance_node = QuerySingleNode("//xns:drx/@remembrance");
                XmlNode emotion_node = QuerySingleNode("//xns:drx/@emotion");
                XmlNode length_node = QuerySingleNode("//xns:drx/@length");

                int vividity = -1;
                int remembrance = -1;
                int emotion = -1;
                int length = -1;

                string bodyHash = null;
                if (bodyHash_node != null) { bodyHash = bodyHash_node.InnerText; } else if (encryptionWrapper_node != null) { bodyHash = encryptionWrapper_node.InnerText; }

                if (vividity_node != null) { vividity = Convert.ToInt32(vividity_node.InnerText); }
                if (remembrance_node != null) { remembrance = Convert.ToInt32(remembrance_node.InnerText); }
                if (emotion_node != null) { emotion = Convert.ToInt32(emotion_node.InnerText); }
                if (length_node != null) { length = Convert.ToInt32(length_node.InnerText); }

                XmlNodeList documentFlags = activeDocument.SelectNodes("//xns:drx/xns:header/xns:flags/xns:flag", activeNamespace);
                List<string> temporaryFlags = new List<string>();
                List<string> nonDescriptiveFlags = new List<string>();

                foreach (XmlNode DataFlag in documentFlags)
                {
                    temporaryFlags.Add(DataFlag.InnerText);
                    nonDescriptiveFlags.Add(ParserUtilities.FlagToNonDescriptive(DataFlag.InnerText));
                }

                Dictionary<string, dynamic> metadata = new Dictionary<string, dynamic>()
                {
                    { "Identifier", Convert.ToInt32(QuerySingleNode("//xns:drx/@id").InnerText) },
                    { "FriendlyName", friendlyName },
                    { "SecurityLevel", securityLevel },
                    { "Redactions", redactions },
                    { "Flags", temporaryFlags },
                    { "FlagsNonDescriptive", nonDescriptiveFlags },
                    { "Setting", setting },
                    { "Date", date },
                    { "Status", status },
                    { "BodyHash", bodyHash },
                    { "VRELScore", ParserUtilities.VRELToString(vividity, remembrance, emotion, length) },
                    { "RetrievalLocation", "DirectFile" }
                };

                return metadata;
            }
            catch (Exception e)
            {
                throw new DRXLoadException("Fatal error: " + e.Message + " Could not query file metadata.", e);
            }
            
        }

        /// <summary>
        /// Attempts to load the specified DRX document.
        /// </summary>
        /// <returns>A boolean value indicating whether the load was successful.</returns>
        public bool LoadDocument()
        {
            if (fileReader == null)
                throw new DRXLoadException("File provider does not yet contain a reader or document to read from.");

            // Attempt to read the DRX.
            try
            {
                #region ReaderInitialization

                try
                {
                    // Load the data into the XmlDocument using the XmlReader.
                    if (fileReader.ReadState == ReadState.Initial && documentFileStream != null)
                    {
                        //string testString = fileReader.ReadContentAsString();

                        activeDocument.Load(fileReader);
                        documentFileStream.Close();
                    }
                }
                catch (XmlException e)
                {
                    throw new DRXLoadException("DRX XML parsing exception: " + e.Message, e);
                }

                // Close the stream to make sure the file is not still open.
                fileReader.Close();

                // Check the validity.
                activeDocument.Validate(ValidationCallback);
                if (errorsOccured == true)
                {
                    // Errors occured!
                    return false;
                }

                #endregion

                #region VariableAssignment
                // Query all namespaces and assign to variables.

                XmlElement rootElement = (XmlElement)QuerySingleNode("//xns:drx");
                XmlNode header = QuerySingleNode("//xns:drx/xns:header");
                XmlNode friendlyName = QuerySingleNode("//xns:drx/xns:header/xns:friendlyName");
                XmlNode securityLevel = QuerySingleNode("//xns:drx/xns:header/xns:secLevel");
                XmlNode redactions = QuerySingleNode("//xns:drx/xns:header/xns:redactions");
                XmlNode flags = QuerySingleNode("//xns:drx/xns:header/xns:flags");
                XmlNode setting = QuerySingleNode("//xns:drx/xns:header/xns:setting");
                XmlNode date = QuerySingleNode("//xns:drx/xns:header/xns:date");
                XmlNode status = QuerySingleNode("//xns:drx/xns:header/xns:status");

                FriendlyName = friendlyName.InnerText;
                SecurityLevel = Classification.ParseFromId(securityLevel.InnerText);
                Redactions = Convert.ToBoolean(redactions.InnerText);

                // Get a list of all flags in the current DRX document.
                XmlNodeList documentFlags = activeDocument.SelectNodes("//xns:drx/xns:header/xns:flags/xns:flag", activeNamespace);

                // Loop for each XmlNode in the DataFlags XmlNodeList.
                foreach (XmlNode flag in documentFlags)
                {
                    DocumentFlag tempFlag = databaseProvider.GetFlagFromId(flag.InnerText);
                    if (tempFlag == null)
                    {
                        tempFlag = new DocumentFlag() { FlagId = flag.InnerText, Description = "Unknown Flag" };
                        databaseProvider.SaveFlag(tempFlag);
                    }


                    // Add the flag to the FlagsTemp list.
                    Flags.Add(tempFlag);
                }

                Setting = setting.InnerText;
                Date = date.InnerText;
                Status = status.InnerText;

                // Query other attributes.
                XmlNode vividity = this.QuerySingleNode("//xns:drx/@vividity");
                XmlNode remembrance = this.QuerySingleNode("//xns:drx/@remembrance");
                XmlNode emotion = this.QuerySingleNode("//xns:drx/@emotion");
                XmlNode length = this.QuerySingleNode("//xns:drx/@length");

                // Check if the document already has "vividity" and "remembrance" attributes.
                if (vividity == null || remembrance == null)
                {
                    // Set values to zero.
                    Vividity = 0;
                    Remembrance = 0;

                    // Initialize the element.
                    rootElement.SetAttribute("vividity", "0");
                    rootElement.SetAttribute("remembrance", "0");
                }
                else
                {
                    // Set them from existing values.
                    Vividity = Convert.ToInt32(vividity.InnerText);
                    Remembrance = Convert.ToInt32(remembrance.InnerText);
                }

                // Check if the document already has "emotion" and "length" attributes.
                if (emotion == null || length == null)
                {
                    // Set values to zero.
                    Emotion = 0;
                    Length = 0;

                    // Initialize the element.
                    rootElement.SetAttribute("emotion", "0");
                    rootElement.SetAttribute("length", "0");
                }
                else
                {
                    // Set them from existing values.
                    Emotion = Convert.ToInt32(emotion.InnerText);
                    Length = Convert.ToInt32(length.InnerText);
                }

                // Query file attributes, and convert them to globals.
                XmlNode id = QuerySingleNode("//xns:drx/@id");
                XmlNode fileRevision = QuerySingleNode("//xns:drx/@fileRevision");
                XmlNode series = QuerySingleNode("//xns:drx/@series");

                Id = Convert.ToInt32(id.InnerText);
                FileRevision = Convert.ToDouble(fileRevision.InnerText);
                Series = Convert.ToInt32(series.InnerText);

                // Query the body namespace.
                XmlNode documentBody = QuerySingleNode("//xns:drx/xns:body");

                XmlNode bodyHash = QuerySingleNode("//xns:drx/@bodyHash");
                if (bodyHash != null)
                {
                    BodyHash = bodyHash.InnerText;
                }
                else
                {
                    BodyHash = null;
                }


                #endregion

                #region EncryptionVariables

                // If encryption is enabled, set the respective attributes.
                if (Convert.ToBoolean(QuerySingleNode("//xns:drx/@encryptionEnabled").InnerText) == true &&
                    Convert.ToDecimal(QuerySingleNode("//xns:drx/@encryptionVersion").InnerText) != 0)
                {
                    // Query the encryption XML nodes.
                    XmlNode encryptionEnabled = QuerySingleNode("//xns:drx/@encryptionEnabled");
                    XmlNode encryptionVersion = QuerySingleNode("//xns:drx/@encryptionVersion");
                    XmlNode encryptionWrapper = QuerySingleNode("//xns:drx/@encryptionWrapper");
                    XmlNode encryptionType = QuerySingleNode("//xns:drx/@encryptionType");
                    XmlNode encryptionKey = QuerySingleNode("//xns:drx/@encryptionKey");
                    XmlNode encryptionSerial = QuerySingleNode("//xns:drx/@encryptionSerial");

                    if (BodyHash == null)
                    {
                        BodyHash = encryptionWrapper.InnerText;
                    }

                    // Set the globals.
                    EncryptionEnabled = Convert.ToBoolean(encryptionEnabled.InnerText);
                    EncryptionVersion = Convert.ToDouble(encryptionVersion.InnerText);
                    FileEncryptionType = (EncryptionType)Enum.Parse(typeof(EncryptionType), encryptionType.InnerText, true);

                    if (encryptionSerial == null)
                    {
                        // Initialize the element.
                        rootElement.SetAttribute("encryptionSerial", "0");
                    }
                    else
                    {
                        EncryptionSerial = encryptionSerial.InnerText;
                    }
                }
                else

                // Encryption isn't enabled, so no hard work to do! :)
                {
                    bodyText = documentBody.InnerText;
                }

                #endregion

                bodyText = documentBody.InnerText;

                // Set the document as valid.
                IsValid = true;

                // Set the filename (because if we loaded from a stream we need to save in the filesystem)
                CurrentFilePath = databaseProvider.GetDRXFilePath(Id);

                // Save the document in the local cache.
                Task.Run(async () => { await SaveChanges(true, true); });

                // Return true.
                return true;
            }

            // The DRX file wasn't found in the filesystem.
            catch (FileNotFoundException)
            {
                // Invalidate the document.
                IsValid = false;

                // Return false.
                return false;
            }
        }

        public async Task DeleteDocument()
        {
            fileReader.Close();
            RemoteDRXController controller = new RemoteDRXController(serviceProvider);
            await controller.DeleteFile(Id);
        }

        /// <summary>
        /// Creates or updates a new DRX document with the provided parameters.
        /// </summary>
        /// <param name="Document">The XmlDocument instance you'd like to create or update the document with.</param>
        /// <returns>A boolean value indicating whether the creation or update was successful.</returns>
        public bool UpdateDocument(bool creating = false, int id = 0)
        {
            // Check whether all document data is present.
            if (Id != 0 && 
                FileRevision != 0 && 
                Series != 0 && 
                FriendlyName != null && 
                SecurityLevel != null && 
                Flags != null && 
                Setting != null && 
                Date != null && 
                Status != null && 
                bodyText != null)
            {
                // Create a variable to store a temporary flag.
                XmlElement temporaryFlag;

                // Query all namespaces and assign to variables.
                XmlNode friendlyName = QuerySingleNode("//xns:drx/xns:header/xns:friendlyName");
                XmlNode securityLevel = QuerySingleNode("//xns:drx/xns:header/xns:secLevel");
                XmlNode redactions = QuerySingleNode("//xns:drx/xns:header/xns:redactions");
                XmlNode flags = QuerySingleNode("//xns:drx/xns:header/xns:flags");
                XmlNode setting = QuerySingleNode("//xns:drx/xns:header/xns:setting");
                XmlNode date = QuerySingleNode("//xns:drx/xns:header/xns:date");
                XmlNode status = QuerySingleNode("//xns:drx/xns:header/xns:status");

                // Set the DRX header.
                friendlyName.InnerText = FriendlyName;
                securityLevel.InnerText = SecurityLevel.ClassificationId;

                // Create an element to store our flags.
                XmlElement temporaryFlags = activeDocument.CreateElement("flags", "core://drx.schema.core/schemas/DRXSchema.xsd");

                // Do for each flag in the Flags global.
                foreach (DocumentFlag flag in Flags)
                {
                    // Create a new flag element for each DataFlag.
                    temporaryFlag = activeDocument.CreateElement("flag", "core://drx.schema.core/schemas/DRXSchema.xsd");
                    temporaryFlag.InnerText = flag.FlagId;
                    SaveNewNode(temporaryFlags, temporaryFlag);
                    flags.InnerXml = temporaryFlags.InnerXml;
                }

                // Set values of other parameters.
                setting.InnerText = Setting;
                date.InnerText = Date;
                status.InnerText = Status;
                redactions.InnerText = BooleanToString(Redactions);

                // Set the DRX file versioning and metadata.
                SetAttribute("//xns:drx/@id", Convert.ToString(Id));
                SetAttribute("//xns:drx/@fileRevision", Convert.ToString(FileRevision + 1));
                SetAttribute("//xns:drx/@series", Convert.ToString(Series.ToString()));

                // Set vividity and remembrance.
                if (Vividity != -1 && Remembrance != -1)
                {
                    SetAttribute("//xns:drx/@vividity", Convert.ToString(Vividity));
                    SetAttribute("//xns:drx/@remembrance", Convert.ToString(Remembrance));
                }

                // Set emotion and length.
                if (Emotion != -1 && Length != -1)
                {
                    SetAttribute("//xns:drx/@emotion", Convert.ToString(Emotion));
                    SetAttribute("//xns:drx/@length", Convert.ToString(Length));
                }

                string bodyShaSum = EncryptionProvider.Sha512(bodyText);
                BodyHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(bodyShaSum));

                // DELETE the encryption wrapper because it's deprecated.
                if (activeDocument.DocumentElement.GetAttribute("encryptionWrapper") != null)
                    activeDocument.DocumentElement.RemoveAttribute("encryptionWrapper");

                // Set the new attribute, the "body hash".
                activeDocument.DocumentElement.SetAttribute("bodyHash", BodyHash);
                activeDocument.DocumentElement.SetAttribute("encryptionEnabled", BooleanToString(EncryptionEnabled));

                XmlNode body = QuerySingleNode("//xns:drx/xns:body");
                if (EncryptionProvisioning == true)
                {
                    // Make sure the body is empty before doing this, just in case.
                    if (String.IsNullOrEmpty(bodyText) == true)
                    {
                        // Set the body to a blank RTF notation.
                        bodyText = @"{\rtf1\ansi\ansicpg1252\deff0\deflang2057{\fonttbl{\f0\fnil\fcharset0 Microsoft Sans Serif;}}\viewkind4\uc1\pard\f0\fs17\par}";
                    }
                }

                // Do this only if encryption is enabled and the encryption wrapper isn't null OR we're provisioning encryption.
                if (EncryptionEnabled == true && EncryptionVersion != 0 && BodyHash != null || EncryptionProvisioning == true)
                {
                    EncryptionProvisioning = false;

                    EncryptionProvider Crypto;

                    if (FileEncryptionType == EncryptionType.Password)
                    {
                        Crypto = new EncryptionProvider(EncryptionProvider.Sha512(EncryptionPassword));
                        body.InnerText = Crypto.Encrypt(bodyText);
                    }
                    else if (FileEncryptionType == EncryptionType.Certificate)
                    {
                        Crypto = new EncryptionProvider(EncryptionKey);
                        body.InnerText = EncryptionProvider.EncryptBody(bodyText, Convert.ToBase64String(encryptionDecryptedKey));

                        EncryptionCertificateKey = Convert.ToBase64String(EncryptionProvider.EncryptWithCert(EncryptionCertificate, encryptionDecryptedKey));
                        activeDocument.DocumentElement.SetAttribute("encryptionKey", EncryptionCertificateKey);
                        activeDocument.DocumentElement.SetAttribute("encryptionSerial", EncryptionSerial);
                    }
                }

                // We're not using encryption.
                else
                {
                    body.InnerText = bodyText;
                }

                // Validate the document.
                activeDocument.Validate(ValidationCallback);
                if (errorsOccured == true)
                {
                    IsValid = false;
                    return false;
                }
                else
                {
                    IsValid = true;
                    return true;
                }
            }

            // Not all document params are set.
            else
            {
                throw new Exception(strings.DRXNotEnoughParameters);
            }
        }

        public string GetBodyContents()
        {
            return GetBodyContents(false);
        }

        private string GetBodyContents(bool cryptoOverride = false)
        {
            if (EncryptionEnabled && (!HasBeenDecrypted && !cryptoOverride))
                throw new CryptographicException("Document has not yet been decrypted.");

            return bodyText;
        }

        public void SetBodyContents(string body)
        {
            // Strip any null bytes from the body.
            body = body.Replace("\0", String.Empty);

            if (EncryptionEnabled && !HasBeenDecrypted)
                throw new CryptographicException("Document has not yet been decrypted.");

            if (EncryptionEnabled)
            {
                // If encryption is enabled, set the respective attributes, and additionally encrypt the body.
                if (EncryptionEnabled == true && EncryptionVersion != 0 && BodyHash != null || EncryptionProvisioning == true)
                {
                    // Set encryption attributes.
                    activeDocument.DocumentElement.SetAttribute("encryptionEnabled", BooleanToString(EncryptionEnabled));
                    activeDocument.DocumentElement.SetAttribute("encryptionVersion", Convert.ToString(EncryptionVersion));
                    activeDocument.DocumentElement.SetAttribute("encryptionType", Enum.GetName(typeof(EncryptionType), FileEncryptionType));

                    if (FileEncryptionType == EncryptionType.Certificate)
                    {
                        // Generate an encryption key if one dosen't exist already.
                        // This is used when switching from encryption to a password.
                        if (EncryptionCertificateKey == null)
                        {
                            encryptionDecryptedKey = EncryptionProvider.GenerateEncryptionKey(EncryptionCertificate);
                        }
                    }
                }
            }

            bodyText = body;
        }

        public void DecryptBodyPassword(string password)
        {
            if (!EncryptionEnabled)
                throw new CryptographicException("Encryption is not enabled.");

            if (FileEncryptionType != EncryptionType.Password)
                throw new CryptographicException("Encryption type does not match the decryption method.");

            XmlNode bodyNode = QuerySingleNode("//xns:drx/xns:body");
            string bodyShaSum = null;
            string decryptionKey = EncryptionProvider.Sha512(password);

            EncryptionProvider CryptoProvider = new EncryptionProvider(decryptionKey);

            string bodyText_old = GetBodyContents(true);

            // Attempt to decrypt the body text.
            try
            {
                bodyText = CryptoProvider.Decrypt(bodyText_old);

                // Create another SHA512 object to validate the hash of the body.
                using (SHA512 SHA = new SHA512Managed())
                {
                    byte[] bodyByte = Encoding.UTF8.GetBytes(bodyText);
                    byte[] bodyBytes = SHA.ComputeHash(bodyByte);
                    string comparisonHash = Encoding.UTF8.GetString(bodyBytes, 0, bodyBytes.Length);
                    bodyShaSum = Convert.ToBase64String(Encoding.UTF8.GetBytes(comparisonHash));
                }
            }

            // The password is incorrect, or something bad happened.
            catch (CryptographicException) { }

            // Check whether the SHA512 hash of the body matches the EncryptionWrapper hash we stored in the file header
            // while we were creating the DRX.
            if (bodyShaSum != BodyHash)
            {
                // Set the body contents back to the original value.
                bodyText = bodyText_old;

                throw new CryptographicException("File hash does not match. Decryption error or incorrect password?");
            }

            // Set the EncryptionPassword global, just in case we need to use it later.
            EncryptionPassword = password;
            HasBeenDecrypted = true;
        }

        public void DecryptBodyCertificate(X509Certificate2 certificate = null)
        {
            if (!EncryptionEnabled)
                throw new CryptographicException("Encryption is not enabled.");

            if (FileEncryptionType != EncryptionType.Certificate)
                throw new CryptographicException("Encryption type does not match the decryption method.");

            XmlNode bodyNode = QuerySingleNode("//xns:drx/xns:body");
            XmlNode encryptionKey = QuerySingleNode("//xns:drx/@encryptionKey");

            if (EncryptionSerial == null)
                throw new CryptographicException("The encryption serial is null!");

            if (certificate == null)
            {
                certificate = EncryptionProvider.GetCertificateFromSerial(EncryptionSerial);
                if (certificate == null)
                    throw new CryptographicException("No matching certificates were found in the local user store to decrypt the document.");
            }

            EncryptionCertificateKey = encryptionKey.InnerText;

            // Create a string to store our sha512 string.
            string bodyShaSum = null;

            string cryptoException = null;

            // Attempt to decrypt the body text.
            try
            {
                // Decrypt the encrypted encryption key with the certificate.
                encryptionDecryptedKey = EncryptionProvider.DecryptWithCert(certificate, Convert.FromBase64String(EncryptionCertificateKey));
                string DecryptedKey = Convert.ToBase64String(encryptionDecryptedKey);

                // Create a new encryption provider using the decrypted encryption key to decrypt the body.
                EncryptionProvider enc = new EncryptionProvider(DecryptedKey);

                // Decrypt the body using the decryption key and pass it to the editor.
                bodyText = enc.Decrypt(GetBodyContents(true));

                // Set the serial of the certificate, if decryption succeeds.
                EncryptionSerial = certificate.SerialNumber;
            }

            // The password is incorrect, or something bad happened.
            catch (CryptographicException e)
            {
                // Make sure we set the BodyText to something invalid, so the next hash won't succeed.
                bodyText = "IncorrectPassword";
                cryptoException = e.Message;
            }

            // Create another SHA512 object to validate the hash of the body.
            using (SHA512 SHA = new SHA512Managed())
            {
                byte[] bodyByte = Encoding.UTF8.GetBytes(bodyText);
                byte[] bodyBytes = SHA.ComputeHash(bodyByte);
                string comparisonHash = Encoding.UTF8.GetString(bodyBytes, 0, bodyBytes.Length);
                bodyShaSum = Convert.ToBase64String(Encoding.UTF8.GetBytes(comparisonHash));
            }

            if (bodyShaSum != null)
            {
                // Check whether the SHA512 hash of the body matches the EncryptionWrapper hash we stored in the file header
                // while we were creating the DRX.
                if (bodyShaSum == BodyHash)
                {
                    EncryptionCertificate = certificate;
                    HasBeenDecrypted = true;
                    return;
                }
                else

                // The password is incorrect, or the hash or body is somehow corrupt.
                {
                    if (cryptoException == "Padding is invalid and cannot be removed.") { cryptoException = "The decryption key is incorrect."; };

                    throw new DRXLoadException(strings.DRXCorrupted + cryptoException ?? "Unknown error.");
                }
            }

            throw new DRXLoadException(strings.DRXCorrupted + "Unknown error.");
        }

        /// <summary>
        /// Sets the encryption key to the specified password.
        /// </summary>
        /// <param name="Password">The password.</param>
        public bool SetEncryptionKey(string Password)
        {
            // Make sure encryption is actually enabled.
            if (EncryptionEnabled == true && EncryptionVersion != 0)
            {
                // Create a new SHA512 object, and hash the encryption key with it.
                EncryptionKey = EncryptionProvider.Sha512(Password);

                // Return true.
                return true;
            }

            // Encryption isn't enabled.
            else
            {
                // Return false.
                return false;
            }
        }

        public void HashBody()
        {
            // Do this only if we're not provisioning encryption this cycle, because otherwise we'd be hashing nothing.
            if (EncryptionProvisioning != true)
            {
                // Set the encryption wrapper to the hash of the body.
                BodyHash = EncryptionProvider.Sha512(bodyText);
            }
        }

        /// <summary>
        /// Initializes a new DRX document with baseline XML.
        /// </summary>
        /// <param name="Document">The XmlDocument instance you'd like to initialize.</param>
        public bool InitializeDocument()
        {
            // Initialize the root namespace.
            XmlDeclaration declaration = activeDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
            activeDocument.AppendChild(declaration);

            // Add all elements to the root.
            XmlElement rootElement = activeDocument.CreateElement("drx", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement header = activeDocument.CreateElement("header", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement body = activeDocument.CreateElement("body", "core://drx.schema.core/schemas/DRXSchema.xsd");

            // Set all root elements to empty or default values.
            rootElement.SetAttribute("id", "0");
            rootElement.SetAttribute("fileRevision", "0");
            rootElement.SetAttribute("series", "0");
            rootElement.SetAttribute("encryptionEnabled", "false");
            rootElement.SetAttribute("vividity", "0");
            rootElement.SetAttribute("remembrance", "0");
            rootElement.SetAttribute("emotion", "0");
            rootElement.SetAttribute("length", "0");

            // Initialize the header namespace.
            XmlElement friendlyName = activeDocument.CreateElement("friendlyName", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement securityLevel = activeDocument.CreateElement("secLevel", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement redactions = activeDocument.CreateElement("redactions", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement flags = activeDocument.CreateElement("flags", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement setting = activeDocument.CreateElement("setting", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement date = activeDocument.CreateElement("date", "core://drx.schema.core/schemas/DRXSchema.xsd");
            XmlElement status = activeDocument.CreateElement("status", "core://drx.schema.core/schemas/DRXSchema.xsd");

            // Save all of the nodes.
            SaveNewNode(header, friendlyName);
            SaveNewNode(header, securityLevel);
            SaveNewNode(header, redactions);
            SaveNewNode(header, flags);
            SaveNewNode(header, setting);
            SaveNewNode(header, date);
            SaveNewNode(header, status);

            SaveNewNode(rootElement, header);
            SaveNewNode(rootElement, body);

            // Write it all to the root.
            activeDocument.AppendChild(rootElement);

            // Return true.
            return true;
        }

        /// <summary>
        /// Sets an attribute using the given XPath query string and value.
        /// </summary>
        /// <param name="XPath">The XPath query you'd like to run.</param>
        /// <param name="Value">The value to set the attribute to.</param>
        private void SetAttribute(string XPath, string Value)
        {
            XmlNode temporaryElement = QuerySingleNode(XPath);

            if (temporaryElement != null)
                temporaryElement.Value = Value;
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
        /// Apply some nice formatting to the declaration. See the method SaveNewNode().
        /// </summary>
        /// <param name="ParentNode">The node that is the direct parent of the child node.</param>
        /// <param name="ChildNode">The node you'd like to save.</param>
        private void SaveNewDeclaration(XmlNode T_ParentNode, XmlDeclaration T_ChildNode)
        {
            activeDocument.InsertBefore(T_ChildNode, T_ParentNode);
        }

        /// <summary>
        /// Method to validate and save the DRX document.
        /// </summary>
        /// <param name="filePath"></param>
        public async Task<bool> SaveChanges(bool skipCoreServer = false, bool skipUpdate = false)
        {
            if (!skipUpdate)
                UpdateDocument();

            // Validate the document.
            activeDocument.Validate(ValidationCallback);

            // Make sure no errors were set by the callback function.
            if (errorsOccured == true)
            {
                throw new Exception(strings.DRXSaveErrors);
            }
            else
            {
                try
                {
                    RemoteDRXController controller = new RemoteDRXController(serviceProvider);
                    await controller.SaveFile(this, Id, skipCoreServer);
                    return true;
                }
                catch (Exception e)
                {
                    throw new Exception(strings.DRXSaveErrorsException + e, e);
                }
            }
        }

        /// <summary>
        /// Method to send an XPath query.
        /// </summary>
        /// <param name="QueryString">The XPath query string.</param>
        /// <returns></returns>
        private XmlNode QuerySingleNode(string QueryString)
        {
            return activeDocument.DocumentElement.SelectSingleNode(QueryString, activeNamespace);
        }

        private string BooleanToString(bool boolean)
        {
            if (boolean) { return "true"; } else { return "false";  }
        }

        /// <summary>
        /// Event to catch errors from XmlDocument.Validate().
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        private void ValidationCallback(object sender, ValidationEventArgs a)
        {
            if (a.Severity == XmlSeverityType.Warning)
            {
                //MessageBox.Show(strings.DRXLoadWarning + a.Message, strings.DRXEditor, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else if (a.Severity == XmlSeverityType.Error)
            {
                //MessageBox.Show(strings.DRXLoadCorrupt + a.Message, strings.DRXEditor, MessageBoxButtons.OK, MessageBoxIcon.Error);
                errorsOccured = true;
                throw new Exception("Serious error: Document is corrupt: " + a.Message);
            }
        }

        #region IDisposable Support
        protected bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    fileReader.Close();
                    fileReader.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~IFileProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
