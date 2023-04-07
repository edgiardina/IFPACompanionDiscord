// See https://aka.ms/new-console-template for more information
using IFPACompanionDiscord;
using Microsoft.Extensions.Configuration;
using System.Globalization;

Console.WriteLine("Starting IFPA Companion Discord Bot");

//Culture is set explicitly because the IFPA values returned are in US Dollars
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

var builder = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
IConfiguration config = builder.Build();

var ifpaBot = new IFPABot(config["Discord:Token"], config["PinballApi:IFPAApiKey"], config["OPDB:OPDBToken"]);

await ifpaBot.Run();