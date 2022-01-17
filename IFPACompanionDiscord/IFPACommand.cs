using DSharpPlus.Entities;
using System.Globalization;

namespace IFPACompanionDiscord
{
    public abstract class IFPACommand
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

        public static bool IsValidCommand(DiscordMessage message)
        {
            var commandComponents = message.Content.Split(' ');
            var command = commandComponents[1];

            return Enum.IsDefined(typeof(IFPACommandType), CultureInfo.CurrentCulture.TextInfo.ToTitleCase(command.ToLower()));
        }
    }
}
