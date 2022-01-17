using DSharpPlus.Entities;

namespace IFPACompanionDiscord
{
    public class IFPACommand
    {
        public IFPACommandType Type { get; set; }

        public IFPACommand(DiscordMessage message)
        {

        }
    }
}
