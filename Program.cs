using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

const string botToken = "7933879826:AAFUp4QddutXqStiKdGKJNj6eN8D-V0ZlZI";

var botClient = new TelegramBotClient(botToken);
using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
};

botClient.StartReceiving(
    updateHandler: HandleUpdateAsync,
    errorHandler: HandleErrorAsync,
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

var me = await botClient.GetMe();
Console.WriteLine($"Бот @{me.Username} запущен.");
Console.WriteLine("Нажми Enter для остановки.");
Console.ReadLine();

cts.Cancel();
return;

async Task HandleUpdateAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
{
    if (update.Message is null)
        return;

    var chatId = update.Message.Chat.Id;

    if (update.Message.Text is not null)
    {
        if (update.Message.Text == "/start")
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

        if (update.Message.Text == "/help")
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
        return;
    }

    if (update.Message.Photo is { Length: > 0 })
    {
        var largestPhoto = update.Message.Photo
            .OrderByDescending(p => p.FileSize ?? 0)
            .First();

        // Здесь можно было бы скачать файл:
        // var file = await bot.GetFile(largestPhoto.FileId, cancellationToken);
        // Но для mock-версии это не нужно.

        var mock = GetMockNutrition();

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

        return;
    }

    await bot.SendMessage(
        chatId: chatId,
        text: "Пожалуйста, отправь фото блюда.",
        cancellationToken: cancellationToken
    );
}

Task HandleErrorAsync(ITelegramBotClient bot, Exception exception, CancellationToken cancellationToken)
{
    Console.WriteLine($"Ошибка: {exception.Message}");
    return Task.CompletedTask;
}

static MockNutritionResult GetMockNutrition()
{
    return new MockNutritionResult
    {
        DishName = "Курица с рисом",
        WeightGrams = 350,
        Calories = 520,
        Protein = 32,
        Fat = 14,
        Carbs = 58,
        Comment = "Оценка примерная, сейчас используется захардкоженный ответ."
    };
}

public sealed class MockNutritionResult
{
    public string DishName { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public int Calories { get; set; }
    public int Protein { get; set; }
    public int Fat { get; set; }
    public int Carbs { get; set; }
    public string Comment { get; set; } = string.Empty;
}