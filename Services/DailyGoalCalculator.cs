using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Расчёт дневной нормы КБЖУ по формуле Миффлина — Сан-Жеора и коэффициенту активности.
/// </summary>
public static class DailyGoalCalculator
{
    /// <summary>
    /// BMR (базовый обмен) по формуле Миффлина — Сан-Жеора.
    /// </summary>
    public static double CalculateBmr(Gender gender, int age, int heightCm, double weightKg)
    {
        var bmr = 10 * weightKg + 6.25 * heightCm - 5 * age;
        return gender == Gender.Male ? bmr + 5 : bmr - 161;
    }

    private static double GetActivityMultiplier(ActivityLevel level) => level switch
    {
        ActivityLevel.Sedentary => 1.2,
        ActivityLevel.Light => 1.375,
        ActivityLevel.Moderate => 1.55,
        ActivityLevel.Active => 1.725,
        ActivityLevel.VeryActive => 1.9,
        _ => 1.2
    };

    /// <summary>
    /// TDEE (суточный расход калорий) = BMR × коэффициент активности.
    /// </summary>
    public static double CalculateTdee(UserProfile profile)
    {
        var bmr = CalculateBmr(profile.Gender, profile.Age, profile.HeightCm, profile.WeightKg);
        return bmr * GetActivityMultiplier(profile.ActivityLevel);
    }

    /// <summary>
    /// Целевые калории с учётом цели: похудение −500 ккал, поддержание без изменений, набор +300 ккал.
    /// </summary>
    public static int GetTargetCalories(UserProfile profile)
    {
        var tdee = CalculateTdee(profile);
        var target = profile.Goal switch
        {
            Goal.LoseWeight => tdee - 500,
            Goal.Maintain => tdee,
            Goal.GainMass => tdee + 300,
            _ => tdee
        };
        return Math.Max(1200, (int)Math.Round(target));
    }

    /// <summary>
    /// Рассчитать дневную норму БЖУ по профилю.
    /// Белки: ~2 г на кг веса (поддержка цели), жиры 25–30%, остальное — углеводы.
    /// </summary>
    public static DailyGoal Calculate(UserProfile profile)
    {
        var calories = GetTargetCalories(profile);
        var proteinGrams = (int)Math.Round(2.0 * profile.WeightKg);
        var fatGrams = (int)Math.Round(calories * 0.3 / 9);
        var carbsGrams = (int)Math.Round((calories - proteinGrams * 4 - fatGrams * 9) / 4.0);
        carbsGrams = Math.Max(0, carbsGrams);

        return new DailyGoal
        {
            Calories = calories,
            ProteinGrams = proteinGrams,
            FatGrams = fatGrams,
            CarbsGrams = carbsGrams
        };
    }
}
