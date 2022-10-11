using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Up2dateShared;

namespace Up2dateClient
{
    public class Client
    {
        private const string ClientType = "RITMS UP2DATE for Windows";

        private readonly ILogger logger;
        private readonly ISettingsManager settingsManager;
        private readonly Func<string> getCertificate;
        private readonly ISetupManager setupManager;
        private readonly Func<SystemInfo> getSysInfo;
        private readonly Func<string> getDownloadLocation;
        private ClientState state;
        private int lastStopID = -1;

        public Client(ISettingsManager settingsManager, Func<string> getCertificate, ISetupManager setupManager, Func<SystemInfo> getSysInfo, Func<string> getDownloadLocation, ILogger logger)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.getCertificate = getCertificate ?? throw new ArgumentNullException(nameof(getCertificate));
            this.setupManager = setupManager ?? throw new ArgumentNullException(nameof(setupManager));
            this.getSysInfo = getSysInfo ?? throw new ArgumentNullException(nameof(getSysInfo));
            this.getDownloadLocation = getDownloadLocation ?? throw new ArgumentNullException(nameof(getDownloadLocation));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ClientState State
        {
            get => state;
            private set
            {
                if (state.Equals(value)) return;
                state = value;
                WriteLogEntry($"Status={state.Status} {state.LastError}");
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
                SetState(ClientStatus.Reconnecting);
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
            yield return new KeyValuePair("client", ClientType);
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

        private void OnDeploymentAction(IntPtr artifact, DeploymentInfo info, out ClientResult result)
        {
            StringBuilder messageBuilder = new StringBuilder();

            void LogMessage(string message)
            {
                messageBuilder.AppendLine(message);
                WriteLogEntry(message, info);
            }

            ClientResult LogAndMakeResult(Finished finished, Execution execution, string message)
            {
                LogMessage(message);

                return new ClientResult
                {
                    Message = messageBuilder.ToString(),
                    Finished = finished,
                    Execution = execution
                };
            }

            LogMessage($"Artifact '{info.artifactFileName}' deployment requested.");

            if (!IsExtensionAllowed(info))
            {
                result = LogAndMakeResult(Finished.FAILURE, Execution.CLOSED, "Package type is not allowed - deployment rejected.");
                return;
            }

            if (!IsSupported(info))
            {
                result = LogAndMakeResult(Finished.FAILURE, Execution.CLOSED , "Package type is not allowed - deployment rejected.");
                return;
            }

            if (lastStopID == info.id)
            {
                result = LogAndMakeResult(Finished.NONE, Execution.CANCELED, "Deployment action is cancelled.");
                lastStopID = -1;
                return;
            }

            if (setupManager.IsFileDownloaded(info.artifactFileName, info.artifactFileHashMd5))
            {
                LogMessage("File has been already downloaded.");
            }
            else
            {
                LogMessage("Download started.");

                setupManager.OnDownloadStarted(info.artifactFileName);
                try
                {
                    Wrapper.DownloadArtifact(artifact, getDownloadLocation());
                }
                catch (Exception)
                {
                    result = LogAndMakeResult(Finished.FAILURE, Execution.CLOSED, "Download failed");
                    return;
                }
                finally
                {
                    setupManager.OnDownloadFinished(info.artifactFileName);
                }

                LogMessage("Download completed.");
            }

            if (setupManager.IsPackageInstalled(info.artifactFileName))
            {
                result = LogAndMakeResult(Finished.SUCCESS, Execution.CLOSED, "Package has been already installed.");
                return;
            }

            switch (info.updateType)
            {
                case "skip":
                    result = info.isInMaintenanceWindow 
                        ? LogAndMakeResult(Finished.SUCCESS, Execution.CLOSED, "Only download is requested.")
                        : LogAndMakeResult(Finished.NONE, Execution.SCHEDULED, "Waiting for maintenance window to start installation.");
                    return;
                case "attempt":
                    {
                        PackageStatus status = setupManager.GetStatus(info.artifactFileName);
                        if (status == PackageStatus.Failed)
                        {
                            InstallPackageResult installPackageResult = setupManager.GetInstallPackageResult(info.artifactFileName);
                            result = LogAndMakeResult(Finished.FAILURE, Execution.CLOSED, ResultToMessage(installPackageResult));
                            return;
                        }
                        setupManager.MarkPackageAsSuggested(info.artifactFileName);
                        result = LogAndMakeResult(Finished.NONE, Execution.SCHEDULED, "Installation is not forced; suggested to user.");
                        return;
                    }
                case "forced":
                    {
                        LogMessage("Forced installation started.");
                        setupManager.InstallPackage(info.artifactFileName);
                        PackageStatus status = setupManager.GetStatus(info.artifactFileName);
                        if (status == PackageStatus.Failed)
                        {
                            InstallPackageResult installPackageResult = setupManager.GetInstallPackageResult(info.artifactFileName);
                            result = LogAndMakeResult(Finished.FAILURE, Execution.CLOSED, ResultToMessage(installPackageResult));
                            return;
                        }
                        result = LogAndMakeResult(Finished.SUCCESS, Execution.CLOSED, "Installation completed.");
                        return;
                    }
                default:
                    result = LogAndMakeResult(Finished.FAILURE, Execution.REJECTED, $"Unsupported update type: {info.updateType}, request rejected.");
                    return;
            }
        }

        private string ResultToMessage(InstallPackageResult installPackageStatus)
        {
            string message = "Installation failed. ";
            switch (installPackageStatus)
            {
                case InstallPackageResult.PackageUnavailable:
                    message += "Package unavailable or unusable";
                    break;
                case InstallPackageResult.FailedToInstallChocoPackage:
                    message += "Failed to install Choco package";
                    break;
                case InstallPackageResult.ChocoNotInstalled:
                    message += "Chocolatey is not installed";
                    break;
                case InstallPackageResult.GeneralInstallationError:
                    message += "General installation error";
                    break;
                case InstallPackageResult.SignatureVerificationFailed:
                    message += "Signature verification for the package is failed. " +
                        $"Requested level: {settingsManager.SignatureVerificationLevel}. Deployment rejected";
                    break;
                case InstallPackageResult.PackageNotSupported:
                    message += "Package of this type is not supported";
                    break;
                case InstallPackageResult.CannotStartInstaller:
                    message += "Failed to start installer process";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return message;
        }

        private bool IsExtensionAllowed(DeploymentInfo info)
        {
            return settingsManager.PackageExtensionFilterList.Contains(Path.GetExtension(info.artifactFileName).ToLowerInvariant());
        }

        private bool IsSupported(DeploymentInfo info)
        {
            return setupManager.IsFileSupported(info.artifactFileName);
        }

        private bool OnCancelAction(int stopId)
        {
            lastStopID = stopId;
            return true;
        }

        private void OnAuthErrorAction(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage)) // means that provisioning error is fixed
            {
                SetState(ClientStatus.Running);
            }
            else
            {
                SetState(ClientStatus.AuthorizationError, errorMessage);
            }
        }

        private void WriteLogEntry(string message, DeploymentInfo? info = null)
        {
            logger.WriteEntry(info == null ? message : $"{message}\nArtifact={info.Value.artifactFileName}");
        }
    }
}
