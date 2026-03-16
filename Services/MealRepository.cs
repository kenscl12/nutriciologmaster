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

    public async Task AddMealAsync(long telegramChatId, DateOnly date, string mealName, int calories, int protein, int fat, int carbs, CancellationToken cancellationToken = default)
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
            Carbs = carbs
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MealEntry>> GetMealsForDateAsync(long telegramChatId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.TelegramChatId == telegramChatId, cancellationToken);
        if (user is null)
            return Array.Empty<MealEntry>();

        var list = await _db.Meals
            .AsNoTracking()
            .Where(m => m.UserId == user.Id && m.Date == date)
            .OrderBy(m => m.Id)
            .Select(m => new MealEntry(m.MealName, m.Calories, m.Protein, m.Fat, m.Carbs))
            .ToListAsync(cancellationToken);
        return list;
    }
}
