using Microsoft.EntityFrameworkCore;
using TelegramNutritionMockBot.Data;

namespace TelegramNutritionMockBot.Services;

public sealed class MealRepository : IMealRepository
{
    private readonly AppDbContext _db;

    public MealRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddMealAsync(long telegramChatId, DateOnly date, string mealName, int calories, int protein, int fat, int carbs, string? photoFileId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramChatId == telegramChatId, cancellationToken);
        if (user is null)
            return;

        _db.Meals.Add(new MealEntity
        {
            UserId = user.Id,
            Date = date,
            MealName = mealName,
            Calories = calories,
            Protein = protein,
            Fat = fat,
            Carbs = carbs,
            Photo = photoFileId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
