using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleTables;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using IFPACompanionDiscord.Extensions;
using PinballApi;
using PinballApi.Models.WPPR.v1.Calendar;

namespace IFPACompanionDiscord.Commands
{
    [SlashCommandGroup("ifpa", "IFPA Pinball Data")]
    public class IfpaSlashCommand : ApplicationCommandModule
    {
        public PinballRankingApiV2 IFPAApi { get; set; }
        public PinballRankingApiV1 IFPALegacyApi { get; set; }

        //[SlashCommandGroup("rank", "Retrieve top 40 IFPA ranked players for a ranking type (women, youth, main, country)")]
        //public class RankCommand : ApplicationCommandModule
        //{
        //    [SlashCommand("command", "description")]
        //    public async Task Command(InteractionContext ctx) { }

        //    [SlashCommand("command2", "description")]
        //    public async Task Command2(InteractionContext ctx) { }
        //}

        //[SlashCommandGroup("player", "description")]
        //public class PlayerCommand : ApplicationCommandModule
        //{
        //    [SlashCommand("command", "description")]
        //    public async Task Command(InteractionContext ctx) { }

        //    [SlashCommand("command2", "description")]
        //    public async Task Command2(InteractionContext ctx) { }
        //}

        [SlashCommand("tournaments", "Retrieve upcoming tournaments by location")]
        public async Task TournamentsCommand(InteractionContext ctx, 
                                            [Option("Radius","Radius in miles from the location")] long radiusInMiles,
                                            [Option("Location", "Location to search tournaments near")] string location) 
        {
            var tournaments = await IFPALegacyApi.GetCalendarSearch(location, (int)radiusInMiles, DistanceUnit.Miles);

            if (tournaments.Calendar != null)
            {
                var table = new ConsoleTable("Tournament", "Date", "Location", "");

                foreach (var tournament in tournaments.Calendar)
                {
                    table.AddRow(tournament.TournamentName,
                                 tournament.StartDate.ToShortDateString(),
                                 tournament.City,
                                 tournament.Website);
                }

                var responseTable = table.ToMinimalString();
                responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950)).RemoveCharactersAfterLastOccurrence('\n');

                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Upcoming tournaments near {location}\n```{responseTable}```"));
            }
            else
            {
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"No upcoming tournaments near {location}"));
            }
        }
    }
}
