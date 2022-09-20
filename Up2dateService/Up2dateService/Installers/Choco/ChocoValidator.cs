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

        public ChocoValidator(ISettingsManager settingsManager, IWhiteListManager whiteListManager)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.whiteListManager = whiteListManager ?? throw new ArgumentNullException(nameof(whiteListManager));
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

            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "nuget.exe";
                p.StartInfo.Arguments = $"verify -Signatures \"{package.Filepath}\" -NonInteractive -Verbosity quiet";
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                p.WaitForExit();
                bool isSigned = !p.StandardError.ReadToEnd().Contains(ErrorCodeNotSigned);

                return byAnyCertificate ? isSigned : p.ExitCode == ExitCodeSuccess;
            }
            catch
            {
                return false;
            }
        }

        private bool CheckIfSigned(Package package, IList<string> certificateSha256s)
        {
            const string ErrorCodeNoSuchCertificate = "NU3034";

            // join SHA256 strings to reduce the number of calls to nuget: each call costs about 0.7 sec
            IEnumerable<string> certificateSha256sets = JoinStrings(certificateSha256s, ";", 2);

            foreach (var certificateSha256set in certificateSha256sets)
            {
                try
                {
                    Process p = new Process();
                    p.StartInfo.FileName = "nuget.exe";
                    p.StartInfo.Arguments = $"verify -Signatures \"{package.Filepath}\" -CertificateFingerprint {certificateSha256set} -NonInteractive -Verbosity quiet";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardError = true;
                    p.Start();
                    p.WaitForExit();
                    bool isAvailable = !p.StandardError.ReadToEnd().Contains(ErrorCodeNoSuchCertificate);

                    if (isAvailable) return true;
                }
                catch
                {
                    return false;
                }
            }

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
