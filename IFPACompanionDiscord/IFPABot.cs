using ConsoleTables;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using PinballApi;
using PinballApi.Models.WPPR.v2.Players;
using System.Globalization;

namespace IFPACompanionDiscord
{
    public class IFPABot
    {
        private string Token;
        private string IFPAApiKey;

        private PinballRankingApiV2 IFPAApi { get; set; }
        private PinballRankingApiV1 IFPALegacyApi { get; set; }

        private string HelpText { get; set; }

        public IFPABot(string token, string apiKey)
        {
            Token = token;
            IFPAApiKey = apiKey;
            IFPAApi = new PinballRankingApiV2(IFPAApiKey);
            IFPALegacyApi = new PinballRankingApiV1(IFPAApiKey);
        }

        /// <summary>
        /// Start the IFPA Discord Companion Bot and listen infinitely for `/ifpa` messages
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            var discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = this.Token,
                TokenType = TokenType.Bot
            });

            //todo: allow bot language changing.
            HelpText = await File.ReadAllTextAsync("Markdown/en-us/help.md");

            discordClient.MessageCreated += DiscordClient_MessageCreated;

            await discordClient.ConnectAsync();
            await Task.Delay(-1);
        }

        private async Task DiscordClient_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if (e.Message.Content.StartsWith("/ifpa"))
            {
                await ProcessDiscordMessage(e.Message);
            }
        }

        private async Task ProcessDiscordMessage(DiscordMessage message)
        {
            var commandComponents = message.Content.Split(' ');

            if (commandComponents.Length >= 2)
            {
                var command = commandComponents[1];

                if (Enum.IsDefined(typeof(IFPACommandType), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.ToLower())))
                {
                    await ProcessIFPACommand(commandComponents, message);
                }
                else
                {
                    await message.RespondAsync($"Command `{command}` isn't valid");
                }
            }
            else
            {
                await message.RespondAsync(HelpText);
            }
        }

        private async Task ProcessIFPACommand(string[] commandcomponents, DiscordMessage message)
        {
            if (commandcomponents[1].ToLower() == "rank")
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

                await message.RespondAsync($"Top of the current IFPA rankings\n```{responseTable}```");
            }
            else if (commandcomponents[1].ToLower() == "nacs")
            {
                int year = DateTime.Now.Year;
                if (commandcomponents.Length >= 3)
                {
                    var wasValidYear = int.TryParse(commandcomponents[2], out year);

                    //ignore invalid year params and continue with current year
                    if (!wasValidYear)
                    {
                        year = DateTime.Now.Year;
                    }
                }

                if (commandcomponents.Length >= 4)
                {
                    var stateProv = commandcomponents[3];
                    var nacsStateProvRankings = await IFPAApi.GetNacsStateProvinceStandings(stateProv, year);

                    var table = new ConsoleTable("Rank", "Player", "Points", "# Events");
                    if (nacsStateProvRankings.PlayerStandings != null)
                    {
                        foreach (var ranking in nacsStateProvRankings.PlayerStandings)
                        {
                            table.AddRow(ranking.Position, ranking.FirstName + " " + ranking.LastName, ranking.WpprPoints, ranking.EventCount);
                        }
                        var responseTable = table.ToMinimalString();
                        responseTable = responseTable.Substring(0, Math.Min(responseTable.Length, 1950));
                        await message.RespondAsync($"NACS IFPA standings for {year} in {stateProv}\n```{responseTable}```");
                    }
                    else
                    {
                        await message.RespondAsync($"State/Province `{stateProv}` returned no results");
                    }
                }
                else 
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
                    await message.RespondAsync($"NACS IFPA standings for {year}\n```{responseTable}```");
                }
            }
            else if (commandcomponents[1].ToLower() == "player")
            {
                if (commandcomponents.Length < 3) 
                {
                    await message.RespondAsync($"Command `player` requires a name parameter");
                }

                var searchString = string.Join(' ', commandcomponents.Skip(2));

                var players = await IFPALegacyApi.SearchForPlayerByName(searchString);

                var player = players.Search.FirstOrDefault();

                if (player != null)
                {
                    await message.RespondAsync($"Searched for {searchString} and found the following\n" +
                                               $"https://www.ifpapinball.com/player.php?p={player.PlayerId}");
                                               
                }
                else
                {
                    await message.RespondAsync($"Found no players matching `{searchString}`");
                }
            }
            else if (commandcomponents[1].ToLower() == "help")
            {
                await message.RespondAsync(HelpText);
            }
        }

    }
}
