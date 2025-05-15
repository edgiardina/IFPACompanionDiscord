using ConsoleTables;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using IFPACompanionDiscord.Extensions;
using DSharpPlus;
using PinballApi.Extensions;
using IFPACompanionDiscord.Commands.ChoiceProviders;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Rankings;
using PinballApi.Models.WPPR.Universal.Players;
using PinballApi.Models.WPPR;
using IFPACompanionDiscord.Geocoding;

namespace IFPACompanionDiscord.Commands
{
    [SlashCommandGroup("ifpa", "IFPA Pinball Data")]
    public class IfpaSlashCommand : ApplicationCommandModule
    {
        public IPinballRankingApi IFPAApi { get; set; }
        public IGeocodingService GeocodingService { get; set; }

        private const int discordCharacterLimit = 1950;

        [SlashCommand("series", "Retrieve Championship Series Ranking Data")]
        public async Task SeriesCommand(InteractionContext ctx,
                                    [ChoiceProvider(typeof(SeriesChoiceProvider))]
                                    [Option("series", "Championship Series")] string series,
                                    [Option("year", "Year")] long? year = null,
                                    [Option("region", "Two-letter region code")] string region = null)
        {
            if (year == null)
                year = DateTime.Now.Year;

            if (string.IsNullOrWhiteSpace(region))
            {
                var seriesRanking = await IFPAApi.GetSeriesOverallStanding(series, (int)year);

                var table = new ConsoleTable("Location", "Current Leader", "# Players", "Prize $");

                foreach (var ranking in seriesRanking.OverallResults)
                {
                    table.AddRow(ranking.RegionName,
                                 ranking.CurrentLeader.PlayerName,
                                 ranking.UniquePlayerCount,
                                 ranking.PrizeFund.ToString("c"));
                }

                var responseTableAsString = table.ToMinimalString();
                var responseLines = responseTableAsString.Split('\n');
                var maxLineCount = 25;

                for (int i = 0; i < responseLines.Length; i += maxLineCount)
                {
                    var response = String.Join("\n", responseLines.Take(new Range(i, i + maxLineCount)));
                    if (i == 0)
                    {
                        response = $"{series} IFPA standings for {year}\n\n" + response;

                        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"```{response}```"));
                    }
                    else
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"```{response}```"));
                    }
                }
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
                    responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, discordCharacterLimit)).RemoveCharactersAfterLastOccurrence('\n');
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

            var table = new ConsoleTable("Rank", "Player", "Points");
            var index = 1;

            RankingType rankingType = RankingType.Wppr;
            string country = null;

            switch (rankType)
            {
                case RankType.Main:
                    rankingType = RankingType.Wppr;
                    break;
                case RankType.Women:
                    rankingType = RankingType.Women;
                    break;
                case RankType.Youth:
                    rankingType = RankingType.Youth;
                    break;
            }

            if (string.IsNullOrWhiteSpace(countryName) == false)
            {
                country = countryName;
            }

            var rankings = await IFPAApi.RankingSearch(rankingType, count: 40, startPosition: 1, countryCode: country);

            foreach (var ranking in rankings.Rankings)
            {
                table.AddRow(index,
                             ranking.Name,
                             ranking.WpprPoints.ToString("N2"));
                index++;
            }

            var responseTable = table.ToMinimalString();
            responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, discordCharacterLimit)).RemoveCharactersAfterLastOccurrence('\n');

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
                var players = await IFPAApi.PlayerSearch(name);
                if (players.Results.Count > 0)
                {
                    playerDetails = await IFPAApi.GetPlayer((int)players.Results.First().PlayerId);
                }
                else
                {
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Search Criteria did not find any players").AsEphemeral());
                    return;
                }
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Player Name or IFPA Number is required.").AsEphemeral());
                return;
            }

            var playerTourneyResults = await IFPAApi.GetPlayerResults((int)playerDetails.PlayerId);

            var embed = new DiscordEmbedBuilder()
                         .WithTitle(playerDetails.FirstName + " " + playerDetails.LastName)
                         .WithUrl($"https://www.ifpapinball.com/player.php?p={playerDetails.PlayerId}")
                         .WithColor(new DiscordColor("#072C53"))
                         .WithDescription($"IFPA #{playerDetails.PlayerId} [{playerDetails.Initials}]")
                         .AddField("Location", playerDetails.City + " " + playerDetails.CountryName)
                         .AddField("Ranking", $"{playerDetails.PlayerStats.Open.CurrentRank.OrdinalSuffix()}     {playerDetails.PlayerStats.Open.CurrentPoints.ToString("F2")}", true)
                         .AddField("Rating", $"{playerDetails.PlayerStats.Open.RatingsRank?.OrdinalSuffix()}     {playerDetails.PlayerStats.Open.RatingsValue?.ToString("F2")}", true)
                         .AddField("Eff percent", $"{playerDetails.PlayerStats.Open.EfficiencyRank?.OrdinalSuffix() ?? "Not Ranked"}      {playerDetails.PlayerStats.Open.EfficiencyValue?.ToString("F2")}", true);

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
                                            [Option("Radius", "Radius in miles from the location")] long radiusInMiles,
                                            [Option("Location", "Location to search tournaments near")] string location)
        {
            // todo: convert string location to lat/long
            var coordinates = await GeocodingService.GeocodeAsync(location);

            var tournaments = await IFPAApi.TournamentSearch(coordinates.Latitude, coordinates.Longitude, (int)radiusInMiles, DistanceType.Miles,
                                                             startDate: DateTime.Now,
                                                             endDate: DateTime.Now.AddYears(1));

            if (tournaments.Tournaments != null)
            {
                var table = new ConsoleTable("Tournament", "Date", "Location", "");

                foreach (var tournament in tournaments.Tournaments)
                {
                    table.AddRow(tournament.TournamentName,
                                 tournament.EventStartDate.UtcDateTime.ToShortDateString(),
                                 tournament.City,
                                 tournament.Website);
                }

                var responseTable = table.ToMinimalString();
                responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, discordCharacterLimit)).RemoveCharactersAfterLastOccurrence('\n');

                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Upcoming tournaments near {location}\n```{responseTable}```"));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"No upcoming tournaments near {location}"));
            }
        }
    }
}
