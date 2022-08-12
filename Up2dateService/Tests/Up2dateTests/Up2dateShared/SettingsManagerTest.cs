using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Up2dateShared;

namespace Up2dateTests.Up2dateShared
{
    [TestClass]
    public class SettingsManagerTest
    {
        private static SettingsManager _settingsManager;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _settingsManager = new SettingsManager();
        }

        [TestMethod]
        public void CertificateSerialNumber_Test_CheckWorkability()
        {
            //Act
            var certificateSerialNumber = _settingsManager.CertificateSerialNumber;
            //Assert
            Assert.AreEqual("1", certificateSerialNumber);
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
            var installAppFromSelectedIssuer = _settingsManager.InstallAppFromSelectedIssuer;
            //Assert
            Assert.AreEqual(false, installAppFromSelectedIssuer);
        }

        [TestMethod]
        public void SelectedIssuers_Test_CheckWorkability()
        {
            //Arrange
            var expectedIssuers = new List<string> { "rts" };
            //Act
            var selectedIssuers = _settingsManager.SelectedIssuers;
            //Assert
            for (var index = 0; index < selectedIssuers.Count; index++)
                Assert.AreEqual(expectedIssuers[index], selectedIssuers[index],
                    $"Item at index {index} are not equal. Expected: {expectedIssuers[index]}. Was {selectedIssuers[index]}");
        }


        [TestMethod]
        public void PackageExtensionFilterList_Test_CheckWorkability()
        {
            //Act
            var packageExtensionFilterList = _settingsManager.PackageExtensionFilterList;
            //Assert
            CollectionAssert.AreEqual(new List<string> { ".msi", ".cert", ".exe" }, packageExtensionFilterList);
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