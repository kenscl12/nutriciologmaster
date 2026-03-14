using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Polling;
using TelegramNutritionMockBot.Configuration;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Фоновый сервис: запускает приём обновлений бота и держит приложение активным.
/// </summary>
public sealed class TelegramBotHostedService : IHostedService
{
    private readonly BotOptions _options;
    private readonly TelegramUpdateHandler _updateHandler;
    private readonly IHostApplicationLifetime _lifetime;
    private TelegramBotClient? _client;
    private CancellationTokenSource? _cts;

    public TelegramBotHostedService(
        IOptions<BotOptions> options,
        TelegramUpdateHandler updateHandler,
        IHostApplicationLifetime lifetime)
    {
        _options = options.Value;
        _updateHandler = updateHandler;
        _lifetime = lifetime;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
        {
            Console.WriteLine("Ошибка: Bot:Token не задан в appsettings.json или переменных окружения.");
            _lifetime.StopApplication();
            return Task.CompletedTask;
        }

        _client = new TelegramBotClient(_options.Token);
        _cts = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<Telegram.Bot.Types.Enums.UpdateType>()
        };

        _ = Task.Run(async () =>
        {
            try
            {
                var me = await _client.GetMe(cancellationToken: _cts.Token);
                Console.WriteLine($"Бот @{me.Username} запущен.");
                Console.WriteLine("Нажми Enter для остановки.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении информации о боте: {ex.Message}");
            }
        }, _cts.Token);

        _client.StartReceiving(
            updateHandler: _updateHandler.HandleUpdateAsync,
            errorHandler: TelegramUpdateHandler.HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: _cts.Token
        );

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
