namespace TelegramNutritionMockBot.Services;

public interface IMealRepository
{
    Task AddMealAsync(long telegramChatId, DateOnly date, string mealName, int calories, int protein, int fat, int carbs, CancellationToken cancellationToken = default);
}
