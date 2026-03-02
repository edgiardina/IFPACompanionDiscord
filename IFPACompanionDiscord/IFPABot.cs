using DSharpPlus;
using IFPACompanionDiscord.Factories;
using Microsoft.Extensions.Logging;

namespace IFPACompanionDiscord;

public class IFPABot(IDiscordClientFactory discordClientFactory, ILogger<IFPABot> logger)
{
    private DiscordClient? _discordClient;

    /// <summary>
    /// Gets the Discord client instance if connected, null otherwise
    /// </summary>
    public DiscordClient? Client => _discordClient;

    /// <summary>
    /// Gets whether the bot is connected to Discord
    /// </summary>
    public bool IsConnected => _discordClient?.CurrentUser != null;

    /// <summary>
    /// Start the IFPA Discord Companion Bot and listen infinitely for `/ifpa` messages
    /// </summary>
    /// <returns></returns>
    public async Task Run()
    {
        _discordClient = await discordClientFactory.CreateAndConfigureClientAsync();

        logger.LogInformation("Connecting to Discord...");
        await _discordClient.ConnectAsync();
        
        logger.LogInformation("Bot is running. Press Ctrl+C to stop.");
        await Task.Delay(-1);
    }
}
