using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;

using Up2dateShared;

namespace Up2dateClient
{
    public class Client
    {
        const string clientType = "RITMS UP2DATE for Windows";

        private readonly HashSet<string> supportedTypes = new HashSet<string> { ".msi" }; // must be lowercase
        private readonly EventLog eventLog;
        private readonly ISettingsManager settingsManager;
        private readonly Func<string> getCertificate;
        private readonly ISetupManager setupManager;
        private readonly Func<SystemInfo> getSysInfo;
        private readonly Func<string> getDownloadLocation;
        private ClientState state;

        public Client(ISettingsManager settingsManager, Func<string> getCertificate, ISetupManager setupManager, Func<SystemInfo> getSysInfo, Func<string> getDownloadLocation, EventLog eventLog = null)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.getCertificate = getCertificate ?? throw new ArgumentNullException(nameof(getCertificate));
            this.setupManager = setupManager ?? throw new ArgumentNullException(nameof(setupManager));
            this.getSysInfo = getSysInfo ?? throw new ArgumentNullException(nameof(getSysInfo));
            this.getDownloadLocation = getDownloadLocation ?? throw new ArgumentNullException(nameof(getDownloadLocation));
            this.eventLog = eventLog;
        }

        public ClientState State
        {
            get => state;
            private set
            {
                if (state.Equals(value)) return;
                state = value;
                WriteLogEntry($"Status={state.Status}; {state.LastError}");
            }
        }

        public void Run()
        {
            IntPtr dispatcher = IntPtr.Zero;
            try
            {
                string cert = getCertificate();
                if (string.IsNullOrEmpty(cert))
                {
                    SetState(ClientStatus.NoCertificate);
                    return;
                }
                dispatcher = Wrapper.CreateDispatcher(OnConfigRequest, OnDeploymentAction, OnCancelAction);
                SetState(ClientStatus.Running);
                Wrapper.RunClient(cert, settingsManager.ProvisioningUrl, settingsManager.XApigToken, dispatcher, OnAuthErrorAction);
                SetState(ClientStatus.Stopped);
            }
            catch (Exception e)
            {
                SetState(ClientStatus.Stopped, e.Message);
            }
            finally
            {
                if (dispatcher != IntPtr.Zero)
                {
                    Wrapper.DeleteDispatcher(dispatcher);
                }
            }
        }

        private void SetState(ClientStatus status, string lastError = null)
        {
            State = new ClientState(status, lastError ?? string.Empty);
        }

        private IEnumerable<KeyValuePair> GetSystemInfo()
        {
            SystemInfo sysInfo = getSysInfo();
            yield return new KeyValuePair("client", clientType);
            yield return new KeyValuePair("computer", sysInfo.MachineName);
            yield return new KeyValuePair("platform", sysInfo.PlatformID.ToString());
            yield return new KeyValuePair("OS type", sysInfo.Is64Bit ? "64-bit" : "32-bit");
            yield return new KeyValuePair("version", sysInfo.VersionString);
            yield return new KeyValuePair("service pack", sysInfo.ServicePack);
        }

        private void OnConfigRequest(IntPtr responseBuilder)
        {
            WriteLogEntry("configuration requested.");

            foreach (var attribute in GetSystemInfo())
            {
                Wrapper.AddConfigAttribute(responseBuilder, attribute.Key, attribute.Value);
            }
        }

        private bool OnDeploymentAction(IntPtr artifact, DeploymentInfo info)
        {
            WriteLogEntry($"deployment requested.", info);
            if (!IsExtensionAllowed(info))
            {
                WriteLogEntry("Package is not allowed - deployment rejected", info);
                return false;
            }

            WriteLogEntry($"downloading...", info);

            setupManager.OnDownloadStarted(info.artifactFileName);

            Wrapper.DownloadArtifact(artifact, getDownloadLocation());

            setupManager.OnDownloadFinished(info.artifactFileName);

            WriteLogEntry("download completed.", info);
            var filePath = Path.Combine(getDownloadLocation(), info.artifactFileName);
            if (settingsManager.CheckSignature && !IsSigned(filePath))
            {
                File.Delete(filePath);
                WriteLogEntry("File not signed. File deleted", info);
                return false;
            }

            if(settingsManager.InstallAppFromSelectedIssuer && IsSignedByIssuer(filePath))
            {
                File.Delete(filePath);
                WriteLogEntry("File not signed by selected issuer. File deleted", info);
                return false;
            }

            if (info.updateType == "skip")
            {
                WriteLogEntry($"skip installation - not requested.", info);
                return true;
            }

            if (setupManager.IsPackageInstalled(info.artifactFileName))
            {
                WriteLogEntry($"skip installation - already installed.", info);
                return true;
            }

            bool success;
            if (IsSupported(info))
            {
                WriteLogEntry("installing...", info);
                success = setupManager.InstallPackage(info.artifactFileName);
                WriteLogEntry(!success ? "installation failed." : "installation finished.", info);
            }
            else
            {
                WriteLogEntry("Package is not supported for installing...", info);
                success = false;
            }


            return success;
        }

        private bool IsExtensionAllowed(DeploymentInfo info)
        {
            return settingsManager.PackageExtensionFilterList.Contains(Path.GetExtension(info.artifactFileName).ToLowerInvariant());
        }

        private bool IsSupported(DeploymentInfo info)
        {
            return supportedTypes.Contains(Path.GetExtension(info.artifactFileName).ToLowerInvariant());
        }

        private bool OnCancelAction(int stopId)
        {
            WriteLogEntry("cancel requested; unsupported");

            // todo
            return false;
        }

        private void OnAuthErrorAction(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) // means that provisioning error is fixed
            {
                SetState(ClientStatus.Running);
            }
            else
            {
                SetState(ClientStatus.CannotAccessServer, errorMessage);
            }
        }

        private void WriteLogEntry(string message, DeploymentInfo? info = null)
        {
            if (info == null)
            {
                eventLog?.WriteEntry($"Up2date client: {message}");
            }
            else
            {
                eventLog?.WriteEntry($"Up2date client: {message} Artifact={info.Value.artifactFileName}");
            }
        }

        private bool IsSigned(string file)
        {
            X509Certificate2 theCertificate;
            try
            {
                X509Certificate theSigner = X509Certificate.CreateFromSignedFile(file);
                theCertificate = new X509Certificate2(theSigner);
            }
            catch (Exception ex)
            {
                WriteLogEntry("No digital signature found: " + ex.Message);

                return false;
            }

            var theCertificateChain = new X509Chain();
            theCertificateChain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            theCertificateChain.ChainPolicy.RevocationMode = X509RevocationMode.Offline;
            theCertificateChain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
            theCertificateChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
            bool chainIsValid = theCertificateChain.Build(theCertificate);

            return chainIsValid;
        }


        private bool IsSignedByIssuer(string file)
        {
            X509Certificate2 theCertificate;
            try
            {
                X509Certificate theSigner = X509Certificate.CreateFromSignedFile(file);
                theCertificate = new X509Certificate2(theSigner);
                
            }
            catch (Exception ex)
            {
                WriteLogEntry("No digital signature found: " + ex.Message);

                return false;
            }

            return settingsManager.SelectedIssuers.Contains(theCertificate.IssuerName.Name);
        }
    }
}
            
