using System.Collections.Concurrent;
using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

public sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly ConcurrentDictionary<long, UserProfile> _profiles = new();

    public Task<UserProfile?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_profiles.TryGetValue(chatId, out var p) ? p : null);
    }

    public Task SaveAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        _profiles[profile.ChatId] = profile;
        return Task.CompletedTask;
    }
}
