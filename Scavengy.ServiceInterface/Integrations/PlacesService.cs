using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Scavengy.ServiceInterface.Integrations;

public interface IPlacesService
{
    Task<List<string>> SearchCitiesAsync(string input, CancellationToken ct);
    Task<PlaceLocation?> FindPlaceAsync(string query, CancellationToken ct);
}

public record PlaceLocation(string Address, double Latitude, double Longitude);

public class GooglePlacesService : IPlacesService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

            var payload = await response.Content.ReadFromJsonAsync<AutocompleteResponse>(JsonOptions, ct);

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

    public async Task<PlaceLocation?> FindPlaceAsync(string query, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "./places:searchText");
            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add("X-Goog-FieldMask", "places.formattedAddress,places.location");
            request.Content = JsonContent.Create(new { textQuery = query });

            using var response = await _http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<SearchTextResponse>(JsonOptions, ct);
            var place = payload?.Places?.FirstOrDefault();
            return place?.Location is null 
                ? null 
                : new PlaceLocation(place.FormattedAddress 
                                    ?? "", place.Location.Latitude, place.Location.Longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Places text search failed for {Query}", query);
            return null;
        }
    }

    private record AutocompleteResponse(List<Suggestion>? Suggestions);
    private record Suggestion(PlacePrediction? PlacePrediction);
    private record PlacePrediction(TextValue Text);
    private record TextValue(string Text);

    private record SearchTextResponse(List<Place>? Places);
    private record Place(string? FormattedAddress, LatLng? Location);
    private record LatLng(double Latitude, double Longitude);
}
