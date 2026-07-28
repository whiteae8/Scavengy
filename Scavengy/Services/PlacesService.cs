using System.Text.Json;

namespace Scavengy.Services;

public interface IPlacesService
{
    Task<List<string>> SearchCitiesAsync(string input, CancellationToken ct);
}

public class GooglePlacesService : IPlacesService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<GooglePlacesService> _logger;

    public GooglePlacesService(HttpClient http, IConfiguration config, ILogger<GooglePlacesService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<List<string>> SearchCitiesAsync(string input, CancellationToken ct)
    {
        var apiKey = _config["GooglePlaces:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("GooglePlaces:ApiKey not configured; returning no suggestions");
            return [];
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "places:autocomplete");
            request.Headers.Add("X-Goog-Api-Key", apiKey);
            request.Content = JsonContent.Create(new { input, includedPrimaryTypes = new[] { "locality" } });

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<AutocompleteResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, ct);

            return payload?.Suggestions?
                .Where(s => s.PlacePrediction is not null)
                .Select(s => s.PlacePrediction!.Text.Text)
                .ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Places autocomplete failed for {Input}", input);
            return [];
        }
    }

    private record AutocompleteResponse(List<Suggestion>? Suggestions);
    private record Suggestion(PlacePrediction? PlacePrediction);
    private record PlacePrediction(TextValue Text);
    private record TextValue(string Text);
}
