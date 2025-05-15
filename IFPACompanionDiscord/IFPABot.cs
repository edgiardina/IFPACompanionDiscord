using DSharpPlus;
using DSharpPlus.SlashCommands;
using IFPACompanionDiscord.Commands;
using IFPACompanionDiscord.Geocoding;
using Microsoft.Extensions.DependencyInjection;
using PinballApi;
using PinballApi.Interfaces;

namespace IFPACompanionDiscord
{
    public class IFPABot
    {
        private string DiscordToken;
        private string IFPAApiKey;

        private IPinballRankingApi IFPAApi { get; set; }

        public IFPABot(string discordToken, string apiKey, string opdbToken)
        {
            DiscordToken = discordToken;
            IFPAApiKey = apiKey;
            IFPAApi = new PinballRankingApi(IFPAApiKey);
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
                TokenType = TokenType.Bot,
                MinimumLogLevel = Microsoft.Extensions.Logging.LogLevel.Debug
            });

            var serviceCollection = new ServiceCollection();

            serviceCollection.AddSingleton<IPinballRankingApi>(IFPAApi);
            serviceCollection.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            var slashCommands = discordClient.UseSlashCommands(new SlashCommandsConfiguration
            {
                Services = serviceProvider
            });
            slashCommands.RegisterCommands<IfpaSlashCommand>();

            await discordClient.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}
