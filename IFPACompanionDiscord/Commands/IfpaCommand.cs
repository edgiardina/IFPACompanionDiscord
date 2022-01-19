using ConsoleTables;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using PinballApi;
using PinballApi.Extensions;
using PinballApi.Models.WPPR.v2.Rankings;

namespace IFPACompanionDiscord.Commands
{
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

            if (rankingType.ToLower() == "women")
            {
                womensRanking = await IFPAApi.GetRankingForWomen(TournamentType.Open, 1, 40);
            }
            else if (rankingType.ToLower() == "youth")
            {
                youthRanking = await IFPAApi.GetRankingForYouth(1, 40);
            }

            var table = new ConsoleTable("Rank", "Player", "Points");

            var index = 1;
            foreach (var ranking in womensRanking?.Rankings ?? youthRanking.Rankings)
            {
                table.AddRow(index,
                             ranking.FirstName + " " + ranking.LastName,
                             ranking.WpprPoints.ToString("N2"));
                index++;
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

        #region nacs
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
            var nacsRankings = await IFPAApi.GetNacsStandings(year);

            var table = new ConsoleTable("Location", "Current Leader", "# Players", "Prize $");

            foreach (var ranking in nacsRankings)
            {
                table.AddRow(ranking.StateProvinceName,
                             ranking.CurrentLeader.FirstName + " " + ranking.CurrentLeader.LastName,
                             ranking.UniquePlayerCount,
                             ranking.PrizeValue.ToString("c"));
            }

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));
            await ctx.Message.RespondAsync($"NACS IFPA standings for {year}\n```{responseTable}```");
        }

        [Command("nacs")]
        [Description("Retrieve NACS Ranking Data")]
        public async Task NacsCommand(CommandContext ctx,
                                     [Description("Year of the NACS data to show")] int year,
                                     [Description("Two letter State or Province abbreviation")] string stateProvince)
        {
            var nacsStateProvRankings = await IFPAApi.GetNacsStateProvinceStandings(stateProvince, year);

            var table = new ConsoleTable("Rank", "Player", "Points", "# Events");
            table.Options.NumberAlignment = Alignment.Right;
            if (nacsStateProvRankings.PlayerStandings != null)
            {
                foreach (var ranking in nacsStateProvRankings.PlayerStandings)
                {
                    table.AddRow(ranking.Position, ranking.FirstName + " " + ranking.LastName, ranking.WpprPoints, ranking.EventCount);
                }
                var responseTable = table.ToMinimalString();
                responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));
                await ctx.Message.RespondAsync($"NACS IFPA standings for {year} in {stateProvince}\n```{responseTable}```");
            }
            else
            {
                await ctx.Message.RespondAsync($"State/Province `{stateProvince}` returned no results");
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

            var table = new ConsoleTable(playerDetails.FirstName + " " + playerDetails.LastName, string.Empty, string.Empty);

            table.AddRow("Ranking", $"{playerDetails.PlayerStats.CurrentWpprRank.OrdinalSuffix()}", playerDetails.PlayerStats.CurrentWpprValue);
            table.AddRow("Rating", playerDetails.PlayerStats.RatingsRank?.OrdinalSuffix(), playerDetails.PlayerStats.RatingsValue);
            table.AddRow("Eff percent", playerDetails.PlayerStats.EfficiencyRank?.OrdinalSuffix(), playerDetails.PlayerStats.EfficiencyValue);

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));

            var resultsTable = new ConsoleTable("Tournament", "Event", "Result", "Date", "Points");
            resultsTable.Options.NumberAlignment = Alignment.Right;
            var playerTourneyResults = await IFPAApi.GetPlayerResults(playerDetails.PlayerId);

            string resultsTableFormatted;
            if (playerTourneyResults.Results != null)
            {
                foreach (var result in playerTourneyResults.Results)
                {
                    resultsTable.AddRow(result.TournamentName, result.EventName, result.Position.OrdinalSuffix(), result.EventDate.ToShortDateString(), result.CurrentPoints);
                }
                resultsTableFormatted = resultsTable.ToMinimalString();
            }
            else
            {
                resultsTableFormatted = "Player has no results";
            }

            resultsTableFormatted = resultsTableFormatted.Substring(0, Math.Min(resultsTableFormatted.Length, 1500));
            await ctx.Message.RespondAsync($"Found the following player\n" +
                                       $"https://www.ifpapinball.com/player.php?p={playerId}\n" +
                                       $"```{responseTable}\n" +
                                       $"{resultsTableFormatted}```");
        }

        #endregion
    }
}
