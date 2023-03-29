using ConsoleTables;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using IFPACompanionDiscord.Extensions;
using PinballApi;
using PinballApi.Extensions;
using PinballApi.Models.WPPR.v1.Calendar;
using PinballApi.Models.WPPR.v2.Rankings;

namespace IFPACompanionDiscord.Commands
{
    [Obsolete("this used raw string parsing of the prefix '/ifpa'. The slash command implementation is preferred.")]
    public class IfpaCommand : BaseCommandModule
    {
        public PinballRankingApiV2 IFPAApi { private get; set; }
        public PinballRankingApiV1 IFPALegacyApi { private get; set; }

        #region rank
        [Command("rank"), Aliases("ranking")]
        [Description("Retrieve top 40 IFPA ranked players for a ranking type (women, youth, main, country)")]
        public async Task RankCommand(CommandContext ctx)
        {
            var rankings = await IFPAApi.GetWpprRanking(1, 40);

            var table = new ConsoleTable("Rank", "Player", "Points");

            var index = 1;
            foreach (var ranking in rankings.Rankings)
            {
                table.AddRow(index,
                             ranking.FirstName + " " + ranking.LastName,
                             ranking.WpprPoints.ToString("N2"));
                index++;
            }

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));

            await ctx.Message.RespondAsync($"Top of the current IFPA rankings\n```{responseTable}```");
        }

        [Command("rank")]
        [Description("Retrieve top 40 IFPA ranked players for a ranking type (women, youth, main, country)")]
        public async Task RankCommand(CommandContext ctx,
                                      [Description("A ranking type (women, youth, main)")] string rankingType)
        {
            WomensRanking womensRanking = null;
            YouthRanking youthRanking = null;

            if (rankingType.ToLower() != "youth" && rankingType.ToLower() != "women" && rankingType.ToLower() != "main")
            {
                await ctx.Message.RespondAsync($"Invalid ranking type {rankingType}");
                return;
            }

            if (rankingType.ToLower() == "main")
            {
                await RankCommand(ctx);
                return;
            }

            var table = new ConsoleTable("Rank", "Player", "Points");

            var index = 1;

            if (rankingType.ToLower() == "women")
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
            else if (rankingType.ToLower() == "youth")
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

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));

            await ctx.Message.RespondAsync($"Top of the current IFPA {rankingType} rankings\n```{responseTable}```");
        }

        [Command("rank")]
        [Description("Retrieve top 40 IFPA ranked players for a ranking type (women, youth, main, country)")]
        public async Task RankCommand(CommandContext ctx,
                                     [Description("'country'")] string rankingType,
                                     [RemainingText][Description("Country Name")] string countryName)
        {
            var table = new ConsoleTable("Rank", "Player", "Points");

            var index = 1;

            var countryRankings = await IFPAApi.GetRankingForCountry(countryName, 1, 40);

            foreach (var ranking in countryRankings.Rankings)
            {
                table.AddRow(index,
                             ranking.FirstName + " " + ranking.LastName,
                             ranking.WpprPoints.ToString("N2"));
                index++;
            }

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));

            await ctx.Message.RespondAsync($"Top of the current IFPA rankings for {countryName}\n```{responseTable}```");
        }

        #endregion

        #region championshipseries
        [Command("nacs")]
        [Description("Retrieve NACS Ranking Data")]
        public async Task NacsCommand(CommandContext ctx)
        {
            await NacsCommand(ctx, DateTime.Now.Year);
        }

        [Command("nacs")]
        [Description("Retrieve NACS Ranking Data")]
        public async Task NacsCommand(CommandContext ctx, [Description("Year of the NACS data to show")] int year)
        {
            await ChampionshipSeriesOverallDataCommand(ctx, "NACS", year);
        }

        [Command("nacs")]
        [Description("Retrieve NACS Ranking Data")]
        public async Task NacsCommand(CommandContext ctx,
                                     [Description("Year of the NACS data to show")] int year,
                                     [Description("Two letter Region Code")] string region)
        {
            await ChampionshipSeriesRankingDataCommand(ctx, "NACS", year, region);
        }

        [Command("acs")]
        [Description("Retrieve ACS Ranking Data")]
        public async Task AcsCommand(CommandContext ctx)
        {
            await AcsCommand(ctx, DateTime.Now.Year);
        }

        [Command("acs")]
        [Description("Retrieve ACS Ranking Data")]
        public async Task AcsCommand(CommandContext ctx, [Description("Year of the ACS data to show")] int year)
        {
            await ChampionshipSeriesOverallDataCommand(ctx, "ACS", year);
        }

        [Command("acs")]
        [Description("Retrieve ACS Ranking Data")]
        public async Task AcsCommand(CommandContext ctx,
                                     [Description("Year of the ACS data to show")] int year,
                                     [Description("Two letter Region Code")] string region)
        {
            await ChampionshipSeriesRankingDataCommand(ctx, "ACS", year, region);
        }

        [Command("wnacso")]
        [Description("Retrieve Women's CS (Open) Ranking Data")]
        public async Task WnacsoCommand(CommandContext ctx)
        {
            await WnacsoCommand(ctx, DateTime.Now.Year);
        }

        [Command("wnacso")]
        [Description("Retrieve Women's CS (Open) Ranking Data")]
        public async Task WnacsoCommand(CommandContext ctx, [Description("Year of the WNACSO data to show")] int year)
        {
            await ChampionshipSeriesOverallDataCommand(ctx, "WNACSO", year);
        }

        [Command("wnacso")]
        [Description("Retrieve Women's CS (Open) Ranking Data")]
        public async Task WnacsoCommand(CommandContext ctx,
                                     [Description("Year of the WNACSO data to show")] int year,
                                     [Description("Two letter Region Code")] string region)
        {
            await ChampionshipSeriesRankingDataCommand(ctx, "WNACSO", year, region);
        }

        [Command("wnacsw")]
        [Description("Retrieve Women's CS (Women) Ranking Data")]
        public async Task WnacswCommand(CommandContext ctx)
        {
            await WnacswCommand(ctx, DateTime.Now.Year);
        }

        [Command("wnacsw")]
        [Description("Retrieve Women's CS (Women) Ranking Data")]
        public async Task WnacswCommand(CommandContext ctx, [Description("Year of the WNACSW data to show")] int year)
        {
            await ChampionshipSeriesOverallDataCommand(ctx, "WNACSW", year);
        }

        [Command("wnacsw")]
        [Description("Retrieve Women's CS (Women) Ranking Data")]
        public async Task WnacswCommand(CommandContext ctx,
                                     [Description("Year of the WNACSW data to show")] int year,
                                     [Description("Two letter Region Code")] string region)
        {
            await ChampionshipSeriesRankingDataCommand(ctx, "WNACSW", year, region);
        }

        private async Task ChampionshipSeriesOverallDataCommand(CommandContext ctx, string seriesCode, int year)
        {
            var seriesRanking = await IFPAApi.GetSeriesOverallStanding(seriesCode);

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
            await ctx.Message.RespondAsync($"{seriesCode} IFPA standings for {year}\n```{responseTable}```");
        }

        private async Task ChampionshipSeriesRankingDataCommand(CommandContext ctx, string seriesCode, int year, string region)
        {
            var championshipSeries = await IFPAApi.GetSeriesStandingsForRegion(seriesCode, region, year);

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
                await ctx.Message.RespondAsync($"{seriesCode} IFPA standings for {year} in {region}\n```{responseTable}```");
            }
            else
            {
                await ctx.Message.RespondAsync($"Region `{region}` returned no results");
            }
        }

        #endregion

        #region player
        [Command("player")]
        [Description("Search for a player by name or IFPA number")]
        public async Task PlayerCommand(CommandContext ctx, [RemainingText][Description("Name to search for")] string searchString)
        {
            var isNumeric = int.TryParse(searchString, out var playerId);

            if (isNumeric)
            {
                await PlayerCommand(ctx, playerId);
                return;
            }

            var players = await IFPALegacyApi.SearchForPlayerByName(searchString);

            var player = players.Search.FirstOrDefault();

            if (player != null)
            {
                await PlayerCommand(ctx, player.PlayerId);
            }
            else
            {
                await ctx.Message.RespondAsync($"Found no players matching `{searchString}`");
            }
        }

        [Command("player")]
        [Description("Search for a player by name or IFPA number")]
        public async Task PlayerCommand(CommandContext ctx, [Description("Player's IFPA number")] int playerId)
        {
            var playerDetails = await IFPAApi.GetPlayer(playerId);         

            var playerTourneyResults = await IFPAApi.GetPlayerResults(playerDetails.PlayerId);
                       
            var embed = new DiscordEmbedBuilder()
                         .WithTitle(playerDetails.FirstName + " " + playerDetails.LastName)
                         .WithUrl($"https://www.ifpapinball.com/player.php?p={playerId}")
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

            if(playerDetails.ProfilePhoto != null)
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

            await ctx.Message.RespondAsync(embed);
        }

        #endregion

        #region tournaments

        [Command("tournaments"), Aliases("tourney")]
        [Description("Retrieve upcoming tournaments by location")]
        public async Task TournamentsCommand(CommandContext ctx,                                             
                                            [Description("Radius in miles from the location")]int radiusInMiles,
                                            [RemainingText][Description("Location to search tournaments near")] string location)
        {
            var tournaments = await IFPALegacyApi.GetCalendarSearch(location, radiusInMiles, DistanceUnit.Miles);

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

                await ctx.Message.RespondAsync($"Upcoming tournaments near {location}\n```{responseTable}```");
            }
            else
            {
                await ctx.Message.RespondAsync($"No upcoming tournaments near {location}");
            }
        }

        #endregion
    }
}
