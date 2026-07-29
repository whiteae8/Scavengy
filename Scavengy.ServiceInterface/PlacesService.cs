using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Scavengy.ServiceInterface;

public interface IPlacesService
{
    Task<PlaceLocation?> FindPlaceAsync(string query, string expectedLocation, CancellationToken ct);
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

    public async Task<PlaceLocation?> FindPlaceAsync(string query, string expectedLocation, CancellationToken ct)
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
            if (place?.Location is null || place.FormattedAddress is null) return null;

            if (!MatchesExpectedLocation(place.FormattedAddress, expectedLocation))
            {
                _logger.LogWarning(
                    "Places result {Address} did not match expected location {ExpectedLocation} for query {Query}",
                    place.FormattedAddress, expectedLocation, query);
                return null;
            }

            return new PlaceLocation(place.FormattedAddress, place.Location.Latitude, place.Location.Longitude);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Places text search failed for {Query}", query);
            return null;
        }
    }

    private static bool MatchesExpectedLocation(string address, string expectedLocation)
    {
        var parts = expectedLocation.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Skip(1).All(part => address.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private record SearchTextResponse(List<Place>? Places);
    private record Place(string? FormattedAddress, LatLng? Location);
    private record LatLng(double Latitude, double Longitude);
}
