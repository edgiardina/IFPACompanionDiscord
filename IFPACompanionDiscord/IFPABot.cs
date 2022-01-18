using ConsoleTables;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using IFPACompanionDiscord.Commands;
using Microsoft.Extensions.DependencyInjection;
using PinballApi;
using PinballApi.Extensions;
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

            var services = new ServiceCollection()
                                .AddSingleton<PinballRankingApiV2>(IFPAApi)
                                .AddSingleton<PinballRankingApiV1>(IFPALegacyApi)
                                .BuildServiceProvider();

            //discordClient.MessageCreated += DiscordClient_MessageCreated;
            var commands = discordClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "/ifpa" },
                Services = services
            });

            commands.RegisterCommands<IfpaCommand>();

            await discordClient.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
