using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Сервис анализа питательности (пока mock).
/// </summary>
public interface IMockNutritionService
{
    MockNutritionResult GetMockNutrition();
}
