using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Обработчик входящих обновлений и ошибок Telegram-бота.
/// </summary>
public sealed class TelegramUpdateHandler 
{
    private readonly INutritionAnalysisService _nutritionAnalysisService;
    private readonly ISmartCommentService _smartCommentService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOnboardingStateStore _onboardingStore;

    public TelegramUpdateHandler(
        INutritionAnalysisService nutritionAnalysisService,
        ISmartCommentService smartCommentService,
        IServiceScopeFactory scopeFactory,
        IOnboardingStateStore onboardingStore)
    {
        _nutritionAnalysisService = nutritionAnalysisService;
        _smartCommentService = smartCommentService;
        _scopeFactory = scopeFactory;
        _onboardingStore = onboardingStore;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var profileRepo = scope.ServiceProvider.GetRequiredService<IUserProfileRepository>();
        var mealRepo = scope.ServiceProvider.GetRequiredService<IMealRepository>();

        if (update.CallbackQuery is { } callback)
        {
            await HandleCallbackQueryAsync(bot, callback, profileRepo, cancellationToken);
            return;
        }

        if (update.Message is null)
            return;

        var chatId = update.Message.Chat.Id;

        if (update.Message.Text is not null)
        {
            await HandleTextMessageAsync(bot, chatId, update.Message.Text, profileRepo, mealRepo, cancellationToken);
            return;
        }

        if (update.Message.Photo is { Length: > 0 })
        {
            await HandlePhotoAsync(bot, chatId, update.Message.Photo, profileRepo, mealRepo, cancellationToken);
            return;
        }

        await bot.SendMessage(
            chatId: chatId,
            text: "Пожалуйста, отправь фото блюда.",
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleCallbackQueryAsync(ITelegramBotClient bot, CallbackQuery callback, IUserProfileRepository profileRepo, CancellationToken cancellationToken)
    {
        if (callback.Message?.Chat is null)
            return;
        var chatId = callback.Message.Chat.Id;
        await bot.AnswerCallbackQuery(callback.Id, cancellationToken: cancellationToken);

        var state = await _onboardingStore.GetAsync(chatId, cancellationToken);
        if (state is null)
            return;

        var data = callback.Data ?? "";

        if (state.Step == OnboardingStep.Gender && data.StartsWith("gender_"))
        {
            state.Gender = data == "gender_m" ? Gender.Male : Gender.Female;
            state.Step = OnboardingStep.Age;
            await _onboardingStore.SetAsync(chatId, state, cancellationToken);
            await bot.SendMessage(chatId, "Сколько тебе полных лет? (число)", cancellationToken: cancellationToken);
            return;
        }

        if (state.Step == OnboardingStep.Goal && data.StartsWith("goal_"))
        {
            state.Goal = data switch
            {
                "goal_lose" => Goal.LoseWeight,
                "goal_maintain" => Goal.Maintain,
                "goal_gain" => Goal.GainMass,
                _ => Goal.Maintain
            };
            state.Step = OnboardingStep.Activity;
            await _onboardingStore.SetAsync(chatId, state, cancellationToken);
            await SendActivityKeyboardAsync(bot, chatId, cancellationToken);
            return;
        }

        if (state.Step == OnboardingStep.Activity && data.StartsWith("act_"))
        {
            state.ActivityLevel = data switch
            {
                "act_sedentary" => ActivityLevel.Sedentary,
                "act_light" => ActivityLevel.Light,
                "act_moderate" => ActivityLevel.Moderate,
                "act_active" => ActivityLevel.Active,
                "act_very" => ActivityLevel.VeryActive,
                _ => ActivityLevel.Moderate
            };
            await FinishOnboardingAsync(bot, chatId, state, profileRepo, cancellationToken);
        }
    }

    private async Task HandleTextMessageAsync(ITelegramBotClient bot, long chatId, string text, IUserProfileRepository profileRepo, IMealRepository mealRepo, CancellationToken cancellationToken)
    {
        if (text == "/start")
        {
            var profile = await profileRepo.GetByChatIdAsync(chatId, cancellationToken);
            if (profile is not null)
            {
                var dailyGoal = DailyGoalCalculator.Calculate(profile);
                await bot.SendMessage(
                    chatId: chatId,
                    text: "Привет! Твоя дневная цель:\n\n" + FormatDailyGoal(dailyGoal),
                    cancellationToken: cancellationToken
                );
                return;
            }

            var state = await _onboardingStore.GetAsync(chatId, cancellationToken);
            if (state is null)
            {
                state = new OnboardingState { Step = OnboardingStep.Gender };
                await _onboardingStore.SetAsync(chatId, state, cancellationToken);
            }

            if (state.Step == OnboardingStep.Gender)
            {
                await SendGenderKeyboardAsync(bot, chatId, cancellationToken);
                return;
            }

            if (state.Step == OnboardingStep.Age)
            {
                if (int.TryParse(text.Trim(), out var age) && age >= 10 && age <= 120)
                {
                    state.Age = age;
                    state.Step = OnboardingStep.Height;
                    await _onboardingStore.SetAsync(chatId, state, cancellationToken);
                    await bot.SendMessage(chatId, "Какой у тебя рост в см? (число)", cancellationToken: cancellationToken);
                }
                else
                    await bot.SendMessage(chatId, "Введи возраст числом от 10 до 120.", cancellationToken: cancellationToken);
                return;
            }

            if (state.Step == OnboardingStep.Height)
            {
                if (int.TryParse(text.Trim(), out var height) && height >= 100 && height <= 250)
                {
                    state.HeightCm = height;
                    state.Step = OnboardingStep.Weight;
                    await _onboardingStore.SetAsync(chatId, state, cancellationToken);
                    await bot.SendMessage(chatId, "Какой у тебя вес в кг? (число, можно с запятой)", cancellationToken: cancellationToken);
                }
                else
                    await bot.SendMessage(chatId, "Введи рост в см числом (100–250).", cancellationToken: cancellationToken);
                return;
            }

            if (state.Step == OnboardingStep.Weight)
            {
                var normalized = text.Trim().Replace(',', '.');
                if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var weight) && weight >= 30 && weight <= 300)
                {
                    state.WeightKg = weight;
                    state.Step = OnboardingStep.Goal;
                    await _onboardingStore.SetAsync(chatId, state, cancellationToken);
                    await SendGoalKeyboardAsync(bot, chatId, cancellationToken);
                }
                else
                    await bot.SendMessage(chatId, "Введи вес в кг числом (30–300).", cancellationToken: cancellationToken);
                return;
            }

            if (state.Step != OnboardingStep.None && state.Step != OnboardingStep.Done)
            {
                await bot.SendMessage(chatId, "Используй кнопки выше для выбора или отправь фото блюда.", cancellationToken: cancellationToken);
                return;
            }
        }

        // Ответы на вопросы онбординга (возраст, рост, вес) приходят отдельным сообщением — обрабатываем вне /start
        var onboardingState = await _onboardingStore.GetAsync(chatId, cancellationToken);
        if (onboardingState is not null)
        {
            if (onboardingState.Step == OnboardingStep.Age)
            {
                if (int.TryParse(text.Trim(), out var age) && age >= 10 && age <= 120)
                {
                    onboardingState.Age = age;
                    onboardingState.Step = OnboardingStep.Height;
                    await _onboardingStore.SetAsync(chatId, onboardingState, cancellationToken);
                    await bot.SendMessage(chatId, "Какой у тебя рост в см? (число)", cancellationToken: cancellationToken);
                }
                else
                    await bot.SendMessage(chatId, "Введи возраст числом от 10 до 120.", cancellationToken: cancellationToken);
                return;
            }

            if (onboardingState.Step == OnboardingStep.Height)
            {
                if (int.TryParse(text.Trim(), out var height) && height >= 100 && height <= 250)
                {
                    onboardingState.HeightCm = height;
                    onboardingState.Step = OnboardingStep.Weight;
                    await _onboardingStore.SetAsync(chatId, onboardingState, cancellationToken);
                    await bot.SendMessage(chatId, "Какой у тебя вес в кг? (число, можно с запятой)", cancellationToken: cancellationToken);
                }
                else
                    await bot.SendMessage(chatId, "Введи рост в см числом (100–250).", cancellationToken: cancellationToken);
                return;
            }

            if (onboardingState.Step == OnboardingStep.Weight)
            {
                var normalized = text.Trim().Replace(',', '.');
                if (double.TryParse(normalized, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var weight) && weight >= 30 && weight <= 300)
                {
                    onboardingState.WeightKg = weight;
                    onboardingState.Step = OnboardingStep.Goal;
                    await _onboardingStore.SetAsync(chatId, onboardingState, cancellationToken);
                    await SendGoalKeyboardAsync(bot, chatId, cancellationToken);
                }
                else
                    await bot.SendMessage(chatId, "Введи вес в кг числом (30–300).", cancellationToken: cancellationToken);
                return;
            }

            if (onboardingState.Step != OnboardingStep.None && onboardingState.Step != OnboardingStep.Done)
            {
                await bot.SendMessage(chatId, "Используй кнопки выше для выбора или отправь фото блюда.", cancellationToken: cancellationToken);
                return;
            }
        }

        if (text == "/day")
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var meals = await mealRepo.GetMealsForDateAsync(chatId, today, cancellationToken);
            var message = FormatDayMeals(today, meals);
            await bot.SendMessage(chatId: chatId, text: message, cancellationToken: cancellationToken);
            return;
        }

        if (text == "/help")
        {
            const string helpText =
                "Команды:\n" +
                "/start — дневная цель (норма КБЖУ)\n" +
                "/day — список блюд за сегодня\n" +
                "/help — эта справка\n\n" +
                "Отправь фото блюда — оценю калории и БЖУ и добавлю в дневник.";
            await bot.SendMessage(chatId: chatId, text: helpText, cancellationToken: cancellationToken);
            return;
        }

        await bot.SendMessage(
            chatId: chatId,
            text: "Отправь фото блюда для анализа, /start для дневной цели или /day — что ел сегодня.",
            cancellationToken: cancellationToken
        );
    }

    private static async Task SendGenderKeyboardAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Мужской", "gender_m"),
                InlineKeyboardButton.WithCallbackData("Женский", "gender_f")
            }
        });
        await bot.SendMessage(chatId, "Привет! Чтобы считать твою дневную норму, нужны данные.\n\nУкажи пол:", replyMarkup: keyboard, cancellationToken: ct);
    }

    private static async Task SendGoalKeyboardAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Похудеть", "goal_lose") },
            new[] { InlineKeyboardButton.WithCallbackData("Поддерживать вес", "goal_maintain") },
            new[] { InlineKeyboardButton.WithCallbackData("Набрать массу", "goal_gain") }
        });
        await bot.SendMessage(chatId, "Какая у тебя цель?", replyMarkup: keyboard, cancellationToken: ct);
    }

    private static async Task SendActivityKeyboardAsync(ITelegramBotClient bot, long chatId, CancellationToken ct)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Почти нет активности", "act_sedentary") },
            new[] { InlineKeyboardButton.WithCallbackData("1–3 дня в неделю", "act_light") },
            new[] { InlineKeyboardButton.WithCallbackData("3–5 дней в неделю", "act_moderate") },
            new[] { InlineKeyboardButton.WithCallbackData("6–7 дней в неделю", "act_active") },
            new[] { InlineKeyboardButton.WithCallbackData("Каждый день / физ. работа", "act_very") }
        });
        await bot.SendMessage(chatId, "Как часто ты занимаешься спортом или физической активностью?", replyMarkup: keyboard, cancellationToken: ct);
    }

    private async Task FinishOnboardingAsync(ITelegramBotClient bot, long chatId, OnboardingState state, IUserProfileRepository profileRepo, CancellationToken cancellationToken)
    {
        if (state.Gender is null || state.Age is null || state.HeightCm is null || state.WeightKg is null || state.Goal is null || state.ActivityLevel is null)
        {
            await bot.SendMessage(chatId, "Что-то пошло не так. Начни заново: /start", cancellationToken: cancellationToken);
            return;
        }

        var profile = new UserProfile
        {
            ChatId = chatId,
            Gender = state.Gender.Value,
            Age = state.Age.Value,
            HeightCm = state.HeightCm.Value,
            WeightKg = state.WeightKg.Value,
            Goal = state.Goal.Value,
            ActivityLevel = state.ActivityLevel.Value
        };

        await profileRepo.SaveAsync(profile, cancellationToken);
        await _onboardingStore.ClearAsync(chatId, cancellationToken);

        var dailyGoal = DailyGoalCalculator.Calculate(profile);
        await bot.SendMessage(
            chatId: chatId,
            text: "Готово! Твоя дневная цель:\n\n" + FormatDailyGoal(dailyGoal) + "\n\nТеперь можешь присылать фото блюд — буду оценивать калории и БЖУ.",
            cancellationToken: cancellationToken
        );
    }

    private static string FormatDailyGoal(DailyGoal g) =>
        $"🔥 {g.Calories} ккал\n" +
        $"🥩 Белки: {g.ProteinGrams} г\n" +
        $"🥑 Жиры: {g.FatGrams} г\n" +
        $"🍞 Углеводы: {g.CarbsGrams} г";

    private static string FormatDayMeals(DateOnly date, IReadOnlyList<MealEntry> meals)
    {
        var dateStr = date.ToString("dd.MM.yyyy");
        if (meals.Count == 0)
            return $"📅 Блюда за {dateStr}\n\nСегодня пока ничего не записано. Пришли фото блюда — добавлю в дневник!";

        var lines = new List<string> { $"📅 Блюда за {dateStr}\n" };
        var totalCal = 0;
        var totalP = 0;
        var totalF = 0;
        var totalC = 0;
        for (var i = 0; i < meals.Count; i++)
        {
            var m = meals[i];
            totalCal += m.Calories;
            totalP += m.Protein;
            totalF += m.Fat;
            totalC += m.Carbs;
            lines.Add($"{i + 1}. 🍽 {m.MealName} — {m.Calories} ккал (Б {m.Protein} Ж {m.Fat} У {m.Carbs})");
        }
        lines.Add($"\n📊 Итого: {totalCal} ккал | Белки {totalP} г | Жиры {totalF} г | Углеводы {totalC} г");
        return string.Join("\n", lines);
    }

    private async Task HandlePhotoAsync(ITelegramBotClient bot, long chatId, Telegram.Bot.Types.PhotoSize[] photoSizes, IUserProfileRepository profileRepo, IMealRepository mealRepo, CancellationToken cancellationToken)
    {
        var state = await _onboardingStore.GetAsync(chatId, cancellationToken);
        if (state is not null && state.Step != OnboardingStep.None && state.Step != OnboardingStep.Done)
        {
            await bot.SendMessage(chatId, "Сначала закончи настройку профиля — ответь на вопросы выше.", cancellationToken: cancellationToken);
            return;
        }

        var largestPhoto = photoSizes
            .OrderByDescending(p => p.FileSize ?? 0)
            .First();

        await using var imageStream = new MemoryStream();
        var file = await bot.GetFile(largestPhoto.FileId, cancellationToken);
        await bot.DownloadFile(file.FilePath!, imageStream, cancellationToken);
        imageStream.Position = 0;

        MockNutritionResult result;
        try
        {
            result = await _nutritionAnalysisService.AnalyzeDishAsync(imageStream, "image/jpeg", cancellationToken);
        }
        catch (Exception ex)
        {
            await bot.SendMessage(
                chatId: chatId,
                text: $"Не удалось проанализировать фото: {ex.Message}. Попробуй другое фото.",
                cancellationToken: cancellationToken
            );
            return;
        }

        var profile = await profileRepo.GetByChatIdAsync(chatId, cancellationToken);
        var daily = profile is not null ? DailyGoalCalculator.Calculate(profile) : null;

        var reply =
            $"🍽 Блюдо: {result.DishName}\n" +
            $"⚖️ Примерный вес: {result.WeightGrams} г\n" +
            $"🔥 Калории: {result.Calories} ккал\n" +
            $"🥩 Белки: {result.Protein} г\n" +
            $"🧈 Жиры: {result.Fat} г\n" +
            $"🍞 Углеводы: {result.Carbs} г\n\n" +
            $"Комментарий: {result.Comment}";

        if (daily is not null)
        {
            var timeOfDay = TimeOnly.FromDateTime(DateTime.UtcNow);
            var smartComment = _smartCommentService.GetComment(daily, result, timeOfDay);
            reply += $"\n\n{smartComment}";
            var remaining = $"Осталось до дневной нормы: {Math.Max(0, daily.Calories - result.Calories)} ккал.";
            reply += "\n\n" + remaining;

            await mealRepo.AddMealAsync(
                chatId,
                DateOnly.FromDateTime(DateTime.UtcNow),
                result.DishName,
                result.Calories,
                result.Protein,
                result.Fat,
                result.Carbs,
                cancellationToken);
        }

        await bot.SendMessage(
            chatId: chatId,
            text: reply,
            cancellationToken: cancellationToken
        );
    }

    public static Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}
