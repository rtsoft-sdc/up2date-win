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
    }
}