using System.Text.Json;

namespace Scavengy.Integrations;

public interface IPlacesService
{
    Task<List<string>> SearchCitiesAsync(string input, CancellationToken ct);
}

public class GooglePlacesService : IPlacesService
{
    private readonly HttpClient _http;
    private readonly ILogger<GooglePlacesService> _logger;
    private readonly string _apiKey;

    public GooglePlacesService(HttpClient http, IConfiguration config, ILogger<GooglePlacesService> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["GooglePlaces:ApiKey"] ?? throw new InvalidOperationException("GooglePlaces:ApiKey not configured");
    }

    public async Task<List<string>> SearchCitiesAsync(string input, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "./places:autocomplete");
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
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
