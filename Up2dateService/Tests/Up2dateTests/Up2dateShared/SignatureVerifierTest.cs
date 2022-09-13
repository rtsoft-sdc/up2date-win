using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using Tests_Shared;
using Up2dateShared;

namespace Up2dateTests.Up2dateShared
{
    [TestClass]
    public class SignatureVerifierTest
    {
        private const string TestFileValidSignature = "testData\\validSignature.testObject";
        private const string TestFileInvalidSignature = "testData\\invalidSignature.testObject";
        private static SignatureVerifier _signatureVerifier;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _signatureVerifier = new SignatureVerifier();
        }

        [TestMethod]
        public void GivenFileSignedByGoodCertificate_WhenVerifiedForSignedByAnyCertificate_ThenReturnsTrue()
        {
            //Act
            var retVal = _signatureVerifier.VerifySignature(TestFileValidSignature, SignatureVerificationLevel.SignedByAnyCertificate);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByGoodCertificate_WhenVerifiedForSignedByTrustedCertificate_ThenReturnsTrue()
        {
            //Act
            var retVal = _signatureVerifier.VerifySignature(TestFileValidSignature, SignatureVerificationLevel.SignedByTrustedCertificate);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByGoodCertificate_WhenVerifiedForSignedByWhitelistedCertificate_ThenReturnsFalse()
        {
            //Act
            var retVal = _signatureVerifier.VerifySignature(TestFileValidSignature, SignatureVerificationLevel.SignedByWhitelistedCertificate);
            //Assert
            Assert.AreEqual(false, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByBadCertificate_WhenVerifiedForSignedByAnyCertificate_ThenReturnsTrue()
        {
            //Act
            var retVal = _signatureVerifier.VerifySignature(TestFileInvalidSignature, SignatureVerificationLevel.SignedByAnyCertificate);
            //Assert
            Assert.AreEqual(true, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByBadCertificate_WhenVerifiedForSignedByTrustedCertificate_ThenReturnsFalse()
        {
            //Act
            var retVal = _signatureVerifier.VerifySignature(TestFileInvalidSignature, SignatureVerificationLevel.SignedByTrustedCertificate);
            //Assert
            Assert.AreEqual(false, retVal);
        }

        [TestMethod]
        public void GivenFileSignedByBadCertificate_WhenVerifiedForSignedByWhitelistedCertificate_ThenReturnsFalse()
        {
            //Act
            var retVal = _signatureVerifier.VerifySignature(TestFileInvalidSignature, SignatureVerificationLevel.SignedByWhitelistedCertificate);
            //Assert
            Assert.AreEqual(false, retVal);
        }
    }
}
