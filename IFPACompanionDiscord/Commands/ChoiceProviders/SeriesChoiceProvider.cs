using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PinballApi;
using PinballApi.Interfaces;

namespace IFPACompanionDiscord.Commands.ChoiceProviders
{
    public class SeriesChoiceProvider : ChoiceProvider
    {
        public IPinballRankingApi IFPAApi { get; set; }

        public override async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            //TODO: there's got to be a better way to do this
            IFPAApi = (IPinballRankingApi)Services.GetService(typeof(IPinballRankingApi));
            var series = await IFPAApi.GetSeries();

            return series.Select(n => new DiscordApplicationCommandOptionChoice(n.Code, n.Code));
        }

    }
}
