using ConsoleTables;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using IFPACompanionDiscord.Extensions;
using PinballApi;
using PinballApi.Models.WPPR.v1.Calendar;
using PinballApi.Models.WPPR.v2.Rankings;
using PinballApi.Models.WPPR.v2.Players;
using DSharpPlus;
using PinballApi.Extensions;
using IFPACompanionDiscord.Commands.ChoiceProviders;

namespace IFPACompanionDiscord.Commands
{
    [SlashCommandGroup("ifpa", "IFPA Pinball Data")]
    public class IfpaSlashCommand : ApplicationCommandModule
    {
        public PinballRankingApiV2 IFPAApi { get; set; }
        public PinballRankingApiV1 IFPALegacyApi { get; set; }

        [SlashCommand("series", "Retrieve Championship Series Ranking Data")]
        public async Task SeriesCommand(InteractionContext ctx,
                                    [ChoiceProvider(typeof(SeriesChoiceProvider))]
                                    [Option("series", "Championship Series")] string series,
                                    [Option("year", "Year")] long? year = null,
                                    [Option("region", "Two-letter region code")] string region = null)
        {
            if (year == null)
                year = DateTime.Now.Year;

            if(string.IsNullOrWhiteSpace(region))
            {
                var seriesRanking = await IFPAApi.GetSeriesOverallStanding(series, (int)year);

                var table = new ConsoleTable("Location", "Current Leader", "# Players", "Prize $");

                foreach (var ranking in seriesRanking.OverallResults.Take(40))
                {
                    table.AddRow(ranking.RegionName,
                                 ranking.CurrentLeader.PlayerName,
                                 ranking.UniquePlayerCount,
                                 ranking.PrizeFund.ToString("c"));
                }

                var responseTable = table.ToMinimalString();
                responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{series} IFPA standings for {year}\n```{responseTable}```"));
            }
            else
            {
                var championshipSeries = await IFPAApi.GetSeriesStandingsForRegion(series, region, (int)year);

                var table = new ConsoleTable("Rank", "Player", "Points", "# Events");
                table.Options.NumberAlignment = Alignment.Right;
                if (championshipSeries.Standings != null)
                {
                    foreach (var ranking in championshipSeries.Standings.Take(40))
                    {
                        table.AddRow(ranking.SeriesRank, ranking.PlayerName, ranking.WpprPoints, ranking.EventCount);
                    }
                    var responseTable = table.ToMinimalString();
                    responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950)).RemoveCharactersAfterLastOccurrence('\n');
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{series} IFPA standings for {year} in {region}\n```{responseTable}```"));
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Region `{region}` returned no results"));
                }
            }
        }

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
                if (players.Results.Count > 0)
                {
                    playerDetails = await IFPAApi.GetPlayer(players.Results.First().PlayerId);
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Search Criteria did not find any players"));
                    return;
                }
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
