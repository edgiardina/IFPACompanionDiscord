using ConsoleTables;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using IFPACompanionDiscord.Commands.ChoiceProviders;
using IFPACompanionDiscord.Extensions;
using IFPACompanionDiscord.Geocoding;
using PinballApi.Extensions;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal.Players;
using PinballApi.Models.WPPR.Universal.Rankings;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace IFPACompanionDiscord.Commands;

[SlashCommandGroup("ifpa", "IFPA Pinball Data")]
public class IfpaSlashCommand(IMemoryCache memoryCache, ILogger<IfpaSlashCommand> logger) : ApplicationCommandModule
{
    public IPinballRankingApi IFPAApi { get; set; } = null!;
    public IGeocodingService GeocodingService { get; set; } = null!;

    private readonly IAsyncCacheProvider _cache = new MemoryCacheProvider(memoryCache);
    private readonly IAsyncPolicy _retryPolicy = Policy
        .Handle<HttpRequestException>()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                logger.LogWarning("Retry {RetryCount} for {Operation} in {Delay}ms",
                    retryCount, context.OperationKey, timespan.TotalMilliseconds);
            });

    private const int DiscordCharacterLimit = 1950;

    [SlashCommand("series", "Retrieve Championship Series Ranking Data")]
    public async Task SeriesCommand(InteractionContext ctx,
                                [ChoiceProvider(typeof(SeriesChoiceProvider))]
                                [Option("series", "Championship Series")] string series,
                                [Option("year", "Year")] long? year = null,
                                [Option("region", "Two-letter region code")] string region = null)
    {
        year ??= DateTime.Now.Year;

        if (string.IsNullOrWhiteSpace(region))
        {
            var cacheKey = $"series_{series}_{year}";
            var cachePolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(30));
            var wrappedPolicy = Policy.WrapAsync(_retryPolicy, cachePolicy);

            var seriesRanking = await wrappedPolicy.ExecuteAsync(
                async (context) => await IFPAApi.GetSeriesOverallStanding(series, (int)year),
                new Context(cacheKey));

            if (seriesRanking == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"No data found for series `{series}` in {year}"));
                return;
            }

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
                    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent($"```{response}```"));
                }
                else
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"```{response}```"));
                }
            }
        }
        else
        {
            var cacheKey = $"series_{series}_{region}_{year}";
            var cachePolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(30));
            var wrappedPolicy = Policy.WrapAsync(_retryPolicy, cachePolicy);

            var championshipSeries = await wrappedPolicy.ExecuteAsync(
                async (context) => await IFPAApi.GetSeriesStandingsForRegion(series, region, (int)year),
                new Context(cacheKey));

            var table = new ConsoleTable("Rank", "Player", "Points", "# Events");
            table.Options.NumberAlignment = Alignment.Right;

            if (championshipSeries?.Standings != null)
            {
                foreach (var ranking in championshipSeries.Standings.Take(40))
                {
                    table.AddRow(ranking.SeriesRank, ranking.PlayerName, ranking.WpprPoints, ranking.EventCount);
                }
                await SendTableResponseAsync(ctx, table, $"{series} IFPA standings for {year} in {region}");
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent($"Region `{region}` returned no results"));
            }
        }
    }

    [SlashCommand("rank", "Retrieve top 40 IFPA ranked players for a ranking type (women, youth, main, country)")]
    public async Task RankCommand(InteractionContext ctx,
                                [Option("ranktype", "Ranking Type")] RankType rankType = RankType.Main,
                                [Option("country", "Country")] string? countryName = null)
    {
        var cacheKey = $"rankings_{rankType}_{countryName ?? "global"}";
        var cachePolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(15));
        var wrappedPolicy = Policy.WrapAsync(_retryPolicy, cachePolicy);

        var rankings = await wrappedPolicy.ExecuteAsync(async (context) =>
        {
            var apiRankingType = rankType switch
            {
                RankType.Main => RankingType.Wppr,
                RankType.Women => RankingType.Women,
                RankType.Youth => RankingType.Youth,
                _ => RankingType.Wppr
            };

            return await IFPAApi.RankingSearch(apiRankingType, count: 40, startPosition: 1, countryCode: countryName);
        }, new Context(cacheKey));

        if (rankings == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Unable to retrieve ranking data"));
            return;
        }

        var table = new ConsoleTable("Rank", "Player", "Points");
        var index = 1;

        foreach (var ranking in rankings.Rankings)
        {
            table.AddRow(index, ranking.Name, ranking.WpprPoints.ToString("N2"));
            index++;
        }

        await SendTableResponseAsync(ctx, table, $"Top of the current IFPA {rankType} rankings");
    }

    [SlashCommand("player", "Search for a player by name or IFPA number")]
    public async Task PlayerCommand(InteractionContext ctx,
                                  [Option("name", "Player Name")] string? name = null,
                                  [Option("ifpanumber", "Player IFPA Number")] long? ifpaNumber = null)
    {
        Player? playerDetails = null;

        if (ifpaNumber.HasValue)
        {
            var cacheKey = $"player_{ifpaNumber}";
            var cachePolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(30));
            var wrappedPolicy = Policy.WrapAsync(_retryPolicy, cachePolicy);

            playerDetails = await wrappedPolicy.ExecuteAsync(
                async (context) => await IFPAApi.GetPlayer((int)ifpaNumber),
                new Context(cacheKey));
        }
        else if (!string.IsNullOrWhiteSpace(name))
        {
            var players = await _retryPolicy.ExecuteAsync(() => IFPAApi.PlayerSearch(name));
            if (players.Results.Count > 0)
            {
                var cacheKey = $"player_{players.Results.First().PlayerId}";
                var cachePolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(30));
                var wrappedPolicy = Policy.WrapAsync(_retryPolicy, cachePolicy);

                playerDetails = await wrappedPolicy.ExecuteAsync(
                    async (context) => await IFPAApi.GetPlayer((int)players.Results.First().PlayerId),
                    new Context(cacheKey));
            }
            else
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent("Search criteria did not find any players").AsEphemeral());
                return;
            }
        }
        else
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Player Name or IFPA Number is required.").AsEphemeral());
            return;
        }

        if (playerDetails == null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent("Player not found").AsEphemeral());
            return;
        }

        var resultsCacheKey = $"player_results_{playerDetails.PlayerId}";
        var resultsPolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(15));
        var resultsWrappedPolicy = Policy.WrapAsync(_retryPolicy, resultsPolicy);

        var playerTourneyResults = await resultsWrappedPolicy.ExecuteAsync(
            async (context) => await IFPAApi.GetPlayerResults((int)playerDetails.PlayerId),
            new Context(resultsCacheKey));

        var embed = new DiscordEmbedBuilder()
            .WithTitle(playerDetails.FirstName + " " + playerDetails.LastName)
            .WithUrl($"https://www.ifpapinball.com/player.php?p={playerDetails.PlayerId}")
            .WithColor(new DiscordColor("#072C53"))
            .WithDescription($"IFPA #{playerDetails.PlayerId} [{playerDetails.Initials}]")
            .AddField("Location", playerDetails.City + " " + playerDetails.CountryName)
            .AddField("Ranking", $"{playerDetails.PlayerStats.Open.CurrentRank.OrdinalSuffix()}     {playerDetails.PlayerStats.Open.CurrentPoints:F2}", true)
            .AddField("Rating", $"{playerDetails.PlayerStats.Open.RatingsRank?.OrdinalSuffix()}     {playerDetails.PlayerStats.Open.RatingsValue?.ToString("F2")}", true)
            .AddField("Eff percent", $"{playerDetails.PlayerStats.Open.EfficiencyRank?.OrdinalSuffix() ?? "Not Ranked"}      {playerDetails.PlayerStats.Open.EfficiencyValue?.ToString("F2")}", true);

        if (playerDetails.IfpaRegistered)
        {
            embed.WithFooter("IFPA Registered", "https://www.ifpapinball.com/images/confirmed.png");
        }

        if (playerDetails.ProfilePhoto != null)
        {
            embed.WithThumbnail(playerDetails.ProfilePhoto);
        }

        if (playerTourneyResults?.Results != null)
        {
            foreach (var result in playerTourneyResults.Results.Take(5))
            {
                embed.AddField(result.TournamentName, $" \n{result.EventDate.ToShortDateString()}                        {result.EventName} \n **{result.Position.OrdinalSuffix()}**                                                     {result.CurrentPoints}");
            }
        }

        await ctx.CreateResponseAsync("", embed);
    }

    [SlashCommand("tournaments", "Retrieve upcoming tournaments by location")]
    public async Task TournamentsCommand(InteractionContext ctx,
                                       [Option("Radius", "Radius in miles from the location")] long radiusInMiles,
                                       [Option("Location", "Location to search tournaments near")] string location)
    {
        var coordinates = await _retryPolicy.ExecuteAsync(() => GeocodingService.GeocodeAsync(location));

        var cacheKey = $"tournaments_{coordinates.Latitude:F2}_{coordinates.Longitude:F2}_${radiusInMiles}";
        var cachePolicy = Policy.CacheAsync(_cache, TimeSpan.FromMinutes(60));
        var wrappedPolicy = Policy.WrapAsync(_retryPolicy, cachePolicy);

        var tournaments = await wrappedPolicy.ExecuteAsync(async (context) =>
            await IFPAApi.TournamentSearch(coordinates.Latitude, coordinates.Longitude, (int)radiusInMiles, DistanceType.Miles,
                                         startDate: DateTime.Now,
                                         endDate: DateTime.Now.AddYears(1)),
            new Context(cacheKey));

        if (tournaments?.Tournaments != null && tournaments.Tournaments.Any())
        {
            var table = new ConsoleTable("Tournament", "Date", "Location", "");
            foreach (var tournament in tournaments.Tournaments)
            {
                table.AddRow(tournament.TournamentName,
                             tournament.EventStartDate.UtcDateTime.ToShortDateString(),
                             tournament.City,
                             tournament.Website);
            }

            await SendTableResponseAsync(ctx, table, $"Upcoming tournaments near {location}");
        }
        else
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"No upcoming tournaments near {location}"));
        }
    }

    private async Task SendTableResponseAsync(InteractionContext ctx, ConsoleTable table, string title)
    {
        var responseTable = table.ToMinimalString();
        responseTable = TruncateToDiscordLimit(responseTable);

        await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().WithContent($"{title}\n```{responseTable}```"));
    }

    private static string TruncateToDiscordLimit(string content)
    {
        if (content.Length <= DiscordCharacterLimit)
            return content;

        return content.Substring(0, DiscordCharacterLimit).RemoveCharactersAfterLastOccurrence('\n');
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
}
