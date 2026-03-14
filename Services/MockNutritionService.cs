using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Mock-реализация сервиса анализа питательности.
/// </summary>
public sealed class MockNutritionService : IMockNutritionService
{
    public MockNutritionResult GetMockNutrition()
    {
        return new MockNutritionResult
        {
            DishName = "Курица с рисом",
            WeightGrams = 350,
            Calories = 520,
            Protein = 32,
            Fat = 14,
            Carbs = 58,
            Comment = "Оценка примерная, сейчас используется захардкоженный ответ."
        };
    }
}
