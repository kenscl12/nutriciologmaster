using Microsoft.EntityFrameworkCore;
using TelegramNutritionMockBot.Configuration;
using TelegramNutritionMockBot.Data;
using TelegramNutritionMockBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://*:5090");

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));

var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(conn));

builder.Services.AddHttpClient(ChatGptNutritionService.HttpClientName, (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>().Value;
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + options.ApiKey);
});

builder.Services.AddSingleton<INutritionAnalysisService, ChatGptNutritionService>();
builder.Services.AddScoped<IUserProfileRepository, UserProfileRepository>();
builder.Services.AddScoped<IMealRepository, MealRepository>();
builder.Services.AddSingleton<IOnboardingStateStore, OnboardingStateStore>();
builder.Services.AddSingleton<TelegramUpdateHandler>();
builder.Services.AddHostedService<TelegramBotHostedService>();

var app = builder.Build();

// Применить миграции при старте (создать/обновить таблицы в PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.MapGet("/", () => Results.Ok("OK"));
app.MapGet("/health", () => Results.Ok("OK"));
// Проверка: какая среда и конфиг загружены (для отладки)
app.MapGet("/env", () => new { env = app.Environment.EnvironmentName });

// Остановка по Enter только при интерактивном запуске (не в Docker)
//if (Environment.UserInteractive)
//{
//    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
//    _ = Task.Run(() => { try { Console.ReadLine(); } catch { } lifetime.StopApplication(); });
//}

await app.RunAsync();
