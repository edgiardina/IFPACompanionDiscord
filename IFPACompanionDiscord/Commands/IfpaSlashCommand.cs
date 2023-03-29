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
using PinballApi.Models.WPPR.v2.Rankings;
using PinballApi.Models.WPPR.v2;
using PinballApi.Models.WPPR.v2.Players;
using DSharpPlus;
using PinballApi.Extensions;

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

        public enum RankType
        {
            [ChoiceName("main")]
            Main,
            [ChoiceName("women")]
            Women,
            [ChoiceName("youth")]
            Youth,     
            [ChoiceName("country")]
            Country
        }

        [SlashCommand("rank", "Retrieve top 40 IFPA ranked players for a ranking type (women, youth, main, country)")]
        public async Task RankCommand(InteractionContext ctx,
                                            [Option("ranktype", "Ranking Type")] RankType rankType = RankType.Main,
                                            [Option("country", "Country")] string countryName = null)
        {
            WomensRanking womensRanking = null;
            YouthRanking youthRanking = null;

            var table = new ConsoleTable("Rank", "Player", "Points");
            var index = 1;

            if (rankType == RankType.Main)
            {
                var rankings = await IFPAApi.GetWpprRanking(1, 40);
                
                foreach (var ranking in rankings.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }  
            else if (rankType == RankType.Women)
            {
                womensRanking = await IFPAApi.GetRankingForWomen(TournamentType.Open, 1, 40);
                foreach (var ranking in womensRanking.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }
            else if (rankType == RankType.Youth)
            {
                youthRanking = await IFPAApi.GetRankingForYouth(1, 40);

                foreach (var ranking in youthRanking.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }
            else if(rankType == RankType.Country && countryName != null)
            {
                var countryRankings = await IFPAApi.GetRankingForCountry(countryName, 1, 40);

                foreach (var ranking in countryRankings.Rankings)
                {
                    table.AddRow(index,
                                 ranking.FirstName + " " + ranking.LastName,
                                 ranking.WpprPoints.ToString("N2"));
                    index++;
                }
            }

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Top of the current IFPA {rankType} rankings\n```{responseTable}```"));
        }

        [SlashCommand("player", "Search for a player by name or IFPA number")]
        public async Task PlayerCommand(InteractionContext ctx,
                                            [Option("name", "Player Name")] string name = null,
                                            [Option("ifpanumber", "Player IFPA Number")] long? ifpaNumber = null)
        {
            Player playerDetails = null;

            if (ifpaNumber.HasValue)
            {
                playerDetails = await IFPAApi.GetPlayer((int)ifpaNumber);
            }
            else if (String.IsNullOrWhiteSpace(name) == false)
            {
                var players = await IFPAApi.GetPlayersBySearch(new PlayerSearchFilter { Name = name });
                playerDetails = players.Results.FirstOrDefault();
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Player Name or IFPA Number is required."));
                return;
            }

            var playerTourneyResults = await IFPAApi.GetPlayerResults(playerDetails.PlayerId);

            var embed = new DiscordEmbedBuilder()
                         .WithTitle(playerDetails.FirstName + " " + playerDetails.LastName)
                         .WithUrl($"https://www.ifpapinball.com/player.php?p={playerDetails.PlayerId}")
                         .WithColor(new DiscordColor("#072C53"))
                         .WithDescription($"IFPA #{playerDetails.PlayerId} [{playerDetails.Initials}]")
                         .AddField("Location", playerDetails.City + " " + playerDetails.CountryName)
                         .AddField("Ranking", $"{playerDetails.PlayerStats.CurrentWpprRank.OrdinalSuffix()}     {playerDetails.PlayerStats.CurrentWpprValue.ToString("F2")}", true)
                         .AddField("Rating", $"{playerDetails.PlayerStats.RatingsRank?.OrdinalSuffix()}     {playerDetails.PlayerStats.RatingsValue?.ToString("F2")}", true)
                         .AddField("Eff percent", $"{playerDetails.PlayerStats.EfficiencyRank?.OrdinalSuffix() ?? "Not Ranked"}      {playerDetails.PlayerStats.EfficiencyValue?.ToString("F2")}", true);

            if (playerDetails.IfpaRegistered)
            {
                embed.WithFooter("IFPA Registered", "https://www.ifpapinball.com/images/confirmed.png");
            }

            if (playerDetails.ProfilePhoto != null)
            {
                embed.WithThumbnail(playerDetails.ProfilePhoto);
            }

            if (playerTourneyResults.Results != null)
            {
                foreach (var result in playerTourneyResults.Results.Take(5))
                {
                    embed.AddField(result.TournamentName, $" \n{result.EventDate.ToShortDateString()}                        {result.EventName} \n **{result.Position.OrdinalSuffix()}**                                                     {result.CurrentPoints}");
                }
            }
            
            await ctx.CreateResponseAsync("", embed);      
        }


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

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Upcoming tournaments near {location}\n```{responseTable}```"));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"No upcoming tournaments near {location}"));
            }
        }
    }
}
