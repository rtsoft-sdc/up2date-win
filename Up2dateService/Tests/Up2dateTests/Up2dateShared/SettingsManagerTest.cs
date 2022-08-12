using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Up2dateShared;

namespace Up2dateTests.Up2dateShared
{
    [TestClass]
    public class SettingsManagerTest
    {
        private static SettingsManager _settings;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _settings = new SettingsManager();
        }

        [TestMethod]
        public void CertificateSerialNumber_Test_CheckWorkability()
        {
            //Act
            var certificateSerialNumber = _settings.CertificateSerialNumber;
            //Assert
            Assert.AreEqual("1", certificateSerialNumber);
        }

        [TestMethod]
        public void CheckSignature_Test_CheckWorkability()
        {
            //Act
            var checkSignature = _settings.CheckSignature;
            //Assert
            Assert.AreEqual(true, checkSignature);
        }

        [TestMethod]
        public void InstallAppFromSelectedIssuer_Test_CheckWorkability()
        {
            //Act
            var installAppFromSelectedIssuer = _settings.InstallAppFromSelectedIssuer;
            //Assert
            Assert.AreEqual(false, installAppFromSelectedIssuer);
        }

        [TestMethod]
        public void SelectedIssuers_Test_CheckWorkability()
        {
            //Act
            var selectedIssuers = _settings.SelectedIssuers;
            //Assert
            CollectionAssert.AreEqual(new List<string> { "rts" }, selectedIssuers);
        }


        [TestMethod]
        public void PackageExtensionFilterList_Test_CheckWorkability()
        {
            //Act
            var packageExtensionFilterList = _settings.PackageExtensionFilterList;
            //Assert
            CollectionAssert.AreEqual(new List<string> { ".msi", ".cert", ".exe" }, packageExtensionFilterList);
        }


        [TestMethod]
        public void ProvisioningUrl_Test_CheckWorkability()
        {
            //Act
            var provisioningUrl = _settings.ProvisioningUrl;
            //Assert
            Assert.AreEqual("https://dps.ritms.online/provisioning", provisioningUrl);
        }

        [TestMethod]
        public void RequestCertificateUrl_Test_CheckWorkability()
        {
            //Act
            var requestCertificateUrl = _settings.RequestCertificateUrl;
            //Assert
            Assert.AreEqual("http://enter.dev.ritms.online", requestCertificateUrl);
        }

        [TestMethod]
        public void XApigToken_Test_CheckWorkability()
        {
            //Act
            var xApigToken = _settings.XApigToken;
            //Assert
            Assert.AreEqual("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", xApigToken);
        }
    }
}