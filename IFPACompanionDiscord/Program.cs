// See https://aka.ms/new-console-template for more information
using IFPACompanionDiscord;
using Microsoft.Extensions.Configuration;

Console.WriteLine("Starting IFPA Companion Discord Bot");

var builder = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
IConfiguration config = builder.Build();

var ifpaBot = new IFPABot(config["Discord:Token"], config["PinballApi:IFPAApiKey"], config["OPDB:OPDBToken"]);

await ifpaBot.Run();