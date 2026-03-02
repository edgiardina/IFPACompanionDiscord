using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace IFPACompanionDiscord.HealthChecks;

public class DiscordConnectionHealthCheck(IFPABot bot) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = bot.Client;
            
            // Check if client exists and is connected
            if (client == null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Discord client not initialized"));
            }

            if (client.CurrentUser == null)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy("Discord client not authenticated"));
            }

            // Check ping if available (indicates websocket is responding)
            if (client.Ping > 0)
            {
                return Task.FromResult(HealthCheckResult.Healthy($"Discord connected as {client.CurrentUser.Username}, ping: {client.Ping}ms"));
            }

            // Connected but no ping data yet
            return Task.FromResult(HealthCheckResult.Healthy($"Discord connected as {client.CurrentUser.Username}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy($"Discord health check failed: {ex.Message}"));
        }
    }
}

public class IFPAApiConnectivityHealthCheck(HttpClient httpClient) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use HEAD request to check API availability without consuming data
            using var request = new HttpRequestMessage(HttpMethod.Head, "https://api.ifpapinball.com/docs/api.json");
            request.Headers.UserAgent.ParseAdd("IfpaCompanionDiscordBot/1.0 (health-check)");
            
            var response = await httpClient.SendAsync(request, cancellationToken);
            
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("IFPA API is reachable");
            }
            
            return HealthCheckResult.Degraded($"IFPA API returned {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy($"IFPA API is not reachable: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return HealthCheckResult.Unhealthy("IFPA API request timed out");
        }
    }
}