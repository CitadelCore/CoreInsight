using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreDynamic.Utils;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;

namespace CoreDynamic.Tests.Utils
{
    [TestClass]
    public class CertificateUtilitiesTests
    {
        [TestMethod]
        public void TestGenerateSignedX509Certificate()
        {
            X509Certificate2 signedCertificate = CertificateUtilities.GenerateSignedX509Certificate("CN=testcert", "CN=testcert", DateTime.Now.AddYears(1), 2048, null, "Unit Test Root Certificate", new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.NonRepudiation | KeyUsage.KeyCertSign | KeyUsage.CrlSign), new ExtendedKeyUsage(KeyPurposeID.IdKPTimeStamping, KeyPurposeID.IdKPOcspSigning));
            Assert.IsTrue(signedCertificate.HasPrivateKey);

            X509Certificate2 childCertificate = CertificateUtilities.GenerateSignedX509Certificate("CN=testcertchild", "CN=testcert", DateTime.Now.AddYears(1), 2048, signedCertificate, "Unit Test Child Certificate", new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment), new ExtendedKeyUsage(KeyPurposeID.IdKPClientAuth));
            Assert.IsTrue(childCertificate.HasPrivateKey);

            X509Certificate2 strippedTest = CertificateUtilities.StripCertificatePrivateKey(signedCertificate);
            Assert.IsFalse(strippedTest.HasPrivateKey);
        }
    }
}
