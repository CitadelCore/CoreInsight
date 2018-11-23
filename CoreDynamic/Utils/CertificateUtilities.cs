using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Prng;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.X509.Extension;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CoreDynamic.Utils
{
    public class CertificateUtilities
    {
        public static X509Certificate2 StripCertificatePrivateKey(X509Certificate2 inputCertificate)
        {
            inputCertificate.PrivateKey = null;
            return inputCertificate;
        }

        /// <summary>
        /// Gets a cryptographically secure random generator instance
        /// for use with BouncyCastle library methods.
        /// </summary>
        /// <returns>Instance of SecureRandom seeded by a CryptoApiRandomGenerator.</returns>
        private static SecureRandom GetCryptoRandomGenerator()
        {
            CryptoApiRandomGenerator apiRandomGenerator = new CryptoApiRandomGenerator();
            SecureRandom random = new SecureRandom(apiRandomGenerator);
            return random;
        }

        /// <summary>
        /// Retrieves a signature factory from a X509Certificate2 private key.
        /// </summary>
        /// <param name="inputCert">Certificate that contains the private key.</param>
        /// <param name="random">SecureRandom instance to use for key generation.</param>
        /// <param name="algorithm">Algorithm to use. Defaults to SHA512WITHRSA.</param>
        /// <returns>ISignatureFactory instance ready for signing.</returns>
        private static ISignatureFactory GetFactoryFromPrivateKey(X509Certificate2 inputCert, SecureRandom random, string algorithm = "SHA512WITHRSA")
        {
            ISignatureFactory factory;
            if (!inputCert.HasPrivateKey)
                throw new Exception("The signing certificate does not contain a private key.");

            RSAParameters parameters;
            Type cspType = inputCert.PrivateKey.GetType();
            if (cspType == typeof(RSACng))
            {
                using (RSACng provider = (RSACng)inputCert.PrivateKey)
                {
                    parameters = provider.ExportParameters(true);
                }
            }
            else if (cspType == typeof(RSACryptoServiceProvider))
            {
                using (RSACryptoServiceProvider provider = (RSACryptoServiceProvider)inputCert.PrivateKey)
                {
                    parameters = provider.ExportParameters(true);
                }
            }
            else
            {
                throw new Exception("The private key uses an unsupported cryptographic provider class.");
            }

            RsaPrivateCrtKeyParameters crtKeyParameters = new RsaPrivateCrtKeyParameters(
                new BigInteger(1, parameters.Modulus),
                new BigInteger(1, parameters.Exponent),
                new BigInteger(1, parameters.D),
                new BigInteger(1, parameters.P),
                new BigInteger(1, parameters.Q),
                new BigInteger(1, parameters.DP),
                new BigInteger(1, parameters.DQ),
                new BigInteger(1, parameters.InverseQ));
            factory = new Asn1SignatureFactory(algorithm, crtKeyParameters, random);
            return factory;
        }

        /// <summary>
        /// Merges a BouncyCastle X509 certificate and a BouncyCastle PrivateKeyInfo into a .NET X509Certificate2 instance.
        /// If PrivateKeyInfo is null, no private key will be attached.
        /// </summary>
        /// <param name="certificate">X509Certificate that contains certificate info and public key.</param>
        /// <param name="keyInfo">Private key of the said certificate.</param>
        /// <returns>.NET certificate which contains entire keypair.</returns>
        private static X509Certificate2 BouncyCastleX509CertToDer(Org.BouncyCastle.X509.X509Certificate certificate, PrivateKeyInfo keyInfo = null)
        {
            X509Certificate2 nativeCertificate = new X509Certificate2(certificate.GetEncoded());

            if (keyInfo != null)
            {
                RsaPrivateKeyStructure certPrivateKey = RsaPrivateKeyStructure.GetInstance((Asn1Sequence)Asn1Object.FromByteArray(keyInfo.ParsePrivateKey().GetDerEncoded()));
                RsaPrivateCrtKeyParameters rsaPrivateCrt = new RsaPrivateCrtKeyParameters(certPrivateKey.Modulus, certPrivateKey.PublicExponent, certPrivateKey.PrivateExponent, certPrivateKey.Prime1, certPrivateKey.Prime2, certPrivateKey.Exponent1, certPrivateKey.Exponent2, certPrivateKey.Coefficient);
                nativeCertificate.PrivateKey = DotNetUtilities.ToRSA(rsaPrivateCrt);
            }

            return nativeCertificate;
        }

        /// <summary>
        /// Generates a root signed or self-signed X509 certificate depending
        /// on the parameters specified.
        /// </summary>
        /// <param name="subject">Subject DN string.</param>
        /// <param name="issuer">Issuer DN string. For self-signed certificates this should be the same as the subject.</param>
        /// <param name="validTo">Time the certificate is valid to.</param>
        /// <param name="privateKeyLength">Length of the private key. Defaults to 4096 bits.</param>
        /// <param name="signingCert">Root certificate to sign this certificate. Specify none to self-sign.</param>
        /// <param name="friendlyName">Friendly name of the certificate. Leave null for none.</param>
        /// <returns>Generated and signed X509Certificate2 containing entire keypair.</returns>
        public static X509Certificate2 GenerateSignedX509Certificate(string subject, string issuer, DateTime validTo, int privateKeyLength = 4096, X509Certificate2 signingCert = null, string friendlyName = null, KeyUsage keyUsage = null, ExtendedKeyUsage extendedKeyUsage = null)
        {
            X509V3CertificateGenerator Generator = new X509V3CertificateGenerator();
            Generator.SetSerialNumber(BigInteger.ProbablePrime(120, new Random()));
            Generator.SetIssuerDN(new X509Name(subject));
            Generator.SetSubjectDN(new X509Name(subject));
            Generator.SetNotAfter(validTo);
            Generator.SetNotBefore(DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0)));

            // Set extensions
            if (keyUsage != null)
                Generator.AddExtension(X509Extensions.KeyUsage, true, keyUsage);
            if (extendedKeyUsage != null)
                Generator.AddExtension(X509Extensions.ExtendedKeyUsage, false, extendedKeyUsage);

            // Generate the private key
            SecureRandom random = GetCryptoRandomGenerator();
            RsaKeyPairGenerator rsaKeyPairGenerator = new RsaKeyPairGenerator();
            rsaKeyPairGenerator.Init(new KeyGenerationParameters(random, privateKeyLength));

            // Generate the public keypair
            AsymmetricCipherKeyPair keyPair = rsaKeyPairGenerator.GenerateKeyPair();
            Generator.SetPublicKey(keyPair.Public);
            Generator.AddExtension(X509Extensions.SubjectKeyIdentifier, false, new SubjectKeyIdentifierStructure(keyPair.Public));

            // Retrive the private key from the signing certificate
            // or use our own if we're self signed
            ISignatureFactory factory;
            if (signingCert != null)
            {
                factory = GetFactoryFromPrivateKey(signingCert, random);
                Generator.AddExtension(X509Extensions.AuthorityKeyIdentifier, false, new AuthorityKeyIdentifierStructure(DotNetUtilities.FromX509Certificate(signingCert)));
            } else {
                factory = new Asn1SignatureFactory("SHA512WITHRSA", keyPair.Private, random);
                Generator.AddExtension(X509Extensions.BasicConstraints, true, new DerOctetString(new BasicConstraints(0)));
            }

            PrivateKeyInfo keyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(keyPair.Private);

            // Convert certificate to native X509 certificate
            Org.BouncyCastle.X509.X509Certificate generatedCert = Generator.Generate(factory);
            X509Certificate2 nativeCertificate = BouncyCastleX509CertToDer(generatedCert, keyInfo);

            // Set the friendly name
            if (friendlyName != null)
                nativeCertificate.FriendlyName = friendlyName;

            return nativeCertificate;
        }
    }
}
