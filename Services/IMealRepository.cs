namespace TelegramNutritionMockBot.Services;

public sealed record MealEntry(string MealName, int Calories, int Protein, int Fat, int Carbs);

public interface IMealRepository
{
    Task AddMealAsync(long telegramChatId, DateOnly date, string mealName, int calories, int protein, int fat, int carbs, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MealEntry>> GetMealsForDateAsync(long telegramChatId, DateOnly date, CancellationToken cancellationToken = default);
}
