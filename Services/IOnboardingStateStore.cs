using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

public interface IOnboardingStateStore
{
    Task<OnboardingState?> GetAsync(long chatId, CancellationToken cancellationToken = default);
    Task SetAsync(long chatId, OnboardingState state, CancellationToken cancellationToken = default);
    Task ClearAsync(long chatId, CancellationToken cancellationToken = default);
}
