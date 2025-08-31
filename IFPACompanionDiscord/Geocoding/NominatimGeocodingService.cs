using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IFPACompanionDiscord.Geocoding
{
    public interface IGeocodingService
    {
        Task<(double Latitude, double Longitude)> GeocodeAsync(string location);
    }
    public class NominatimGeocodingService : IGeocodingService
    {
        private readonly HttpClient _httpClient;

        public NominatimGeocodingService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(double Latitude, double Longitude)> GeocodeAsync(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
                throw new NullReferenceException();

            var url = $"https://nominatim.openstreetmap.org/search?format=json&q={Uri.EscapeDataString(location)}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.UserAgent.ParseAdd("IfpaCompanionDiscordBot/1.0 (ed@edgiardina.com)");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Error fetching geocode data: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            var results = JsonSerializer.Deserialize<List<NominatimResult>>(content);

            var first = results?.FirstOrDefault();
            if (first == null)
                throw new Exception("No results found for the given location.");

            if (double.TryParse(first.Lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) &&
                double.TryParse(first.Lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var lon))
            {
                return (lat, lon);
            }

            throw new FormatException("Could not parse latitude and longitude from Nominatim response.");
        }
        private class NominatimResult
        {
            [JsonPropertyName("place_id")]
            public long PlaceId { get; set; }

            [JsonPropertyName("licence")]
            public string Licence { get; set; }

            [JsonPropertyName("osm_type")]
            public string OsmType { get; set; }

            [JsonPropertyName("osm_id")]
            public long OsmId { get; set; }

            [JsonPropertyName("lat")]
            public string Lat { get; set; }

            [JsonPropertyName("lon")]
            public string Lon { get; set; }

            [JsonPropertyName("class")]
            public string Class { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("place_rank")]
            public int PlaceRank { get; set; }

            [JsonPropertyName("importance")]
            public double Importance { get; set; }

            [JsonPropertyName("addresstype")]
            public string AddressType { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("display_name")]
            public string DisplayName { get; set; }

            [JsonPropertyName("boundingbox")]
            public List<string> BoundingBox { get; set; }
        }
    }

}
