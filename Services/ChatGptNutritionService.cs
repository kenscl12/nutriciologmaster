using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;
using TelegramNutritionMockBot.Configuration;
using TelegramNutritionMockBot.Models;

namespace TelegramNutritionMockBot.Services;

/// <summary>
/// Анализ питательности через ChatGPT Vision API.
/// </summary>
public sealed class ChatGptNutritionService : INutritionAnalysisService
{
    public const string HttpClientName = "OpenAI";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenAIOptions _options;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string NutritionPrompt = """
        Ты — консультант по питанию. По фото блюда оцени питательность.
        Ответь ТОЛЬКО валидным JSON без markdown и без лишнего текста, в таком формате:
        {"dishName":"название блюда","weightGrams":число,"calories":число,"protein":число,"fat":число,"carbs":число,"comment":"краткий комментарий на русском"}
        Оценки делай реалистично по виду блюда и порции. Вес в граммах, калории в ккал, БЖУ в граммах.
        """;

    public ChatGptNutritionService(IHttpClientFactory httpClientFactory, IOptions<OpenAIOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<MockNutritionResult> AnalyzeDishAsync(Stream imageStream, string mimeType, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("OpenAI:ApiKey не задан в конфигурации.");

        var base64 = await ToBase64Async(imageStream, cancellationToken).ConfigureAwait(false);
        var imageUrl = $"data:{mimeType};base64,{base64}";

        var request = new
        {
            model = _options.Model,
            messages = new object[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = NutritionPrompt },
                        new { type = "image_url", image_url = new { url = imageUrl } }
                    }
                }
            },
            max_tokens = 500
        };

        using var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.PostAsJsonAsync(
            "https://api.openai.com/v1/chat/completions",
            request,
            cancellationToken
        ).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<OpenAIResponse>(cancellationToken).ConfigureAwait(false);
        var content = json?.Choices?.FirstOrDefault()?.Message?.Content?.Trim();
        if (string.IsNullOrEmpty(content))
            throw new InvalidOperationException("Пустой ответ от ChatGPT.");

        content = StripMarkdownJson(content);
        var result = JsonSerializer.Deserialize<MockNutritionResult>(content, JsonOptions);
        if (result is null)
            throw new InvalidOperationException("Не удалось разобрать ответ ChatGPT: " + content);

        return result;
    }

    private static async Task<string> ToBase64Async(Stream stream, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        return Convert.ToBase64String(ms.ToArray());
    }

    private static string StripMarkdownJson(string raw)
    {
        var s = raw.Trim();
        if (s.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
            s = s.Substring(7);
        else if (s.StartsWith("```"))
            s = s.Substring(3);
        if (s.EndsWith("```"))
            s = s.Substring(0, s.Length - 3);
        return s.Trim();
    }

    private sealed class OpenAIResponse
    {
        public List<Choice>? Choices { get; set; }
        public class Choice
        {
            public Message? Message { get; set; }
        }
        public class Message
        {
            public string? Content { get; set; }
        }
    }
}
