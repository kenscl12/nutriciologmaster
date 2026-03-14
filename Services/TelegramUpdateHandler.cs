using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Обработчик входящих обновлений и ошибок Telegram-бота.
/// </summary>
public sealed class TelegramUpdateHandler
{
    private readonly IMockNutritionService _mockNutritionService;

    public TelegramUpdateHandler(IMockNutritionService mockNutritionService)
    {
        _mockNutritionService = mockNutritionService;
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
                    "Привет! Я тестовый бот-консультант по питанию.\n\n" +
                    "Пришли фото блюда, и я верну mock-оценку калорий.",
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

        // Здесь можно было бы скачать файл:
        // var file = await bot.GetFile(largestPhoto.FileId, cancellationToken);
        // Но для mock-версии это не нужно.

        var mock = _mockNutritionService.GetMockNutrition();

        var reply =
            $"🍽 Блюдо: {mock.DishName}\n" +
            $"⚖️ Примерный вес: {mock.WeightGrams} г\n" +
            $"🔥 Калории: {mock.Calories} ккал\n" +
            $"🥩 Белки: {mock.Protein} г\n" +
            $"🧈 Жиры: {mock.Fat} г\n" +
            $"🍞 Углеводы: {mock.Carbs} г\n\n" +
            $"Комментарий: {mock.Comment}\n\n" +
            $"Это mock-ответ. Фото пока не анализируется по-настоящему.";

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
