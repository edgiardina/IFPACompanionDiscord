using DSharpPlus.Entities;

namespace IFPACompanionDiscord
{
    public class IFPACommand
    {
        public IFPACommandType Type { get; set; }

        private DiscordMessage Message { get; set; }

        public IFPACommand(DiscordMessage message)
        {
            this.Message = message;
        }

        public async Task Render()
        {
            throw new NotImplementedException();
        }
    }
}
