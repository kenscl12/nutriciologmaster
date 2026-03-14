using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TelegramNutritionMockBot.Configuration;
using TelegramNutritionMockBot.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<BotOptions>(builder.Configuration.GetSection(BotOptions.SectionName));
builder.Services.Configure<OpenAIOptions>(builder.Configuration.GetSection(OpenAIOptions.SectionName));

builder.Services.AddHttpClient(ChatGptNutritionService.HttpClientName, (sp, client) =>
{
    var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAIOptions>>().Value;
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + options.ApiKey);
});

builder.Services.AddSingleton<INutritionAnalysisService, ChatGptNutritionService>();
builder.Services.AddSingleton<TelegramUpdateHandler>();
builder.Services.AddHostedService<TelegramBotHostedService>();

var host = builder.Build();

// Остановка по Enter
var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
_ = Task.Run(() => { Console.ReadLine(); lifetime.StopApplication(); });

await host.RunAsync();
