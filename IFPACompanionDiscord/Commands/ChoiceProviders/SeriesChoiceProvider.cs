using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PinballApi.Interfaces;

namespace IFPACompanionDiscord.Commands.ChoiceProviders;

public class SeriesChoiceProvider : ChoiceProvider
{
    public IPinballRankingApi IFPAApi { get; set; } = null!;

    public override async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
    {
        // Get service from DI if not already injected
        IFPAApi ??= Services.GetService(typeof(IPinballRankingApi)) as IPinballRankingApi 
                   ?? throw new InvalidOperationException("IPinballRankingApi service not found");

        var series = await IFPAApi.GetSeries();

        return series.Select(n => new DiscordApplicationCommandOptionChoice(n.Code, n.Code));
    }
}
