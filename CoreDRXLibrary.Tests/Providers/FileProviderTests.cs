using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CoreDynamic.Providers;
using System.IO;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using CoreDynamic.Interfaces;
using CoreDRXLibrary.Interfaces;
using CoreDRXLibrary.Providers;
using log4net;
using CoreDRXLibrary.Tests.Mocks;
using PCLStorage;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CoreDRXLibrary.Tests.Providers
{
    [TestClass]
    public class FileProviderTests
    {
        IServiceCollection serviceCollection;

        public FileProviderTests()
        {
            serviceCollection = new ServiceCollection
            {
                new ServiceDescriptor(typeof(ILog), new MockLogger())
            };

            serviceCollection.Add(new ServiceDescriptor(typeof(IApplicationProvider), new ApplicationProvider(serviceCollection, "CoreDRXLibrary.Tests", "CoreDRXLibrary.Tests", false)));
            serviceCollection.Add(new ServiceDescriptor(typeof(IDatabaseProvider), new MockDatabase(serviceCollection)));
        }

        [TestMethod]
        public void LoadDocumentTests()
        {
            IFileProvider TestProvider = new FileProvider(serviceCollection);

            // Assert that the file provider throws an exception if it isn't given a file to read from.
            Assert.ThrowsException<DRXLoadException>(() => { TestProvider.LoadDocument(); });

            // Assets that the file provider throws an exception if it's given a file with invalid syntax.
            Stream IncorrectSyntaxStream = new MemoryStream(Encoding.UTF8.GetBytes("incorrect_syntax"));
            TestProvider = new FileProvider(serviceCollection, IncorrectSyntaxStream);
            Assert.ThrowsException<DRXLoadException>(() => { TestProvider.LoadDocument(); });
        }

        [TestMethod]
        public void DocumentVariableTests()
        {
            IFileProvider TestProvider = new FileProvider(serviceCollection);

            // Assert that the file provider loads correctly if it's given a file with correct syntax and matching hashes.
            Stream CorrectSyntaxStream = new MemoryStream(Encoding.UTF8.GetBytes(Files.syntax_testfile));
            TestProvider = new FileProvider(serviceCollection.BuildServiceProvider(), CorrectSyntaxStream);
            TestProvider.LoadDocument();

            // Verify that all of the variables are set correctly.
            Assert.AreEqual(new string[] { "R" }.ToString(), TestProvider.Flags.ToString());
            Assert.AreEqual(false, TestProvider.Redactions);
            Assert.AreEqual("PLC", TestProvider.SecurityLevel);
            Assert.AreEqual("Test", TestProvider.Setting);
            Assert.AreEqual("2017-09-23T00:00:00", TestProvider.Date);
            Assert.AreEqual("default", TestProvider.Status);
            Assert.AreEqual(0, TestProvider.Vividity);
            Assert.AreEqual(0, TestProvider.Remembrance);
            Assert.AreEqual(0, TestProvider.Emotion);
            Assert.AreEqual(0, TestProvider.Length);
            Assert.AreEqual("Test", TestProvider.FriendlyName);
            Assert.AreEqual(@"{\rtf1\ansi\ansicpg1252\deff0\deflang2057{\fonttbl{\f0\fnil\fcharset0 Microsoft Sans Serif;}}{\colortbl;\red0\green0\blue0;\red255\green255\blue255;}\viewkind4\uc1\pard\f0\fs17 This file is a test DRX.\cf0\highlight0\par}", TestProvider.GetBodyContents());
        }
    }
}
