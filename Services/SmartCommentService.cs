using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

public sealed class SmartCommentService : ISmartCommentService
{
    public string GetComment(DailyGoal dailyGoal, MockNutritionResult meal, TimeOnly timeOfDay)
    {
        var isFirstHalfOfDay = timeOfDay.Hour < 14;

        // Слишком много жиров в первой половине дня
        if (dailyGoal.FatGrams > 0 && meal.Fat > dailyGoal.FatGrams * 0.45 && isFirstHalfOfDay)
            return "⚠️ Уже многовато жиров для первой половины дня. Попробуй на ужин добавить больше белка (курица, рыба).";

        // Мало белка во второй половине дня (ужин)
        if (!isFirstHalfOfDay && meal.Protein < 15 && meal.Calories > 200)
            return "💡 На ужин хорошо бы добавить белок — курица, рыба, творог. Так сытнее и лучше для цели.";

        // Один приём пищи — почти половина дневных калорий
        if (dailyGoal.Calories > 0 && meal.Calories > dailyGoal.Calories * 0.5)
            return "⚠️ Это почти половина дневной нормы калорий. Следующий приём можно сделать легче.";

        // Хороший белок в приёме
        if (meal.Protein >= 25 || (meal.Calories > 0 && meal.Protein * 4 >= meal.Calories * 0.25))
            return "👍 Отличный баланс белков.";

        // Много быстрых углеводов (калории в основном из углеводов), мало белка
        if (meal.Calories > 250 && meal.Carbs > meal.Protein * 3 && meal.Protein < 15)
            return "💡 К следующему приёму добавь белок или клетчатку — так дольше не захочется перекусывать.";

        return "👍 Хороший приём пищи. Следи за балансом в течение дня.";
    }
}
