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
        [Command("rank")]
        [Description("Retrieve top 20 IFPA ranked players")]
        public async Task RankCommand(CommandContext ctx)
        {
            var rankings = await IFPAApi.GetWpprRanking(1, 20);

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
        [Description("Retrieve top 20 IFPA ranked players for a ranking type (women, youth)")]
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

            if(rankingType.ToLower() == "main")
            {
                await RankCommand(ctx);
                return;
            }

            if (rankingType.ToLower() == "women")
            {
                womensRanking = await IFPAApi.GetRankingForWomen(TournamentType.Open);
            }
            else if (rankingType.ToLower() == "youth")
            {
                youthRanking = await IFPAApi.GetRankingForYouth(1, 50);
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
                                     [Description("Year of the NACS data to show")]int year, 
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
        [Description("Search for a player by name")]
        public async Task PlayerCommand(CommandContext ctx, [RemainingText][Description("Name to search for")] string searchString)
        {
            var players = await IFPALegacyApi.SearchForPlayerByName(searchString);

            var player = players.Search.FirstOrDefault();

            if (player != null)
            {
                var playerDetails = await IFPAApi.GetPlayer(player.PlayerId);

                var table = new ConsoleTable(player.FirstName + " " + player.LastName, string.Empty, string.Empty);

                table.AddRow("Ranking", $"{player.WpprRank.OrdinalSuffix()}", playerDetails.PlayerStats.CurrentWpprValue);
                table.AddRow("Rating", playerDetails.PlayerStats.RatingsRank?.OrdinalSuffix(), playerDetails.PlayerStats.RatingsValue);
                table.AddRow("Eff percent", playerDetails.PlayerStats.EfficiencyRank?.OrdinalSuffix(), playerDetails.PlayerStats.EfficiencyValue);

                var responseTable = table.ToMinimalString();
                responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));

                var resultsTable = new ConsoleTable("Tournament", "Event", "Result", "Date", "Points");
                resultsTable.Options.NumberAlignment = Alignment.Right;
                var playerTourneyResults = await IFPAApi.GetPlayerResults(player.PlayerId);

                foreach (var result in playerTourneyResults.Results)
                {
                    resultsTable.AddRow(result.TournamentName, result.EventName, result.Position.OrdinalSuffix(), result.EventDate.ToShortDateString(), result.CurrentPoints);
                }

                var resultsTableFormatted = resultsTable.ToMinimalString();
                resultsTableFormatted = resultsTableFormatted.Substring(0, Math.Min(resultsTableFormatted.Length, 1500));
                await ctx.Message.RespondAsync($"Searched for {searchString} and found the following\n" +
                                           $"https://www.ifpapinball.com/player.php?p={player.PlayerId}\n" +
                                           $"```{responseTable}\n" +
                                           $"{resultsTableFormatted}```");

            }
            else
            {
                await ctx.Message.RespondAsync($"Found no players matching `{searchString}`");
            }
        }
        #endregion
    }
}
