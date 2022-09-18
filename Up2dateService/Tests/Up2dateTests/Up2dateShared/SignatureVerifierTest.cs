using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Security.Cryptography.X509Certificates;
using Tests_Shared;
using Up2dateShared;

namespace Up2dateTests.Up2dateShared
{
    [TestClass]
    public class SignatureVerifierTest
    {
        private const string TestFileValidSignature = "testData\\validSignature.testObject";
        private const string TestFileInvalidSignature = "testData\\invalidSignature.testObject";

        [TestMethod]
        public void GivenFileSignedByGoodCertificate_WhenVerifiedForSignedByAnyCertificate_ThenReturnsTrue()
        {
            //Arrange
            var signatureVerifier = new SignatureVerifier();
            //Act
            var retVal = signatureVerifier.IsSignedbyAnyCertificate(TestFileValidSignature);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByBadCertificate_WhenVerifiedForSignedByAnyCertificate_ThenReturnsTrue()
        {
            //Arrange
            var signatureVerifier = new SignatureVerifier();
            //Act
            var retVal = signatureVerifier.IsSignedbyAnyCertificate(TestFileInvalidSignature);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByGoodCertificate_WhenVerifiedForSignedByTrustedCertificate_ThenReturnsTrue()
        {
            //Arrange
            var signatureVerifier = new SignatureVerifier();
            //Act
            var retVal = signatureVerifier.IsSignedbyValidAndTrustedCertificate(TestFileValidSignature);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByBadCertificate_WhenVerifiedForSignedByTrustedCertificate_ThenReturnsFalse()
        {
            //Arrange
            var signatureVerifier = new SignatureVerifier();
            //Act
            var retVal = signatureVerifier.IsSignedbyValidAndTrustedCertificate(TestFileInvalidSignature);
            //Assert
            Assert.AreEqual(false, retVal);
        }

        [DataTestMethod]
        [DataRow(TestFileValidSignature)]
        [DataRow(TestFileInvalidSignature)]
        public void GivenFileSignedByBadCertificate_WhenVerifiedForSignedByWhitelistedCertificate_ThenReturnsFalse(string testFile)
        {
            //Arrange
            var signatureVerifier = new SignatureVerifier();
            var wlmm = new WhiteListManagerMock();
            wlmm.Setup(o => o.IsWhitelistedCertificate(It.IsAny<X509Certificate2>())).Returns(false);
            //Act
            var retVal = signatureVerifier.IsSignedByWhitelistedCertificate(testFile, wlmm.Object);
            //Assert
            Assert.AreEqual(false, retVal);
        }

        [DataTestMethod]
        [DataRow(TestFileValidSignature)]
        [DataRow(TestFileInvalidSignature)]
        public void GivenFileSignedByWhitelistedCertificate_WhenVerifiedForSignedByWhitelistedCertificate_ThenReturnsTrue(string testFile)
        {
            //Arrange
            var signatureVerifier = new SignatureVerifier();
            var wlmm = new WhiteListManagerMock();
            wlmm.Setup(o => o.IsWhitelistedCertificate(It.IsAny<X509Certificate2>())).Returns(true);
            //Act
            var retVal = signatureVerifier.IsSignedByWhitelistedCertificate(testFile, wlmm.Object);
            //Assert
            Assert.AreEqual(true, retVal);
        }
    }
}
