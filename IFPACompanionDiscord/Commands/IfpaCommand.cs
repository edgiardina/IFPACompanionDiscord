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

            foreach (var ranking in nacsRankings.Take(40))
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
                foreach (var ranking in nacsStateProvRankings.PlayerStandings.Take(40))
                {
                    table.AddRow(ranking.Position, ranking.FirstName + " " + ranking.LastName, ranking.WpprPoints, ranking.EventCount);
                }
                var responseTable = table.ToMinimalString();
                responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950)).RemoveCharactersAfterLastOccurrence('\n'); 
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
