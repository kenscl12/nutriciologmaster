using Microsoft.EntityFrameworkCore;
using TelegramNutritionMockBot.Data;
using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _db;

    public UserProfileRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<UserProfile?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default)
    {
        var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.TelegramChatId == chatId, cancellationToken);
        return u is null ? null : MapToProfile(u);
    }

    public async Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        var daily = DailyGoalCalculator.Calculate(profile);
        var u = await _db.Users.FirstOrDefaultAsync(x => x.TelegramChatId == profile.ChatId, cancellationToken);
        if (u is null)
        {
            u = new UserEntity
            {
                TelegramChatId = profile.ChatId,
                Goal = profile.Goal.ToString(),
                Gender = profile.Gender.ToString(),
                Age = profile.Age,
                Height = profile.HeightCm,
                Weight = profile.WeightKg,
                Activity = profile.ActivityLevel.ToString(),
                DailyCalories = daily.Calories,
                ProteinTarget = daily.ProteinGrams,
                FatTarget = daily.FatGrams,
                CarbTarget = daily.CarbsGrams,
                StreakDays = 0
            };
            _db.Users.Add(u);
        }
        else
        {
            u.Goal = profile.Goal.ToString();
            u.Gender = profile.Gender.ToString();
            u.Age = profile.Age;
            u.Height = profile.HeightCm;
            u.Weight = profile.WeightKg;
            u.Activity = profile.ActivityLevel.ToString();
            u.DailyCalories = daily.Calories;
            u.ProteinTarget = daily.ProteinGrams;
            u.FatTarget = daily.FatGrams;
            u.CarbTarget = daily.CarbsGrams;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static UserProfile MapToProfile(UserEntity u)
    {
        return new UserProfile
        {
            ChatId = u.TelegramChatId,
            Gender = Enum.Parse<Gender>(u.Gender),
            Age = u.Age,
            HeightCm = u.Height,
            WeightKg = u.Weight,
            Goal = Enum.Parse<Goal>(u.Goal),
            ActivityLevel = Enum.Parse<ActivityLevel>(u.Activity)
        };
    }
}
