using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests_Shared;
using Up2dateService;
using Up2dateShared;

namespace Up2dateTests.Up2dateService
{
    [TestClass]
    public class SettingsManagerTest
    {
        private static SettingsManager _settingsManager;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _settingsManager = new SettingsManager(new LoggerMock().Object);
        }

        [TestMethod]
        public void CheckSignature_Test_CheckWorkability()
        {
            //Act
            var checkSignature = _settingsManager.CheckSignature;
            //Assert
            Assert.AreEqual(true, checkSignature);
        }

        [TestMethod]
        public void InstallAppFromSelectedIssuer_Test_CheckWorkability()
        {
            //Act
            var signatureVerificationLevel = _settingsManager.SignatureVerificationLevel;
            //Assert
            Assert.AreEqual(SignatureVerificationLevel.SignedByAnyCertificate, signatureVerificationLevel);
        }

        [TestMethod]
        public void PackageExtensionFilterList_Test_CheckWorkability()
        {
            //Act
            var packageExtensionFilterList = _settingsManager.PackageExtensionFilterList;
            //Assert
            CollectionAssert.AreEqual(new List<string> { ".msi", ".nupkg" }, packageExtensionFilterList);
        }


        [TestMethod]
        public void ProvisioningUrl_Test_CheckWorkability()
        {
            //Act
            var provisioningUrl = _settingsManager.ProvisioningUrl;
            //Assert
            Assert.AreEqual("https://dps.ritms.online/provisioning", provisioningUrl);
        }

        [TestMethod]
        public void RequestCertificateUrl_Test_CheckWorkability()
        {
            //Act
            var requestCertificateUrl = _settingsManager.RequestCertificateUrl;
            //Assert
            Assert.AreEqual("http://enter.dev.ritms.online", requestCertificateUrl);
        }

        [TestMethod]
        public void XApigToken_Test_CheckWorkability()
        {
            //Act
            var xApigToken = _settingsManager.XApigToken;
            //Assert
            Assert.AreEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", xApigToken);
        }
    }
}