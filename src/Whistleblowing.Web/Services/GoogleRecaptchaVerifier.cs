using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Whistleblowing.Web.Services;

public sealed class GoogleRecaptchaVerifier(
    HttpClient httpClient,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<GoogleRecaptchaVerifier> logger) : IRecaptchaVerifier
{
    private const string VerificationUrl = "https://www.google.com/recaptcha/api/siteverify";

    public async Task<bool> VerifyAsync(string? responseToken, CancellationToken cancellationToken = default)
    {
        var secret = hostEnvironment.IsDevelopment()
            ? configuration["GoogleReCaptcha:Development:SecretKey"] ?? configuration["GoogleReCaptcha:SecretKey"]
            : configuration["GoogleReCaptcha:SecretKey"];
        if (string.IsNullOrWhiteSpace(secret) || string.IsNullOrWhiteSpace(responseToken))
            return false;

        try
        {
            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secret,
                ["response"] = responseToken
            });

            using var response = await httpClient.PostAsync(VerificationUrl, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google reCAPTCHA verification returned HTTP {StatusCode}.", response.StatusCode);
                return false;
            }

            var result = await response.Content.ReadFromJsonAsync<RecaptchaVerificationResult>(cancellationToken: cancellationToken);
            return result?.Success == true;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            logger.LogWarning(exception, "Could not verify the anonymous concern reCAPTCHA response.");
            return false;
        }
    }

    private sealed class RecaptchaVerificationResult
    {
        [JsonPropertyName("success")]
        public bool Success { get; init; }
    }
}
