namespace TelegramNutritionMockBot.Models;

/// <summary>
/// Результат mock-анализа питательности блюда.
/// </summary>
public sealed class MockNutritionResult
{
    public string DishName { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Fat { get; set; }
    public int Carbs { get; set; }
    public string Comment { get; set; } = string.Empty;
}
