using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Mock-реализация сервиса анализа питательности (игнорирует фото, возвращает тестовые данные).
/// </summary>
public sealed class MockNutritionService : INutritionAnalysisService
{
    public Task<MockNutritionResult> AnalyzeDishAsync(Stream imageStream, string mimeType, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MockNutritionResult
        {
            DishName = "Курица с рисом",
            WeightGrams = 350,
            Calories = 520,
            Protein = 32,
            Fat = 14,
            Carbs = 58,
            Comment = "Оценка примерная (mock)."
        });
    }
}
