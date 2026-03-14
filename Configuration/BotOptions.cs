namespace TelegramNutritionMockBot.Configuration;

/// <summary>
/// Настройки Telegram-бота (секция "Bot" в appsettings).
/// </summary>
public sealed class BotOptions
{
    public const string SectionName = "Bot";

    /// <summary>
    /// Токен бота от @BotFather.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
