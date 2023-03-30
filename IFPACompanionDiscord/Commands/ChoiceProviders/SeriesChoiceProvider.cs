using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using PinballApi;

namespace IFPACompanionDiscord.Commands.ChoiceProviders
{
    public class SeriesChoiceProvider : ChoiceProvider
    {
        public PinballRankingApiV2 IFPAApi { get; set; }

        public override async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            //TODO: there's got to be a better way to do this
            IFPAApi = (PinballRankingApiV2)Services.GetService(typeof(PinballRankingApiV2));
            var series = await IFPAApi.GetSeries();

            return series.Select(n => new DiscordApplicationCommandOptionChoice(n.Code, n.Code));
        }

    }
}
