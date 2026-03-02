namespace IFPACompanionDiscord.Configuration;

public class BotConfiguration
{
    public DiscordSettings Discord { get; set; } = new();
    public PinballApiSettings PinballApi { get; set; } = new();
    public OpdbSettings OPDB { get; set; } = new();
}

public class DiscordSettings
{
    public string Token { get; set; } = string.Empty;
    public ulong? GuildId { get; set; }
    public string LogLevel { get; set; } = "Information";
}

public class PinballApiSettings
{
    public string IFPAApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public int RetryCount { get; set; } = 3;
}

public class OpdbSettings
{
    public string OPDBToken { get; set; } = string.Empty;
}