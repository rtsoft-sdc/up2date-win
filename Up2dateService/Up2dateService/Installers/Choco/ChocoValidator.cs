using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.Installers.Choco
{
    public class ChocoValidator : IPackageValidator
    {
        private const int ExitCodeSuccess = 0;

        private readonly ISettingsManager settingsManager;
        private readonly IWhiteListManager whiteListManager;
        private readonly ILogger logger;

        public ChocoValidator(ISettingsManager settingsManager, IWhiteListManager whiteListManager, ILogger logger)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.whiteListManager = whiteListManager ?? throw new ArgumentNullException(nameof(whiteListManager));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool VerifySignature(Package package)
        {
            if (!settingsManager.CheckSignature) return true;

            switch (settingsManager.SignatureVerificationLevel)
            {
                case SignatureVerificationLevel.SignedByAnyCertificate:
                    return CheckIfSigned(package, true);
                case SignatureVerificationLevel.SignedByTrustedCertificate:
                    return CheckIfSigned(package, false);
                case SignatureVerificationLevel.SignedByWhitelistedCertificate:
                    return CheckIfSigned(package, whiteListManager.GetWhitelistedCertificatesSha256());
                default:
                    throw new InvalidOperationException($"unsupported SignatureVerificationLevel {settingsManager.SignatureVerificationLevel}");
            }
        }

        private bool CheckIfSigned(Package package, bool byAnyCertificate)
        {
            const string ErrorCodeNotSigned = "NU3004";
            string nugetAgruments  = $"verify -Signatures \"{package.Filepath}\" -NonInteractive -Verbosity quiet";
            var mode = byAnyCertificate ? "any" : "trusted";
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "nuget.exe";
                p.StartInfo.Arguments = nugetAgruments;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                p.WaitForExit();
                string stdError = p.StandardError.ReadToEnd();
                bool isSigned = !stdError.Contains(ErrorCodeNotSigned);

                var result = byAnyCertificate ? isSigned : p.ExitCode == ExitCodeSuccess;

                if (!result)
                {
                    logger.WriteEntry($"Signature verification failed ({mode} certificate mode).\n{nugetAgruments}\n{stdError}");
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.WriteEntry($"Exception during signature verification ({mode} certificate mode).\nnuget.exe {nugetAgruments}", ex);
                return false;
            }
        }

        private bool CheckIfSigned(Package package, IList<string> certificateSha256s)
        {
            const string ErrorCodeNoSuchCertificate = "NU3034";

            // join SHA256 strings to reduce the number of calls to nuget: each call costs about 0.7 sec
            IEnumerable<string> certificateSha256sets = JoinStrings(certificateSha256s, ";", 2);

            var logFaultBuilder = new StringBuilder();

            foreach (var certificateSha256set in certificateSha256sets)
            {
                string nugetAgruments = $"verify -Signatures \"{package.Filepath}\" -CertificateFingerprint {certificateSha256set} -NonInteractive -Verbosity quiet";
                try
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "nuget.exe";
                    p.StartInfo.Arguments = nugetAgruments;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();
                    p.WaitForExit();
                    string stdError = p.StandardError.ReadToEnd();
                    bool isAvailable = !stdError.Contains(ErrorCodeNoSuchCertificate);

                    if (isAvailable) return true;

                    logFaultBuilder.AppendLine(nugetAgruments);
                    logFaultBuilder.AppendLine(stdError);
                }
                catch (Exception ex)
                {
                    logger.WriteEntry($"Exception during signature verification (white list mode).\nnuget.exe {nugetAgruments}", ex);
                    return false;
                }
            }

            logger.WriteEntry($"Signature verification failed (white list mode).\n{logFaultBuilder}");
            return false;
        }

        private IList<string> JoinStrings(IList<string> strings, string delimiter, int limit)
        {
            var resultList = new List<string>();
            var sb = new StringBuilder();
            for (int i = 0; i < strings.Count; i++)
            {
                if (i % limit != 0)
                {
                    sb.Append(delimiter);
                }
                sb.Append(strings[i]);
                if ((i + 1) % limit == 0 || i == strings.Count - 1)
                {
                    resultList.Add(sb.ToString());
                    sb.Clear();
                }
            }

            return resultList;
        }

    }
}
