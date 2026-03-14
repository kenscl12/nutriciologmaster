namespace TelegramNutritionMockBot.Configuration;

/// <summary>
/// Настройки OpenAI API (секция "OpenAI" в appsettings).
/// </summary>
public sealed class OpenAIOptions
{
    public const string SectionName = "OpenAI";

    /// <summary>
    /// API-ключ от platform.openai.com
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Модель с поддержкой зрения (например gpt-4o, gpt-4o-mini).
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";
}
