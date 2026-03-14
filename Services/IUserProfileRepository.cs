using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default);
    Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
