using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ShoppingAgent.Services.Ai;

public sealed class OpenAiClient(HttpClient httpClient)
{
    public async Task<string> CreateJsonResponseAsync(string apiKey, string model, string systemPrompt, string userPrompt, int maxOutputTokens, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model,
            input = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_output_tokens = maxOutputTokens,
            text = new { format = new { type = "json_object" } }
        }), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.TryGetProperty("output_text", out var outputText))
        {
            return outputText.GetString() ?? "{}";
        }

        foreach (var output in root.GetProperty("output").EnumerateArray())
        {
            if (!output.TryGetProperty("content", out var content))
            {
                continue;
            }

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text))
                {
                    return text.GetString() ?? "{}";
                }
            }
        }

        return "{}";
    }
}
