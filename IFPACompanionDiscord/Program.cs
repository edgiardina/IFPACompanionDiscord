using IFPACompanionDiscord;
using IFPACompanionDiscord.Configuration;
using IFPACompanionDiscord.Geocoding;
using IFPACompanionDiscord.Factories;
using IFPACompanionDiscord.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Interfaces;
using Polly;
using Polly.Extensions.Http;
using System.Globalization;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Starting IFPA Companion Discord Bot");

        // Culture is set explicitly because the IFPA values returned are in US Dollars
        CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

        var builder = Host.CreateApplicationBuilder(args);

        // Configuration
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>();

        // Configure strongly typed configuration
        builder.Services.Configure<BotConfiguration>(builder.Configuration);

        // Configure Polly retry policy for HTTP clients
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        // Register HttpClient for geocoding with retry policy
        builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>(client =>
        {
            client.DefaultRequestHeaders.UserAgent.ParseAdd("IfpaCompanionDiscordBot/1.0 (ed@edgiardina.com)");
            client.Timeout = TimeSpan.FromSeconds(30);
        }).AddPolicyHandler(retryPolicy);

        // Register HttpClient for health checks
        builder.Services.AddHttpClient<IFPAApiConnectivityHealthCheck>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        }).AddPolicyHandler(retryPolicy);

        // Register core services
        builder.Services.AddSingleton<IPinballRankingApi>(provider =>
        {
            var config = provider.GetRequiredService<IOptions<BotConfiguration>>().Value;
            return new PinballRankingApi(config.PinballApi.IFPAApiKey);
        });

        // Register memory cache for Polly caching
        builder.Services.AddMemoryCache();

        // Register Discord factory
        builder.Services.AddSingleton<IDiscordClientFactory, DiscordClientFactory>();
        builder.Services.AddSingleton<IFPABot>();

        // Add health checks
        builder.Services.AddHealthChecks()
            .AddCheck<DiscordConnectionHealthCheck>("discord")
            .AddCheck<IFPAApiConnectivityHealthCheck>("ifpa-api");

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

        var host = builder.Build();

        try
        {
            var ifpaBot = host.Services.GetRequiredService<IFPABot>();
            await ifpaBot.Run();
        }
        catch (Exception ex)
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogCritical(ex, "Application failed to start");
            throw;
        }
    }
}