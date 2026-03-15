namespace TelegramNutritionMockBot.Data;

public sealed class UserEntity
{
    public long Id { get; set; }
    public long TelegramChatId { get; set; }
    public string Goal { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public int Age { get; set; }
    public int Height { get; set; }
    public double Weight { get; set; }
    public string Activity { get; set; } = string.Empty;
    public int DailyCalories { get; set; }
    public int ProteinTarget { get; set; }
    public int FatTarget { get; set; }
    public int CarbTarget { get; set; }
    public int StreakDays { get; set; }

    public ICollection<MealEntity> Meals { get; set; } = new List<MealEntity>();
}
