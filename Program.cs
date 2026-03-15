using TelegramNutritionMockBot.Configuration;
using TelegramNutritionMockBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5090");

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));

builder.Services.AddHttpClient(ChatGptNutritionService.HttpClientName, (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>().Value;
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + options.ApiKey);
});

builder.Services.AddSingleton<INutritionAnalysisService, ChatGptNutritionService>();
builder.Services.AddSingleton<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddSingleton<IOnboardingStateStore, OnboardingStateStore>();
builder.Services.AddSingleton<TelegramUpdateHandler>();
builder.Services.AddHostedService<TelegramBotHostedService>();

var app = builder.Build();

app.MapGet("/", () => Results.Ok("OK"));
app.MapGet("/health", () => Results.Ok("OK"));

// Остановка по Enter только при интерактивном запуске (не в Docker)
//if (Environment.UserInteractive)
//{
//    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
//    _ = Task.Run(() => { try { Console.ReadLine(); } catch { } lifetime.StopApplication(); });
//}

await app.RunAsync();
