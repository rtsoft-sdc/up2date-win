using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests_Shared;
using Up2dateShared;

namespace Up2dateTests.Up2dateShared
{
    [TestClass]
    public class CertificateManagerTest
    {
        private const string TestFileValidSignature = "testData\\validSignature.testObject";
        private const string TestFileInvalidSignature = "testData\\invalidSignature.testObject";
        private static readonly string TestIssuer = "Microsoft Code Signing PCA 2011";
        private static SettingsManagerMock _settings;
        private static CertificateManager _certificateManager;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _settings = new SettingsManagerMock
            {
                SelectedIssuers = new List<string> { TestIssuer }
            };

            _certificateManager = new CertificateManager(_settings, new EventLog());
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _settings = null;
        }

        [TestMethod]
        public void IsSigned_Test_ValidSignature()
        {
            //Act
            var retVal = _certificateManager.IsSigned(TestFileValidSignature);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void IsSigned_Test_InvalidSignature()
        {
            //Act
            var retVal = _certificateManager.IsSigned(TestFileInvalidSignature);
            //Assert
            Assert.AreEqual(false, retVal);
        }

        [TestMethod]
        public void IsSignedByIssuer_Test_IssuerIsAllowed()
        {
            //Act
            var retVal = _certificateManager.IsSignedByIssuer(TestFileValidSignature);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void IsSignedByIssuer_Test_IssuerIsNotAllowed()
        {
            //Act
            var retVal = _certificateManager.IsSignedByIssuer(TestFileInvalidSignature);
            //Assert
            Assert.AreEqual(false, retVal);
        }
    }
}