using System.Net.Http;
using System.Text;
using System.Text.Json;
using WordCollector.Models;

namespace WordCollector.Services;

public class AiService : IAiLookupProvider, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;

    public AiService(SettingsService settingsService)
    {
        _settingsService = settingsService;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public Task<(AiExplanationResult? Result, string? RawResponse, string? Error)> QueryAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        return QueryAsync(text, _settingsService.Load(), cancellationToken);
    }

    public async Task<(AiExplanationResult? Result, string? RawResponse, string? Error)> QueryAsync(
        string text,
        AppSettings settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.AiApiKey))
        {
            return (null, null, "请先在设置中配置 API Key");
        }

        try
        {
            if (!AiEndpointResolver.TryResolve(
                    settings, out var endpoint, out var model, out var endpointError))
            {
                return (null, null, endpointError);
            }

            var systemPrompt = @"You are an English tutor. For the given English text, return ONLY a JSON object (no other text) with these fields:
- item_type: ""word""/""phrase""/""sentence""
- phonetic: IPA pronunciation (for words, use standard IPA; for phrases/sentences leave empty string)
- meaning_zh: short Chinese translation
- brief_explanation: one-sentence usage tip in Chinese
- detailed_explanation: longer explanation in Chinese (1-2 sentences)
- example_en: one natural English example sentence
- example_zh: Chinese translation of the example
- key_expressions: array of {expression, meaning_zh} for sentences (empty array for words/phrases)

Keep responses concise. Example:
{""item_type"":""word"",""phonetic"":""/ɪɡˈzæmpl̩/"",""meaning_zh"":""例子"",""brief_explanation"":""常用 for example 引出具体事例"",""detailed_explanation"":""example 是可数名词，指能代表同类事物的具体事例。"",""example_en"":""Can you give me an example?"",""example_zh"":""你能给我举个例子吗？"",""key_expressions"":[]}";

            var requestBody = new
            {
                model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = text }
                },
                temperature = 0.2,
                max_tokens = 600
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = content
            };
            request.Headers.Add("Authorization", $"Bearer {settings.AiApiKey.Trim()}");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "API Key 无效，请检查设置",
                    System.Net.HttpStatusCode.TooManyRequests => "请求过于频繁，请稍后重试",
                    System.Net.HttpStatusCode.NotFound => "API 地址无效，请检查 Base URL",
                    _ => $"API 请求失败 ({response.StatusCode})"
                };
                return (null, null, errorMsg);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!AiResponseParser.TryExtractAssistantContent(
                    responseBody, out var assistantContent, out var parseError))
            {
                return (null, null, parseError);
            }

            if (!AiResponseParser.TryParseExplanation(
                    assistantContent, out var result, out var rawContent))
            {
                return (null, rawContent, null);
            }

            return (result, rawContent, null);
        }
        catch (TaskCanceledException)
        {
            return (null, null, "请求超时，请检查网络连接");
        }
        catch (HttpRequestException ex)
        {
            return (null, null, $"网络错误：{ex.Message}");
        }
        catch (Exception ex)
        {
            return (null, null, $"查询失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 用给定配置发起一次真实查询来测试连通性，不落盘。
    /// 只要 API 正常返回了内容（即使 JSON 解析失败），就视为连接成功。
    /// </summary>
    public async Task<(bool Ok, string? Error)> TestConnectionAsync(AppSettings settings)
    {
        var (result, rawResponse, error) = await QueryAsync("hello", settings);
        if (error != null)
            return (false, error);
        return (result != null || rawResponse != null, null);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
