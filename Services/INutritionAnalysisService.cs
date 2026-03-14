using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Сервис анализа питательности блюда по фото (ChatGPT Vision или mock).
/// </summary>
public interface INutritionAnalysisService
{
    /// <summary>
    /// Анализирует фото блюда и возвращает оценку КБЖУ.
    /// </summary>
    Task<MockNutritionResult> AnalyzeDishAsync(Stream imageStream, string mimeType, CancellationToken cancellationToken = default);
}
