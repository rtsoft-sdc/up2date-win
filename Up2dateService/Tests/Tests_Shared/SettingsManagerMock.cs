using Moq;
using System.Collections.Generic;
using Up2dateShared;

namespace Tests_Shared
{
    public class SettingsManagerMock : Mock<ISettingsManager>
    {
        public SettingsManagerMock()
        {
            SetupProperty(o => o.CheckSignature);
            SetupProperty(o => o.SignatureVerificationLevel);
            SetupProperty(o => o.ProvisioningUrl);
            SetupProperty(o => o.RequiresConfirmationBeforeInstall);

            Object.ProvisioningUrl = "provisioningUrl";
            SetupGet(o => o.XApigToken).Returns("XApigToken");
            SetupGet(m => m.PackageExtensionFilterList).Returns(new List<string> { ".nuget", ".msi" });
        }
    }
}
