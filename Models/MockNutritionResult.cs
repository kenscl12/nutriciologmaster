using System.Text.Json.Serialization;

namespace TelegramNutritionMockBot.Models;

/// <summary>
/// Результат анализа питательности блюда (mock или от ChatGPT).
/// </summary>
public sealed class MockNutritionResult
{
    [JsonPropertyName("dishName")]
    public string DishName { get; set; } = string.Empty;

    [JsonPropertyName("weightGrams")]
    public int WeightGrams { get; set; }

    [JsonPropertyName("calories")]
    public int Calories { get; set; }

    [JsonPropertyName("protein")]
    public int Protein { get; set; }

    [JsonPropertyName("fat")]
    public int Fat { get; set; }

    [JsonPropertyName("carbs")]
    public int Carbs { get; set; }

    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;
}
