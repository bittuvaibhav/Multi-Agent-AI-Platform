using System.ComponentModel;
using System.Text.Json;
using Enterprise.Agent.Core.Abstractions.Tools;
using Enterprise.Agent.Tools.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Enterprise.Agent.Tools;

/// <summary>
/// Retrieves current weather for a location from a keyless JSON endpoint. Degrades to a
/// clear message when the service is unavailable rather than throwing into the model.
/// </summary>
public sealed class WeatherTool : IKernelPluginSource
{
    public const string PluginName = "Weather";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly WeatherToolOptions _options;
    private readonly ILogger<WeatherTool> _logger;

    public WeatherTool(
        IHttpClientFactory httpClientFactory,
        IOptions<WeatherToolOptions> options,
        ILogger<WeatherTool> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    [KernelFunction("get_current_weather"), Description("Gets the current weather for a city or location.")]
    public async Task<string> GetCurrentWeatherAsync(
        [Description("City or location name, e.g. 'London'.")] string location,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return "A location must be provided.";
        }

        try
        {
            var url = string.Format(_options.EndpointFormat, Uri.EscapeDataString(location));
            var client = _httpClientFactory.CreateClient(nameof(WeatherTool));
            using var response = await client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return $"Weather service returned {(int)response.StatusCode} for '{location}'.";
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (doc.RootElement.TryGetProperty("current_condition", out var current) && current.GetArrayLength() > 0)
            {
                var c = current[0];
                var tempC = c.TryGetProperty("temp_C", out var t) ? t.GetString() : "?";
                var desc = c.TryGetProperty("weatherDesc", out var d) && d.GetArrayLength() > 0
                    ? d[0].GetProperty("value").GetString()
                    : "unknown";
                return $"Current weather in {location}: {desc}, {tempC}°C.";
            }

            return $"No current weather data available for '{location}'.";
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Weather lookup failed for {Location}.", location);
            return $"Unable to retrieve weather for '{location}' at this time.";
        }
    }

    public KernelPlugin BuildPlugin() => KernelPluginFactory.CreateFromObject(this, PluginName);
}
