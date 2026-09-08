using System.Text.Json;

namespace PhotoService.Services;

/// <summary>
/// Resolves the caller's int profile id (UserService ProfileId) from their
/// bearer token. photo-service historically keyed per-user resources by a
/// per-process hash of the Keycloak UUID claim, which is neither the int
/// ProfileId the rest of the system uses nor stable across restarts.
///
/// This resolver forwards the caller's Authorization header to UserService
/// GET /api/profiles/me (which maps the Keycloak "sub" to the int ProfileId)
/// and returns that id. Null is returned when the caller cannot be resolved
/// (e.g. UserService unreachable) so callers can fail closed.
/// </summary>
public interface IProfileIdResolver
{
    /// <summary>Resolve the bearer caller to their int UserService ProfileId.</summary>
    Task<int?> ResolveProfileIdAsync(string? authorizationHeader, CancellationToken ct);
}

/// <summary>Default <see cref="IProfileIdResolver"/> backed by UserService.</summary>
public class ProfileIdResolver : IProfileIdResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProfileIdResolver> _logger;

    public ProfileIdResolver(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<ProfileIdResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int?> ResolveProfileIdAsync(string? authorizationHeader, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
            return null;

        var client = _httpClientFactory.CreateClient();
        try
        {
            // Route through the gateway (profilesRoute → userCluster). The caller's
            // bearer token is forwarded unchanged; UserService validates it.
            var gatewayBase = _configuration["Gateway:BaseUrl"] ?? "http://localhost:8080";
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"{gatewayBase.TrimEnd('/')}/api/profiles/me");
            request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);

            using var response = await client.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "ProfileId resolution failed: UserService returned {Status}", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // UserService wraps in { success, data: { ... id ... } }; tolerate unwrapped too.
            JsonElement profile = root;
            if (root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
                profile = data;

            if (profile.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.Number)
                return idProp.GetInt32();

            _logger.LogWarning("ProfileId resolution failed: no numeric id in UserService response");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ProfileId resolution failed (UserService unreachable?)");
            return null;
        }
    }
}
