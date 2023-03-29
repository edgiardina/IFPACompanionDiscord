using ConsoleTables;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using IFPACompanionDiscord.Commands;
using Microsoft.Extensions.DependencyInjection;
using PinballApi;
using PinballApi.Extensions;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.v2.Players;
using System.Globalization;

namespace IFPACompanionDiscord
{
    public class IFPABot
    {
        private string DiscordToken;
        private string IFPAApiKey;
        private string OpdbToken;

        private PinballRankingApiV2 IFPAApi { get; set; }
        private PinballRankingApiV1 IFPALegacyApi { get; set; }
        private OPDBApi OPDBApi { get; set; }

        public IFPABot(string discordToken, string apiKey, string opdbToken)
        {
            DiscordToken = discordToken;
            IFPAApiKey = apiKey;
            OpdbToken = opdbToken;
            IFPAApi = new PinballRankingApiV2(IFPAApiKey);
            IFPALegacyApi = new PinballRankingApiV1(IFPAApiKey);
            OPDBApi = new OPDBApi(OpdbToken);
        }

        /// <summary>
        /// Start the IFPA Discord Companion Bot and listen infinitely for `/ifpa` messages
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            var discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = this.DiscordToken,
                TokenType = TokenType.Bot
            });

            var services = new ServiceCollection()
                                .AddSingleton<PinballRankingApiV2>(IFPAApi)
                                .AddSingleton<PinballRankingApiV1>(IFPALegacyApi)
                                .AddSingleton<IOpdbApi>(OPDBApi)
                                .BuildServiceProvider();

            //discordClient.MessageCreated += DiscordClient_MessageCreated;
            var commands = discordClient.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "/ifpa", "/opdb" },
                Services = services
            });

            commands.RegisterCommands<IfpaCommand>();
            commands.RegisterCommands<OpdbCommand>();

            await discordClient.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
