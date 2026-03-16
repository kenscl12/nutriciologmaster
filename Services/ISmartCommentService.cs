using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Генерирует персональные комментарии диетолога по приёму пищи и целям пользователя.
/// </summary>
public interface ISmartCommentService
{
    string GetComment(DailyGoal dailyGoal, MockNutritionResult meal, TimeOnly timeOfDay);
}
