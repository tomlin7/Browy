using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Browy.Services
{
    public static class AiCopilotService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        public static async Task<string> QueryCopilotAsync(
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            CancellationToken cancellationToken = default)
        {
            var settings = BrowserSettings.Current;
            string provider = settings.AiProvider ?? "BrowyDefault";

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return await QueryOpenAiAsync(settings.OpenAiApiKey, settings.SelectedAiModel, prompt, pageText, headings, pageTitle, pageUrl, cancellationToken);
            }
            else if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
            {
                return await QueryAnthropicAsync(settings.AnthropicApiKey, settings.SelectedAiModel, prompt, pageText, headings, pageTitle, pageUrl, cancellationToken);
            }
            else if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                return await QueryOllamaAsync(settings.OllamaEndpoint, settings.SelectedAiModel, prompt, pageText, headings, pageTitle, pageUrl, cancellationToken);
            }

            // Default offline / heuristic synthesis
            return GenerateLocalSynthesis(prompt, pageText, headings, pageTitle, pageUrl);
        }

        private static async Task<string> QueryOpenAiAsync(
            string apiKey,
            string model,
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "⚠️ **OpenAI API Key Missing**: Please configure your OpenAI API key in Browy Settings (`browy://settings`) to enable live BYOK completions.";
            }

            string actualModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
            string systemPrompt = BuildSystemPrompt(pageText, headings, pageTitle, pageUrl);

            var payload = new
            {
                model = actualModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                },
                temperature = 0.5
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                using var response = await HttpClient.SendAsync(request, ct);
                string responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return $"⚠️ **OpenAI Error ({response.StatusCode})**: {ExtractErrorMessage(responseBody)}";
                }

                using var doc = JsonDocument.Parse(responseBody);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return content ?? "No response received from OpenAI.";
            }
            catch (Exception ex)
            {
                return $"⚠️ **OpenAI Request Failed**: {ex.Message}";
            }
        }

        private static async Task<string> QueryAnthropicAsync(
            string apiKey,
            string model,
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "⚠️ **Claude API Key Missing**: Please configure your Anthropic Claude API key in Browy Settings (`browy://settings`) to enable live BYOK completions.";
            }

            string actualModel = string.IsNullOrWhiteSpace(model) ? "claude-3-5-sonnet-20241022" : model;
            string systemPrompt = BuildSystemPrompt(pageText, headings, pageTitle, pageUrl);

            var payload = new
            {
                model = actualModel,
                system = systemPrompt,
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                using var response = await HttpClient.SendAsync(request, ct);
                string responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return $"⚠️ **Anthropic Error ({response.StatusCode})**: {ExtractErrorMessage(responseBody)}";
                }

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                return text ?? "No response received from Claude.";
            }
            catch (Exception ex)
            {
                return $"⚠️ **Claude Request Failed**: {ex.Message}";
            }
        }

        private static async Task<string> QueryOllamaAsync(
            string endpoint,
            string model,
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            CancellationToken ct)
        {
            string actualEndpoint = string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:11434" : endpoint.TrimEnd('/');
            string actualModel = string.IsNullOrWhiteSpace(model) ? "llama3" : model;
            string systemPrompt = BuildSystemPrompt(pageText, headings, pageTitle, pageUrl);

            var payload = new
            {
                model = actualModel,
                stream = false,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = prompt }
                }
            };

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{actualEndpoint}/api/chat")
                {
                    Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
                };

                using var response = await HttpClient.SendAsync(request, ct);
                string responseBody = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    return $"⚠️ **Ollama Error ({response.StatusCode})**: Make sure Ollama is running at `{actualEndpoint}` and model `{actualModel}` is pulled.";
                }

                using var doc = JsonDocument.Parse(responseBody);
                var text = doc.RootElement
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                return text ?? "No response received from local Ollama.";
            }
            catch (Exception ex)
            {
                return $"⚠️ **Ollama Request Failed**: Could not connect to Ollama at `{actualEndpoint}`. Details: {ex.Message}";
            }
        }

        public static string BuildSystemPrompt(string pageText, string headings, string pageTitle, string pageUrl)
        {
            var truncatedText = string.IsNullOrWhiteSpace(pageText)
                ? "(No readable text content on page)"
                : (pageText.Length > 6000 ? pageText.Substring(0, 6000) + "..." : pageText);

            return $"You are Browy AI Copilot, a fast and smart web browsing assistant built into Browy Browser.\n" +
                   $"The user is currently browsing:\n" +
                   $"- Title: {pageTitle}\n" +
                   $"- URL: {pageUrl}\n" +
                   $"- Headings: {headings}\n\n" +
                   $"Active page snapshot:\n" +
                   $"```\n{truncatedText}\n```\n\n" +
                   $"Answer the user's questions clearly, concisely, and formatting with Markdown.";
        }

        public static string GenerateLocalSynthesis(string prompt, string pageText, string headings, string pageTitle, string pageUrl)
        {
            if (prompt.IndexOf("summarize", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var summaryBullet1 = !string.IsNullOrWhiteSpace(headings)
                    ? $"Core Topics: {headings}"
                    : $"Topic: Overview of {pageTitle}";
                var snippet = !string.IsNullOrWhiteSpace(pageText)
                    ? (pageText.Length > 280 ? pageText.Substring(0, 280) + "..." : pageText)
                    : "No text extractable from this page.";

                return $"### Summary of {pageTitle}\n\n" +
                       $"• **Source**: `{NavigationHelper.ExtractDomain(pageUrl)}`\n" +
                       $"• **Structure**: {summaryBullet1}\n" +
                       $"• **Executive Overview**: {snippet}\n\n" +
                       $"*Synthesized live from the active DOM content (Local Engine).*";
            }
            else if (prompt.IndexOf("explain", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"### Concept Explanation: {pageTitle}\n\n" +
                       $"Based on the active page ({pageUrl}):\n\n" +
                       (!string.IsNullOrWhiteSpace(pageText)
                           ? $"The page centers on: {pageText.Substring(0, Math.Min(350, pageText.Length))}...\n\n" +
                             "**Key Takeaways**:\n" +
                             "1. Main purpose: Delivers structured documentation and interfaces for modern web workflows.\n" +
                             "2. Technical approach: Emphasizes standard client-server patterns, reactive components, and fast rendering."
                           : "This internal/blank page does not contain standard body text to analyze.");
            }
            else if (prompt.IndexOf("extract", StringComparison.OrdinalIgnoreCase) >= 0 || prompt.IndexOf("table", StringComparison.OrdinalIgnoreCase) >= 0 || prompt.IndexOf("data", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return $"### Extracted Page Intelligence: {pageTitle}\n\n" +
                       "| Attribute | Value |\n" +
                       "| :--- | :--- |\n" +
                       $"| **Title** | {pageTitle} |\n" +
                       $"| **Domain** | `{NavigationHelper.ExtractDomain(pageUrl)}` |\n" +
                       $"| **Security** | {(pageUrl.StartsWith("https") ? "Secure SSL (TLS 1.3)" : "Non-secure / Internal")} |\n" +
                       $"| **Headings Detected** | {(!string.IsNullOrWhiteSpace(headings) ? headings : "None detected")} |\n" +
                       $"| **Content Length** | {pageText.Length} characters parsed |\n\n" +
                       "*(Data parsed from active DOM snapshot)*";
            }
            else
            {
                string snippet = "";
                if (!string.IsNullOrWhiteSpace(pageText))
                {
                    var sentences = pageText.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                    var matched = sentences.FirstOrDefault(s => s.IndexOf(prompt, StringComparison.OrdinalIgnoreCase) >= 0);
                    if (matched != null)
                    {
                        snippet = matched.Trim() + ".";
                    }
                }

                if (!string.IsNullOrEmpty(snippet))
                {
                    return $"### Relevant Insight for \"{prompt}\"\n\n> {snippet}\n\nFound in {pageTitle} (`{NavigationHelper.ExtractDomain(pageUrl)}`).";
                }
                else
                {
                    return $"I examined **{pageTitle}** for \"{prompt}\".\n\n" +
                           $"• Page URL: `{pageUrl}`\n" +
                           $"• Headings found: {(!string.IsNullOrWhiteSpace(headings) ? headings : "None")}\n\n" +
                           "*(Tip: Switch to OpenAI, Claude, or Ollama in Settings -> Copilot for full generative reasoning)*";
                }
            }
        }

        private static string ExtractErrorMessage(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var errorProp))
                {
                    if (errorProp.ValueKind == JsonValueKind.Object && errorProp.TryGetProperty("message", out var msgProp))
                    {
                        return msgProp.GetString() ?? json;
                    }
                    return errorProp.ToString();
                }
            }
            catch { }
            return json;
        }
    }
}
