using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace IFPACompanionDiscord
{
    public class IFPABot
    {
        private string Token;

        public IFPABot(string token)
        {
            Token = token;
        }

        /// <summary>
        /// var botUrl = "https://discord.com/api/oauth2/authorize?client_id=932451797749075998&permissions=84992&scope=bot";
        /// </summary>
        /// <returns></returns>
        public async Task Run()
        {
            var discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = this.Token,
                TokenType = TokenType.Bot
            });

            discordClient.MessageCreated += DiscordClient_MessageCreated;

            await discordClient.ConnectAsync();
            await Task.Delay(-1);
        }

        private async Task DiscordClient_MessageCreated(DiscordClient sender, MessageCreateEventArgs e)
        {
            if(e.Message.Content.StartsWith("/ifpa"))
            {
                await ProcessIFPACommand(e.Message);
            }
        }

        private async Task ProcessIFPACommand(DiscordMessage message)
        {
            await message.RespondAsync($"you typed command {message.Content}");
        }

    }
}
