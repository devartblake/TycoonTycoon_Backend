using Microsoft.Extensions.Configuration;
using Serilog;
using Synaptix.Setup.Secrets;

namespace Synaptix.Setup.Security;

/// <summary>
/// Security-aware validator for setup secrets.
/// Wraps SecretValidationService and adds KMS availability check.
/// Additional rules per the synaptix_setup_security_kms_reuse_recommendation.md:
///   - Outside local: reject ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION
///   - Outside local: reject JWT_SECRET_KEY containing "change-me"
///   - Outside local: reject MIGRATION_RESET_DATABASE=true
///   - Outside local: reject MIGRATION_ALLOW_ENSURE_CREATED=true
///   - When KmsRequired: fail if KMS unreachable
/// </summary>
public sealed class SetupSecretValidator
{
    private readonly SetupSecretOptions _options;

    public SetupSecretValidator(SetupSecretOptions options)
    {
        _options = options;
    }

    public async Task<ValidationReport> ValidateAsync(
        IConfiguration cfg,
        bool isLocal,
        bool strict,
        CancellationToken ct = default)
    {
        // Base secret check
        var baseReport = new SecretValidationService().Validate(cfg, isLocal, strict);

        var extraErrors   = new List<string>(baseReport.Errors);
        var extraWarnings = new List<string>(baseReport.Warnings);

        // KMS availability check
        if (_options.ProtectionMode is SetupSecretProtectionMode.KmsRequired or SetupSecretProtectionMode.KmsPreferred)
        {
            var kmsAvailable = await CheckKmsAsync(ct);
            if (!kmsAvailable)
            {
                var msg = $"KMS at '{_options.KmsBaseUrl}' is unreachable.";
                if (_options.ProtectionMode == SetupSecretProtectionMode.KmsRequired && !isLocal)
                    extraErrors.Add(msg);
                else
                    extraWarnings.Add($"{msg} Falling back to PlaintextLocal.");
            }
            else
            {
                Log.Information("KMS reachable at {Url}.", _options.KmsBaseUrl);
            }
        }

        var passed = extraErrors.Count == 0 && (!strict || extraWarnings.Count == 0);
        return new ValidationReport(extraErrors, extraWarnings, passed);
    }

    private async Task<bool> CheckKmsAsync(CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.KmsBaseUrl)) return false;
        try
        {
            using var http = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var resp = await http.GetAsync($"{_options.KmsBaseUrl.TrimEnd('/')}/health", ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
