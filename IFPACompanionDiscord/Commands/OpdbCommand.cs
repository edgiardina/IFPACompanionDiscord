using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using PinballApi.Interfaces;

namespace IFPACompanionDiscord.Commands
{
    public class OpdbCommand : BaseCommandModule
    {
        public IOpdbApi OpdbApi { private get; set; }

        [Command("machine")]
        [Description("Search for a Pinball Machine by name")]
        public async Task MachineCommand(CommandContext ctx, [RemainingText][Description("Pinball Machine Name")] string machineName)
        {
            var pinballMachine = (await OpdbApi.Search(machineName)).FirstOrDefault();

            var embed = new DiscordEmbedBuilder()
                         .WithTitle(pinballMachine.Name)
                         //.WithUrl($"https://www.ifpapinball.com/player.php?p={playerId}")
                         .WithColor(new DiscordColor("#072C53"))
                         .WithDescription(pinballMachine.Description)
                         .AddField("Manufacturer", pinballMachine.Manufacturer.Name)
                         .AddField("Manufacturer Date", pinballMachine.ManufactureDate.ToString("d"))
                         .AddField("Type", pinballMachine.Type)
                         .AddField("Display", pinballMachine.Display);


            if (pinballMachine.Images.Any())
            {
                embed.WithThumbnail(pinballMachine.Images.Single(n => n.Primary).Urls.Large);
            }

            await ctx.Message.RespondAsync(embed);
        }

    }
}
