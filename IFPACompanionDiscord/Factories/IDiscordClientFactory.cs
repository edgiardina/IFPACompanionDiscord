using DSharpPlus;

namespace IFPACompanionDiscord.Factories;

public interface IDiscordClientFactory
{
    Task<DiscordClient> CreateAndConfigureClientAsync();
}