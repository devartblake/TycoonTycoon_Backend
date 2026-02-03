using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using System.Text.Json;
using EHttpMethod = Elastic.Transport.HttpMethod;

namespace Tycoon.Backend.Infrastructure.Analytics.Elastic
{
    /// <summary>
    /// Elastic admin utilities: templates + (optional) ILM/aliases via raw REST calls.
    /// ✅ Enhanced with better error logging
    /// </summary>
    public sealed class ElasticAdmin
    {
        private readonly ElasticsearchClient _client;
        private readonly ElasticOptions _opt;
        private readonly ILogger<ElasticAdmin>? _logger;

        public ElasticAdmin(
            ElasticsearchClient client,
            IOptions<ElasticOptions> opt,
            ILogger<ElasticAdmin>? logger = null)
        {
            _client = client;
            _opt = opt.Value;
            _logger = logger;

            if (string.IsNullOrWhiteSpace(_opt.Url))
                throw new InvalidOperationException("ElasticOptions.Url is missing.");
        }

        /// <summary>
        /// Step 6: Ensure index templates exist for rollups.
        /// This does NOT require ILM and avoids client.Ilm compile breaks.
        /// </summary>
        public async Task EnsureTemplatesAsync(CancellationToken ct)
        {
            await EnsureDailyRollupTemplateAsync("tycoon-qa-daily-rollups-template", ct);
            await EnsurePlayerDailyRollupTemplateAsync("tycoon-qa-player-daily-rollups-template", ct);
        }

        private async Task EnsureDailyRollupTemplateAsync(string templateName, CancellationToken ct)
        {
            try
            {
                _logger?.LogInformation("Checking if template '{TemplateName}' exists...", templateName);

                var exists = await _client.Indices.ExistsIndexTemplateAsync(templateName, ct);

                if (exists.Exists)
                {
                    _logger?.LogInformation("Template '{TemplateName}' already exists, skipping creation", templateName);
                    return;
                }

                _logger?.LogInformation("Creating template '{TemplateName}'...", templateName);

                var put = await _client.Indices.PutIndexTemplateAsync(templateName, t => t
                    .IndexPatterns(new[] { $"{_opt.DailyWriteAlias}*" })
                    .Template(tmp => tmp
                        .Settings(s => s
                            .NumberOfShards(1)
                            .NumberOfReplicas(0)
                        )
                        .Mappings(m => m.Properties(p => p
                            .Keyword("id")
                            .Date("utcDate", d => d.Format("strict_date"))
                            .Keyword("mode")
                            .Keyword("category")
                            .IntegerNumber("difficulty")
                            .LongNumber("totalAnswers")
                            .LongNumber("correctAnswers")
                            .LongNumber("wrongAnswers")
                            .LongNumber("sumAnswerTimeMs")
                            .LongNumber("minAnswerTimeMs")
                            .LongNumber("maxAnswerTimeMs")
                            .DoubleNumber("accuracy")
                            .DoubleNumber("avgAnswerTimeMs")
                            .Date("updatedAtUtc", d => d.Format("strict_date_optional_time||epoch_millis"))
                        ))
                    )
                    .Priority(500)
                    .Version(1), ct);

                if (!put.IsValidResponse)
                {
                    // ✅ Enhanced error logging
                    var errorDetails = new StringBuilder();
                    errorDetails.AppendLine($"Failed to create index template '{templateName}'");

                    if (put.ElasticsearchServerError != null)
                    {
                        //errorDetails.AppendLine($"Server Error: {put.ElasticsearchServerError}");
                        errorDetails.AppendLine($"Error Type: {put.ElasticsearchServerError.Error?.Type}");
                        errorDetails.AppendLine($"Error Reason: {put.ElasticsearchServerError.Error?.Reason}");
                    }

                    if (put.ApiCallDetails != null)
                    {
                        errorDetails.AppendLine($"HTTP Status: {put.ApiCallDetails.HttpStatusCode}");
                        errorDetails.AppendLine($"Debug Information: {put.ApiCallDetails.DebugInformation}");
                    }

                    if (put.TryGetOriginalException(out var ex))
                    {
                        errorDetails.AppendLine($"Original Exception: {ex.Message}");
                    }

                    _logger?.LogError("Template creation failed. Details: {ErrorDetails}", errorDetails.ToString());

                    throw new InvalidOperationException(errorDetails.ToString());
                }

                _logger?.LogInformation("Successfully created template '{TemplateName}'", templateName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception while ensuring template '{TemplateName}'", templateName);
                throw;
            }
        }

        private async Task EnsurePlayerDailyRollupTemplateAsync(string templateName, CancellationToken ct)
        {
            try
            {
                _logger?.LogInformation("Checking if template '{TemplateName}' exists...", templateName);

                var exists = await _client.Indices.ExistsIndexTemplateAsync(templateName, ct);

                if (exists.Exists)
                {
                    _logger?.LogInformation("Template '{TemplateName}' already exists, skipping creation", templateName);
                    return;
                }

                _logger?.LogInformation("Creating template '{TemplateName}'...", templateName);

                var put = await _client.Indices.PutIndexTemplateAsync(templateName, t => t
                    .IndexPatterns(new[] { $"{_opt.PlayerDailyWriteAlias}*" })
                    .Template(tmp => tmp
                        .Settings(s => s
                            .NumberOfShards(1)
                            .NumberOfReplicas(0)
                        )
                        .Mappings(m => m.Properties(p => p
                            .Keyword("id")
                            .Date("utcDate", d => d.Format("strict_date"))
                            .Keyword("playerId")
                            .Keyword("mode")
                            .Keyword("category")
                            .IntegerNumber("difficulty")
                            .LongNumber("totalAnswers")
                            .LongNumber("correctAnswers")
                            .LongNumber("wrongAnswers")
                            .LongNumber("sumAnswerTimeMs")
                            .LongNumber("minAnswerTimeMs")
                            .LongNumber("maxAnswerTimeMs")
                            .DoubleNumber("accuracy")
                            .DoubleNumber("avgAnswerTimeMs")
                            .Date("updatedAtUtc", d => d.Format("strict_date_optional_time||epoch_millis"))
                        ))
                    )
                    .Priority(500)
                    .Version(1), ct);

                if (!put.IsValidResponse)
                {
                    // ✅ Enhanced error logging
                    var errorDetails = new StringBuilder();
                    errorDetails.AppendLine($"Failed to create index template '{templateName}'");

                    if (put.ElasticsearchServerError != null)
                    {
                        //errorDetails.AppendLine($"Server Error: {put.ElasticsearchServerError}");
                        errorDetails.AppendLine($"Error Type: {put.ElasticsearchServerError.Error?.Type}");
                        errorDetails.AppendLine($"Error Reason: {put.ElasticsearchServerError.Error?.Reason}");
                    }

                    if (put.ApiCallDetails != null)
                    {
                        errorDetails.AppendLine($"HTTP Status: {put.ApiCallDetails.HttpStatusCode}");
                        errorDetails.AppendLine($"Debug Information: {put.ApiCallDetails.DebugInformation}");
                    }

                    if (put.TryGetOriginalException(out var ex))
                    {
                        errorDetails.AppendLine($"Original Exception: {ex.Message}");
                    }

                    _logger?.LogError("Template creation failed. Details: {ErrorDetails}", errorDetails.ToString());

                    throw new InvalidOperationException(errorDetails.ToString());
                }

                _logger?.LogInformation("Successfully created template '{TemplateName}'", templateName);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception while ensuring template '{TemplateName}'", templateName);
                throw;
            }
        }

        // ------------------------------------------------------------
        // Optional: Step 6.5 ILM + rollover aliases using raw REST calls
        // ------------------------------------------------------------

        /// <summary>
        /// Creates/updates an ILM policy via raw REST (works even if typed client lacks .Ilm surface).
        /// </summary>
        public async Task PutIlmPolicyRawAsync(string policyName, CancellationToken ct)
        {
            var body = new
            {
                policy = new
                {
                    phases = new
                    {
                        hot = new
                        {
                            actions = new
                            {
                                rollover = new { max_age = "30d", max_size = "20gb" }
                            }
                        }
                    }
                }
            };

            await RawPutAsync($"/_ilm/policy/{policyName}", body, ct);
        }

        /// <summary>
        /// Bootstrap rollover pattern:
        /// - create first backing index (e.g. tycoon-qa-daily-rollups-000001)
        /// - attach write alias (e.g. tycoon-qa-daily-rollups-write, is_write_index=true)
        /// - attach read alias (e.g. tycoon-qa-daily-rollups)
        /// </summary>
        public async Task BootstrapRolloverAliasesRawAsync(
            string firstIndex,
            string writeAlias,
            string readAlias,
            CancellationToken ct)
        {
            var body = new
            {
                aliases = new Dictionary<string, object>
                {
                    [writeAlias] = new { is_write_index = true },
                    [readAlias] = new { }
                }
            };

            await RawPutAsync($"/{firstIndex}", body, ct);
        }

        private async Task RawPutAsync(string path, object body, CancellationToken ct)
        {
            var json = JsonSerializer.Serialize(body);

            // v9 transport style: build an EndpointPath
            var endpointPath = new EndpointPath(EHttpMethod.PUT, path);

            var response = await _client.Transport.RequestAsync<StringResponse>(
                endpointPath,
                PostData.String(json),
                null,
                null,
                cancellationToken: ct);

            var code = response.ApiCallDetails?.HttpStatusCode ?? 0;
            if (code < 200 || code >= 300)
                throw new InvalidOperationException(
                    $"Elastic PUT {path} failed: HTTP {code} :: {response.Body}");
        }

    }
}