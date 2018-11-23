using CoreDRXLibrary.Models;
using CoreDynamic.Interfaces;
using CoreDynamic.Providers;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml;

namespace CoreDRXLibrary.Interfaces
{
    /// <summary>
    /// Type of encryption. Can be Password (simple password encryption) or Certificate (local user/smart card certificate).
    /// </summary>
    public enum EncryptionType { Password, Certificate };

    /// <summary>
    /// Document mode; whether it is "registered" (e.g. in the DRX database), or "unregistered" (opened from a loose .DRX document with an invalid database hash)
    /// </summary>
    public enum DocumentMode { Registered, Unregistered };

    /// <summary>
    /// Type of the document. Defines behaviour in the editor.
    /// </summary>
    public enum DocumentType { DRX, Note, RealityCheck };

    /// <summary>
    /// Abstract class for manipulating a DRX file, and handling all of the "dirty work".
    /// </summary>
    public interface IFileProvider : IDisposable
    {
        /// <summary>
        /// The absolute file path to the current DRX.
        /// </summary>
        string CurrentFilePath { get; set; }

        /// <summary>
        /// The DRX ID to use while creating the file.
        /// </summary>
        int CreationID { get; set; }

        /// <summary>
        /// Whether the resulting DRX document is valid or not.
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        /// The ID of the DRX document.
        /// </summary>
        int Id { get; set; }

        /// <summary>
        /// The file revision of the DRX schema.
        /// </summary>
        double FileRevision { get; set; }

        /// <summary>
        /// The series of the DRX.
        /// </summary>
        int Series { get; set; }

        /// <summary>
        /// Whether the body is encrypted or not.
        /// </summary>
        bool EncryptionEnabled { get; set; }

        /// <summary>
        /// The version of encryption used.
        /// </summary>
        double EncryptionVersion { get; set; }

        /// <summary>
        /// Hash of the body to verify decryption success.
        /// </summary>
        string BodyHash { get; set; }

        /// <summary>
        /// Used for determining between encryption types.
        /// </summary>
        EncryptionType FileEncryptionType { get; set; }

        /// <summary>
        /// The serial of the encryption certificate.
        /// </summary>
        string EncryptionSerial { get; set; }

        /// <summary>
        /// The certificate used for DRX crypto operations.
        /// </summary>
        X509Certificate2 EncryptionCertificate { get; set; }

        /// <summary>
        /// The encrypted decryption key that decrypts the DRX body. Encrypted with the public key of the certificate.
        /// </summary>
        string EncryptionCertificateKey { get; set; }

        /// <summary>
        /// The other half of the encryption key, not stored in the file.
        /// </summary>
        string EncryptionKey { get; set; }

        /// <summary>
        /// Whether encryption is being provisioned for the first time.
        /// </summary>
        bool EncryptionProvisioning { get; set; }

        /// <summary>
        /// A password to optionally pass to the decryption function to override it.
        /// </summary>
        string EncryptionPassOverride { get; set; }

        /// <summary>
        /// The password used for re-encrypting the document.
        /// </summary>
        string EncryptionPassword { get; set; }

        /// <summary>
        /// Whether or not the document is encrypted.
        /// This is set by the initialization method.
        /// </summary>
        bool HasBeenDecrypted { get; set; }

        /// <summary>
        /// The "friendly name", or title, of the DRX.
        /// </summary>
        string FriendlyName { get; set; }

        /// <summary>
        /// The security level required to access the DRX.
        /// </summary>
        Classification SecurityLevel { get; set; }

        /// <summary>
        /// Whether any redactions have been made in the DRX.
        /// </summary>
        bool Redactions { get; set; }

        /// <summary>
        /// The array of flags in the DRX.
        /// </summary>
        List<DocumentFlag> Flags { get; set; }

        /// <summary>
        /// The setting of the DRX.
        /// </summary>
        string Setting { get; set; }

        /// <summary>
        /// The date of the DRX, in xs:date notation.
        /// </summary>
        string Date { get; set; }

        /// <summary>
        /// The status of the DRX.
        /// </summary>
        string Status { get; set; }

        /// <summary>
        /// The vividity rating of the DRX.
        /// </summary>
        int Vividity { get; set; }

        /// <summary>
        /// The remembrance rating of the DRX.
        /// </summary>
        int Remembrance { get; set; }

        /// <summary>
        /// The emotion rating of the DRX.
        /// </summary>
        int Emotion { get; set; }

        /// <summary>
        /// The length rating of the DRX.
        /// </summary>
        int Length { get; set; }

        /// <summary>
        /// Retrieves metadata from the DRX file without completely loading it.
        /// </summary>
        /// <returns>A dictionary value containing the file metadata.</returns>
        Dictionary<string, dynamic> GetMetadata();

        /// <summary>
        /// Attempts to load the specified DRX document.
        /// </summary>
        /// <returns>A boolean value indicating whether the load was successful.</returns>
        bool LoadDocument();

        /// <summary>
        /// Retrieves the contents of the DRX body.
        /// Will throw a CryptographicException if the DRX is still encrypted
        /// while it is being queried.
        /// </summary>
        /// <returns>String of the body contents.</returns>
        string GetBodyContents();

        /// <summary>
        /// Sets the contents of the body. This does NOT save the file to disk / CoreServer.
        /// </summary>
        void SetBodyContents(string body);

        /// <summary>
        /// Decrypts the body with a password.
        /// Throws a CryptographicException if the encryption type is not a password,
        /// or the body is not encrypted.
        /// </summary>
        /// <param name="password">The password to decrypt the body.</param>
        void DecryptBodyPassword(string password);

        /// <summary>
        /// Decrypts the body with a certificate.
        /// Throws a CryptographicException if the encryption type is not a certificate,
        /// or the body is not encrypted.
        /// </summary>
        /// <param name="certificate">The X.509 certificate to decrypt the body. If this parameter is not passed, it will attempt to find a certificate matching the EncryptionSerial parameter in the document.</param>
        void DecryptBodyCertificate(X509Certificate2 certificate = null);
        
        /// <summary>
        /// Deletes the specified DRX document.
        /// </summary>
        /// <returns>Whether the deletion was successful.</returns>
        Task DeleteDocument();

        /// <summary>
        /// Creates or updates a new DRX document with the provided parameters.
        /// </summary>
        /// <param name="Document">The XmlDocument instance you'd like to create or update the document with.</param>
        /// <returns>A boolean value indicating whether the creation or update was successful.</returns>
        bool UpdateDocument(bool Create = false, int id = 0);

        /// <summary>
        /// Sets the encryption key to the specified password.
        /// </summary>
        /// <param name="Password">The password.</param>
        bool SetEncryptionKey(string Password);

        /// <summary>
        /// Re-hashes the body and saves it in the XML file.
        /// </summary>
        void HashBody();

        /// <summary>
        /// Initializes a new DRX document with baseline XML.
        /// </summary>
        /// <param name="Document">The XmlDocument instance you'd like to initialize.</param>
        bool InitializeDocument();

        /// <summary>
        /// Method to validate and save the DRX document.
        /// </summary>
        /// <param name="skipCoreServer">Whether to skip uploading the document to CoreServer, and simply save it to the filesystem.</param>
        Task<bool> SaveChanges(bool skipCoreServer = false, bool skipUpdate = false);
    }
}
