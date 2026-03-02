using DSharpPlus;
using DSharpPlus.SlashCommands;
using IFPACompanionDiscord.Commands;
using IFPACompanionDiscord.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IFPACompanionDiscord.Factories;

public class DiscordClientFactory(
    IOptions<BotConfiguration> configuration,
    IServiceProvider serviceProvider,
    ILogger<DiscordClientFactory> logger) : IDiscordClientFactory
{
    private readonly BotConfiguration _config = configuration?.Value ?? throw new ArgumentNullException(nameof(configuration));

    public async Task<DiscordClient> CreateAndConfigureClientAsync()
    {
        if (string.IsNullOrEmpty(_config.Discord.Token))
        {
            throw new InvalidOperationException("Discord token is not configured");
        }

        logger.LogInformation("Creating Discord client...");

        var discordClient = new DiscordClient(new DiscordConfiguration
        {
            Token = _config.Discord.Token,
            TokenType = TokenType.Bot,
            MinimumLogLevel = ParseLogLevel(_config.Discord.LogLevel)
        });

        var slashCommands = discordClient.UseSlashCommands(new SlashCommandsConfiguration
        {
            Services = serviceProvider
        });

        slashCommands.RegisterCommands<IfpaSlashCommand>();

        logger.LogInformation("Discord client configured with slash commands");

        return discordClient;
    }

    private static LogLevel ParseLogLevel(string logLevel) => logLevel.ToLowerInvariant() switch
    {
        "trace" => LogLevel.Trace,
        "debug" => LogLevel.Debug,
        "information" => LogLevel.Information,
        "warning" => LogLevel.Warning,
        "error" => LogLevel.Error,
        "critical" => LogLevel.Critical,
        "none" => LogLevel.None,
        _ => LogLevel.Debug
    };
}