namespace TelegramNutritionMockBot.Models;

public sealed class OnboardingState
{
    public OnboardingStep Step { get; set; }
    public Gender? Gender { get; set; }
    public int? Age { get; set; }
    public int? HeightCm { get; set; }
    public double? WeightKg { get; set; }
    public Goal? Goal { get; set; }
    public ActivityLevel? ActivityLevel { get; set; }
}
