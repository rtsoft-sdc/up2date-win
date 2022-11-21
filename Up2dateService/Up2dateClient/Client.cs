using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Up2dateDotNet;
using Up2dateShared;

namespace Up2dateClient
{
    public class Client
    {
        private const string ClientType = "RITMS UP2DATE for Windows";

        private readonly ILogger logger;
        private readonly Version clientVersion;
        private readonly IWrapper wrapper;
        private readonly ISettingsManager settingsManager;
        private readonly Func<string> getCertificate;
        private readonly ISetupManager setupManager;
        private readonly Func<SystemInfo> getSysInfo;
        private ClientState state;
        private int lastStopID = -1;

        public Client(IWrapper wrapper, ISettingsManager settingsManager, Func<string> getCertificate, ISetupManager setupManager, Func<SystemInfo> getSysInfo, ILogger logger, Version clientVersion)
        {
            this.wrapper = wrapper ?? throw new ArgumentNullException(nameof(wrapper));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.getCertificate = getCertificate ?? throw new ArgumentNullException(nameof(getCertificate));
            this.setupManager = setupManager ?? throw new ArgumentNullException(nameof(setupManager));
            this.getSysInfo = getSysInfo ?? throw new ArgumentNullException(nameof(getSysInfo));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.clientVersion = clientVersion ?? throw new ArgumentNullException(nameof(clientVersion));
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

        public string Run()
        {
            try
            {
                string cert = getCertificate();
                if (settingsManager.SecureAuthorizationMode)
                {
                    if (string.IsNullOrEmpty(cert))
                    {
                        SetState(ClientStatus.NoCertificate);
                        return "No certificate.";
                    }
                    SetState(ClientStatus.Running);
                    wrapper.RunClient(cert, settingsManager.ProvisioningUrl, settingsManager.XApigToken, OnAuthErrorAction, 
                        OnConfigRequest, OnDeploymentAction, OnCancelAction);
                }
                else
                {
                    SetState(ClientStatus.Running);
                    var uri = settingsManager.HawkbitUrl.TrimEnd('/') + "/" + settingsManager.DeviceId;
                    wrapper.RunClientWithDeviceToken(settingsManager.SecurityToken, uri,
                        OnConfigRequest, OnDeploymentAction, OnCancelAction);
                }

                SetState(ClientStatus.Reconnecting);
            }
            catch (Exception e)
            {
                SetState(ClientStatus.Reconnecting, e.ToString());
                return e.Message;
            }
            return string.Empty;
        }

        public void RequestStop()
        {
            wrapper.StopClient();
        }

        private void SetState(ClientStatus status, string lastError = null)
        {
            State = new ClientState(status, lastError ?? string.Empty);
        }

        private IEnumerable<KeyValuePair> GetSystemInfo()
        {
            SystemInfo sysInfo = getSysInfo();
            yield return new KeyValuePair("client", ClientType);
            yield return new KeyValuePair("client version", $"{clientVersion.Major}.{clientVersion.Minor}.{clientVersion.Build}");
            yield return new KeyValuePair("computer", sysInfo.MachineName);
            yield return new KeyValuePair("machine GUID", sysInfo.MachineGuid);
            yield return new KeyValuePair("OS platform", sysInfo.PlatformID.ToString());
            yield return new KeyValuePair("OS type", sysInfo.Is64Bit ? "64-bit" : "32-bit");
            yield return new KeyValuePair("OS version", sysInfo.VersionString);
            yield return new KeyValuePair("OS service pack", sysInfo.ServicePack);
        }

        private void OnConfigRequest(IntPtr responseBuilder)
        {
            WriteLogEntry("configuration requested.");

            // system info
            foreach (var attribute in GetSystemInfo())
            {
                wrapper.AddConfigAttribute(responseBuilder, attribute.Key, attribute.Value);
            }

            // settings
            wrapper.AddConfigAttribute(responseBuilder, "settings.requires_confirmation_before_update",
                settingsManager.RequiresConfirmationBeforeInstall ? "yes" : "no");
            wrapper.AddConfigAttribute(responseBuilder, "settings.signature_verification_level",
                settingsManager.CheckSignature ? settingsManager.SignatureVerificationLevel.ToString() : "off");
            wrapper.AddConfigAttribute(responseBuilder, "settings.connection_mode",
                settingsManager.SecureAuthorizationMode ? "secure" : "by token (unsafe)");
        }

        private void OnDeploymentAction(IntPtr artifact, DeploymentInfo info, out ClientResult result)
        {
            StringBuilder messageBuilder = new StringBuilder();

            void LogMessage(string message)
            {
                messageBuilder.AppendLine(message);
                WriteLogEntry(message, info);
            }

            ClientResult CompleteExecution(Finished finished, Execution execution, string message)
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
                result = CompleteExecution(Finished.FAILURE, Execution.CLOSED, "Package type is not allowed - deployment rejected.");
                return;
            }

            if (!IsSupported(info))
            {
                result = CompleteExecution(Finished.FAILURE, Execution.CLOSED , "Package type is not allowed - deployment rejected.");
                return;
            }

            if (lastStopID == info.id)
            {
                result = CompleteExecution(Finished.NONE, Execution.CANCELED, "Deployment action is cancelled.");
                lastStopID = -1;
                return;
            }

            setupManager.CreateOrUpdatePackage(info.artifactFileName, info.id);

            if (setupManager.IsFileDownloaded(info.artifactFileName, info.artifactFileHashMd5))
            {
                LogMessage("File has been already downloaded.");
            }
            else
            {
                LogMessage("Download started.");
                Result downloadResult = setupManager.DownloadPackage(info.artifactFileName, info.artifactFileHashMd5,
                    location => wrapper.DownloadArtifact(artifact, location));
                if (!downloadResult.Success)
                {
                    result = CompleteExecution(Finished.FAILURE, Execution.CLOSED, $"Download failed. {downloadResult.ErrorMessage}");
                    return;
                }
                LogMessage("Download completed.");
            }

            if (setupManager.IsPackageInstalled(info.artifactFileName))
            {
                result = CompleteExecution(Finished.SUCCESS, Execution.CLOSED, "Package has been already installed.");
                return;
            }

            switch (info.updateType)
            {
                case "skip":
                    result = info.isInMaintenanceWindow
                        ? CompleteExecution(Finished.SUCCESS, Execution.CLOSED, "Only download is requested.")
                        : CompleteExecution(Finished.NONE, Execution.DOWNLOADED, "Waiting for maintenance window to start installation.");
                    return;
                case "attempt":
                    result = CheckUserFeedbackToCompleteInstallation(info.artifactFileName, CompleteExecution, LogMessage);
                    return;
                case "forced":
                    if (settingsManager.RequiresConfirmationBeforeInstall)
                    {
                        result = CheckUserFeedbackToCompleteInstallation(info.artifactFileName, CompleteExecution, LogMessage, forced: true);
                        return;
                    }
                    LogMessage("Forced installation started.");
                    result = InstallPackage(info.artifactFileName, CompleteExecution);
                    return;
                default:
                    result = CompleteExecution(Finished.FAILURE, Execution.CLOSED, $"Unsupported update type: {info.updateType}, request rejected.");
                    return;
            }
        }

        private ClientResult CheckUserFeedbackToCompleteInstallation(string artifactFileName,
            Func<Finished, Execution, string, ClientResult> completeExecution,
            Action<string> logMessage,
            bool forced = false)
        {
            PackageStatus status = setupManager.GetStatus(artifactFileName);
            if (status == PackageStatus.AcceptPending)
            {
                logMessage("Installation is accepted by user.");
                return InstallPackage(artifactFileName, completeExecution);
            }
            if (status == PackageStatus.RejectPending)
            {
                setupManager.MarkPackageRejected(artifactFileName);
                return completeExecution(Finished.FAILURE, Execution.CLOSED, "Installation is rejected by user.");
            }

            setupManager.MarkPackageWaitingForConfirmation(artifactFileName, forced);

            return completeExecution(Finished.NONE, Execution.DOWNLOADED,
                "Installation pending - waiting for user confirmation.");
        }

        private ClientResult InstallPackage(string artifactFileName, Func<Finished, Execution, string, ClientResult> completeExecution)
        {
            InstallPackageResult installPackageResult = setupManager.InstallPackage(artifactFileName);
            Finished finished = installPackageResult == InstallPackageResult.Success || installPackageResult == InstallPackageResult.RestartNeeded ? Finished.SUCCESS : Finished.FAILURE;
            return completeExecution(finished, Execution.CLOSED, ResultToMessage(installPackageResult));
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
                case InstallPackageResult.Success:
                    message = "Installation successfully completed";
                    break;
                case InstallPackageResult.RestartNeeded:
                    message = "To complete installation system restart is needed.";
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
            setupManager.Cancel(stopId);
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
