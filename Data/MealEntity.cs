namespace TelegramNutritionMockBot.Data;

public sealed class MealEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public UserEntity User { get; set; } = null!;
    public DateOnly Date { get; set; }
    public string MealName { get; set; } = string.Empty;
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Fat { get; set; }
    public int Carbs { get; set; }
    public string? Photo { get; set; }
}
