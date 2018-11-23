using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;

namespace CoreDRXLibrary.Providers
{  
    /// <summary>
    /// Helper class which encrypts and decrypts using PKCS#11 certificates using RSA.
    /// </summary>
    public class EncryptionProvider
    {
        /// <summary>
        /// Unique encryption salt.
        /// </summary>
        private byte[] Salt = Encoding.ASCII.GetBytes("81PO9j8I1a94j");

        /// <summary>
        /// The plaintext (or hashed) encryption key.
        /// </summary>
        private string EncryptionKey;

        /// <summary>
        /// Whether we're using OAEP padding.
        /// Smart Cards don't support OAEP.
        /// </summary>
        private const bool UseOAEP = true;

        /// <summary>
        /// Class initializer for the EncryptionProvider class.
        /// </summary>
        /// <param name="EncryptionKey">The encryption key (e.g. password.)</param>
        public EncryptionProvider(string EncryptionKey)
        {
            this.EncryptionKey = EncryptionKey;
        }

        /// <summary>
        /// Encrypts the body of a document with a generated encryption key.
        /// </summary>
        /// <param name="body">The body to encrypt.</param>
        /// <param name="encryptionkey">The encryption key.</param>
        /// <returns>The Base64-encoded encrypted body.</returns>
        public static string EncryptBody(string body, string encryptionkey)
        {
            // Use the plaintext master key to encrypt the body.
            EncryptionProvider enc = new EncryptionProvider(encryptionkey);

            // Encrypt the body.
            return enc.Encrypt(body);
        }

        /// <summary>
        /// Retrieves the maximum key size of a certificate.
        /// </summary>
        /// <param name="cert">The certificate to use.</param>
        /// <returns>The max key size, in bytes.</returns>
        public static int GetMaxKeySize(X509Certificate2 cert)
        {
            RSACryptoServiceProvider csp = cert.PublicKey.Key as RSACryptoServiceProvider;

            return csp.KeySize;
        }

        /// <summary>
        /// Decrypts the body of a document with a given encrypted decryption key and certificate.
        /// </summary>
        /// <param name="body">The body to decrypt.</param>
        /// <param name="encryptionkey">The encryption key.</param>
        /// <param name="cert">The smart card or user store certificate to use to decrypt the master key.</param>
        /// <returns>The plaintext decrypted body.</returns>
        public static string DecryptBody(string body, string encryptionkey, X509Certificate2 cert)
        {
            // Decrypt the encrypted encryption key with the certificate.
            byte[] DecryptedBytes = DecryptWithCert(cert, Convert.FromBase64String(encryptionkey));
            
            string DecryptedKey = Convert.ToBase64String(DecryptedBytes);

            // Create a new encryption provider using the decrypted encryption key to decrypt the body.
            EncryptionProvider enc = new EncryptionProvider(DecryptedKey);

            // Return the decrypted body.
            return enc.Decrypt(body);
        }

        /// <summary>
        /// Generates a random encryption key.
        /// </summary>
        /// <param name="cert">The certificate to retrieve parameters from.</param>
        /// <returns>The generated encryption key bytes.</returns>
        public static byte[] GenerateEncryptionKey(X509Certificate2 cert)
        {
            if (IsSmartCard(cert))
            {
                // PKCS#1 padding - for smart cards.
                return GenerateRandomBytes((GetMaxKeySize(cert) / 8) - 11);
            }
            else
            {
                // OAEP padding - for everything else.
                return GenerateRandomBytes((GetMaxKeySize(cert) / 8) - 42);
            }
        }

        /// <summary>
        /// Generates random bytes of a specified length.
        /// </summary>
        /// <param name="Bytes">How many bytes to return.</param>
        /// <returns>The cryptographically secure random bytes.</returns>
        public static byte[] GenerateRandomBytes(int Bytes)
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] CryptoBytes = new byte[Bytes];
                rng.GetBytes(CryptoBytes);

                return CryptoBytes;
            }
        }

        /// <summary>
        /// Selects a user certificate from the certificate store.
        /// </summary>
        /// <param name="title">The title of the selection dialog.</param>
        /// <param name="message">The message of the selection dialog.</param>
        /// <returns>The certificate that was selected.</returns>
        public static X509Certificate2 GetCertificate(string title, string message, bool sc_only = false)
        {
            X509Store cstore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            cstore.Open(OpenFlags.ReadOnly);
            
            X509Certificate2Collection filtered_certs = cstore.Certificates.Find(X509FindType.FindByIssuerDistinguishedName, "CN=Tower Root CA, STREET=3 Airthrey Castle Yard University of Stirling, E=security@towerdevs.xyz, O=TOWER, OU=Security Dept, S=Scotland, L=Stirling, C=GB, DC=tower, DC=local", true);

            X509Certificate2Collection certs;

            if (sc_only)
            {
                X509Certificate2Collection sc_filtered_certs = new X509Certificate2Collection();

                foreach (X509Certificate2 certificate in filtered_certs)
                {
                    if (certificate.HasPrivateKey)
                    {
                        RSACng csp = certificate.PrivateKey as RSACng;

                        if (csp.Key.Provider.Provider == "Microsoft Smart Card Key Storage Provider")
                        {
                            sc_filtered_certs.Add(certificate);
                        }
                    }
                }

                certs = sc_filtered_certs;
            }
            else
            {
                throw new CryptographicException("The certificate specified could not be found.");
            }

            if (certs.Count == 1)
            {
                X509Certificate2 mcert = certs[0] as X509Certificate2;
                return mcert;
            }
            else
            {
                return null;
            }
        }

        public static X509Certificate2 GetCertificateFromSerial(string serial)
        {
            X509Store cstore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            cstore.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection test_certs = cstore.Certificates;
            X509Certificate2Collection filtered_certs = cstore.Certificates.Find(X509FindType.FindBySerialNumber, serial, true);

            if (filtered_certs.Count >= 1) {
                return filtered_certs[0];
            }
            else
            {
                return null;
            }
        }

        public static bool IsSmartCard(X509Certificate2 cert)
        {
            RSACng csp = cert.PrivateKey as RSACng;
            string Ksp = csp.Key.Provider.Provider;
            if (Ksp == "Microsoft Smart Card Key Storage Provider") { return true; } else { return false; }
        }

        /// <summary>
        /// Encrypts the plain text with RSA encryption from an X509 certificate.
        /// </summary>
        /// <param name="cert">The X509 certificate to use.</param>
        /// <param name="PlainText">The byte array to encrypt.</param>
        /// <returns>The encrypted bytes.</returns>
        public static byte[] EncryptWithCert(X509Certificate2 cert, byte[] PlainText)
        {
            // Create the service provider.
            RSACng csp = cert.PublicKey.Key as RSACng;

            byte[] CryptoBytes;

            // Check whether a smart card is being used.
            // Many smart cards don't support OAEP, so we have no other choice
            // than to disable OAEP.
            if (IsSmartCard(cert))
            {
                // Decrypt the bytes. (No OAEP)
                CryptoBytes = csp.Encrypt(PlainText, RSAEncryptionPadding.Pkcs1);
            }
            else
            {
                // Decrypt the bytes. OaepSHA1 is used for backwards compatability.
                CryptoBytes = csp.Encrypt(PlainText, RSAEncryptionPadding.OaepSHA1);
            }

            return CryptoBytes;
        }

        /// <summary>
        /// Decrypt the Base64 encoded encrypted text with a certificate.
        /// </summary>
        /// <param name="cert">The X509 certificate to use.</param>
        /// <param name="EncryptedText">The Base64 encoded text to decrypt.</param>
        /// <returns>The result of the decryption, as a byte array.</returns>
        public static byte[] DecryptWithCert(X509Certificate2 cert, byte[] EncryptedBytes)
        {
            byte[] DecryptedBytes = null;

            try
            {
                if (IsSmartCard(cert))
                {
                    // Create the service provider.
                    RSACng csp = cert.PrivateKey as RSACng;

                    // Decrypt the bytes.
                    DecryptedBytes = csp.Decrypt(EncryptedBytes, RSAEncryptionPadding.Pkcs1);
                }
                else
                {
                    // Create the service provider.
                    RSACng csp = cert.PrivateKey as RSACng;

                    DecryptedBytes = csp.Decrypt(EncryptedBytes, RSAEncryptionPadding.OaepSHA1);
                }
            }
            catch (Exception e)
            {
                throw new CryptographicException(e.Message);
            }

            return DecryptedBytes;
        }

        public string Encrypt(string PlainText)
        {
            AesManaged Algorithm = null;
            string Output = null;

            try
            {
                // Create a new Rfc2898DeriveBytes with the encryption key and salt.
                Rfc2898DeriveBytes PrivateKey = new Rfc2898DeriveBytes(this.EncryptionKey, this.Salt);

                // Create a Rijndael Managed instance.
                Algorithm = new AesManaged();

                // Assign the private key bytes from the derivebytes instance.
                Algorithm.Key = PrivateKey.GetBytes(Algorithm.KeySize / 8);

                // Set the padding to PKCS7.
                Algorithm.Padding = PaddingMode.PKCS7;

                // Create the CryptoTransform.
                ICryptoTransform Encryption = Algorithm.CreateEncryptor(Algorithm.Key, Algorithm.IV);

                // Write our encrypted bytes out to a StreamWriter.
                using (MemoryStream msa = new MemoryStream())
                {
                    msa.Write(BitConverter.GetBytes(Algorithm.IV.Length), 0, sizeof(int));
                    msa.Write(Algorithm.IV, 0, Algorithm.IV.Length);
                    using (CryptoStream csa = new CryptoStream(msa, Encryption, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swa = new StreamWriter(csa))
                        {
                            swa.Write(PlainText);
                        }
                    }

                    Output = Convert.ToBase64String(msa.ToArray());
                }
            }
            finally
            {
                if (Algorithm != null)
                {
                    Algorithm.Clear();
                }
            }

            return Output;
        }

        public string Decrypt(string EncryptedText)
        {
            AesManaged Algorithm = null;
            string Output = null;

            try
            {
                Rfc2898DeriveBytes PrivateKey = new Rfc2898DeriveBytes(this.EncryptionKey, this.Salt);

                byte[] KeyBytes = Convert.FromBase64String(EncryptedText);
                using (MemoryStream msb = new MemoryStream(KeyBytes))
                {
                    Algorithm = new AesManaged();
                    Algorithm.Key = PrivateKey.GetBytes(Algorithm.KeySize / 8);
                    Algorithm.IV = ReadByteArray(msb);
                    Algorithm.Padding = PaddingMode.PKCS7;
                    ICryptoTransform Decryption = Algorithm.CreateDecryptor(Algorithm.Key, Algorithm.IV);
                    using (CryptoStream csb = new CryptoStream(msb, Decryption, CryptoStreamMode.Read))
                    {
                        using (StreamReader srb = new StreamReader(csb))
                        {
                            Output = srb.ReadToEnd();
                        }
                    }
                }
            }
            finally
            {
                if (Algorithm != null)
                {
                    Algorithm.Clear();
                }
            }

            return Output;
        }
        
        public static string Sha512(string ToHash)
        {
            using (SHA512 SHA = new SHA512Managed())
            {
                byte[] HashByte = Encoding.UTF8.GetBytes(ToHash);
                byte[] HashBytes = SHA.ComputeHash(HashByte);
                string Hash = Encoding.UTF8.GetString(HashBytes, 0, HashBytes.Length);
                return Hash;
            }
        }

        public static string Base64Encode(string data)
        {
            byte[] str = Encoding.UTF8.GetBytes(data);
            return Convert.ToBase64String(str);
        }

        public static string Base64Decode(string data)
        {
            byte[] str = Convert.FromBase64String(data);
            return Encoding.UTF8.GetString(str);
        }

        private byte[] ReadByteArray(Stream st)
        {
            byte[] Length = new byte[sizeof(int)];
            st.Read(Length, 0, Length.Length);
            byte[] Buffer = new byte[BitConverter.ToInt32(Length, 0)];
            st.Read(Buffer, 0, Buffer.Length);

            return Buffer;
        }
    }
}
