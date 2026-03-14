using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Обработчик входящих обновлений и ошибок Telegram-бота.
/// </summary>
public sealed class TelegramUpdateHandler
{
    private readonly INutritionAnalysisService _nutritionAnalysisService;

    public TelegramUpdateHandler(INutritionAnalysisService nutritionAnalysisService)
    {
        _nutritionAnalysisService = nutritionAnalysisService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is null)
            return;

        var chatId = update.Message.Chat.Id;

        if (update.Message.Text is not null)
        {
            await HandleTextMessageAsync(bot, chatId, update.Message.Text, cancellationToken);
            return;
        }

        if (update.Message.Photo is { Length: > 0 })
        {
            await HandlePhotoAsync(bot, chatId, update.Message.Photo, cancellationToken);
            return;
        }

        await bot.SendMessage(
            chatId: chatId,
            text: "Пожалуйста, отправь фото блюда.",
            cancellationToken: cancellationToken
        );
    }

    private async Task HandleTextMessageAsync(ITelegramBotClient bot, long chatId, string text, CancellationToken cancellationToken)
    {
        if (text == "/start")
        {
            await bot.SendMessage(
                chatId: chatId,
                text:
                    "Привет! Я бот-консультант по питанию.\n\n" +
                    "Пришли фото блюда — я оценю калории и БЖУ по изображению.",
                cancellationToken: cancellationToken
            );
            return;
        }

        if (text == "/help")
        {
            await bot.SendMessage(
                chatId: chatId,
                text:
                    "Команды:\n" +
                    "/start — запуск\n" +
                    "/help — помощь\n\n" +
                    "Просто отправь фото блюда.",
                cancellationToken: cancellationToken
            );
            return;
        }

        await bot.SendMessage(
            chatId: chatId,
            text: "Пришли фото блюда, а не текст.",
            cancellationToken: cancellationToken
        );
    }

    private async Task HandlePhotoAsync(ITelegramBotClient bot, long chatId, Telegram.Bot.Types.PhotoSize[] photoSizes, CancellationToken cancellationToken)
    {
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

        var reply =
            $"🍽 Блюдо: {result.DishName}\n" +
            $"⚖️ Примерный вес: {result.WeightGrams} г\n" +
            $"🔥 Калории: {result.Calories} ккал\n" +
            $"🥩 Белки: {result.Protein} г\n" +
            $"🧈 Жиры: {result.Fat} г\n" +
            $"🍞 Углеводы: {result.Carbs} г\n\n" +
            $"Комментарий: {result.Comment}";

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
