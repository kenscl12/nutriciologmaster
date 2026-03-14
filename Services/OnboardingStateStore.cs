using System.Collections.Concurrent;
using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

public sealed class OnboardingStateStore : IOnboardingStateStore
{
    private readonly ConcurrentDictionary<long, OnboardingState> _store = new();

    public Task<OnboardingState?> GetAsync(long chatId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.TryGetValue(chatId, out var s) ? s : null);
    }

    public Task SetAsync(long chatId, OnboardingState state, CancellationToken cancellationToken = default)
    {
        _store[chatId] = state;
        return Task.CompletedTask;
    }

    public Task ClearAsync(long chatId, CancellationToken cancellationToken = default)
    {
        _store.TryRemove(chatId, out _);
        return Task.CompletedTask;
    }
}
