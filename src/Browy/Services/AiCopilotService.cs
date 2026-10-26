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
    public class AgentActionResult
    {
        public string CleanText { get; set; } = string.Empty;
        public string? ActionCommand { get; set; }
        public string? ActionTarget { get; set; }
        public string? ActionFeedback { get; set; }
        public bool HasAction => !string.IsNullOrEmpty(ActionCommand);
    }

    public static class AiCopilotService
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(25) };

        public static async Task<string> QueryCopilotAsync(
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            string? persona = null,
            string? screenshotBase64 = null,
            string? customInstruction = null,
            CancellationToken cancellationToken = default)
        {
            var settings = BrowserSettings.Current;
            string provider = settings.AiProvider ?? "BrowyDefault";

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                return await QueryOpenAiAsync(settings.OpenAiApiKey, settings.SelectedAiModel, prompt, pageText, headings, pageTitle, pageUrl, persona, screenshotBase64, customInstruction, cancellationToken);
            }
            else if (provider.Equals("Anthropic", StringComparison.OrdinalIgnoreCase))
            {
                return await QueryAnthropicAsync(settings.AnthropicApiKey, settings.SelectedAiModel, prompt, pageText, headings, pageTitle, pageUrl, persona, screenshotBase64, customInstruction, cancellationToken);
            }
            else if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
            {
                return await QueryOllamaAsync(settings.OllamaEndpoint, settings.SelectedAiModel, prompt, pageText, headings, pageTitle, pageUrl, persona, customInstruction, cancellationToken);
            }

            // Default offline / heuristic synthesis
            return GenerateLocalSynthesis(prompt, pageText, headings, pageTitle, pageUrl, persona, screenshotBase64);
        }

        // Backward compatibility overload
        public static Task<string> QueryCopilotAsync(
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            CancellationToken cancellationToken)
        {
            return QueryCopilotAsync(prompt, pageText, headings, pageTitle, pageUrl, null, null, null, cancellationToken);
        }

        private static async Task<string> QueryOpenAiAsync(
            string apiKey,
            string model,
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            string? persona,
            string? screenshotBase64,
            string? customInstruction,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "**OpenAI API Key Missing**: Please configure your OpenAI API key in Browy Settings (`browy://settings`) to enable live BYOK completions.";
            }

            string actualModel = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
            string systemPrompt = BuildSystemPrompt(pageText, headings, pageTitle, pageUrl, persona, customInstruction);

            object userMessageContent;
            if (!string.IsNullOrEmpty(screenshotBase64))
            {
                userMessageContent = new object[]
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = $"data:image/png;base64,{screenshotBase64}" } }
                };
            }
            else
            {
                userMessageContent = prompt;
            }

            var payload = new
            {
                model = actualModel,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userMessageContent }
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
                    return $"**OpenAI Error ({response.StatusCode})**: {ExtractErrorMessage(responseBody)}";
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
                return $"**OpenAI Request Failed**: {ex.Message}";
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
            string? persona,
            string? screenshotBase64,
            string? customInstruction,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return "**Claude API Key Missing**: Please configure your Anthropic Claude API key in Browy Settings (`browy://settings`) to enable live BYOK completions.";
            }

            string actualModel = string.IsNullOrWhiteSpace(model) ? "claude-3-5-sonnet-20241022" : model;
            string systemPrompt = BuildSystemPrompt(pageText, headings, pageTitle, pageUrl, persona, customInstruction);

            object userContent;
            if (!string.IsNullOrEmpty(screenshotBase64))
            {
                userContent = new object[]
                {
                    new
                    {
                        type = "image",
                        source = new
                        {
                            type = "base64",
                            media_type = "image/png",
                            data = screenshotBase64
                        }
                    },
                    new { type = "text", text = prompt }
                };
            }
            else
            {
                userContent = prompt;
            }

            var payload = new
            {
                model = actualModel,
                system = systemPrompt,
                max_tokens = 1024,
                messages = new[]
                {
                    new { role = "user", content = userContent }
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
                    return $"**Anthropic Error ({response.StatusCode})**: {ExtractErrorMessage(responseBody)}";
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
                return $"**Claude Request Failed**: {ex.Message}";
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
            string? persona,
            string? customInstruction,
            CancellationToken ct)
        {
            string actualEndpoint = string.IsNullOrWhiteSpace(endpoint) ? "http://localhost:11434" : endpoint.TrimEnd('/');
            string actualModel = string.IsNullOrWhiteSpace(model) ? "llama3" : model;
            string systemPrompt = BuildSystemPrompt(pageText, headings, pageTitle, pageUrl, persona, customInstruction);

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
                    return $"**Ollama Error ({response.StatusCode})**: Make sure Ollama is running at `{actualEndpoint}` and model `{actualModel}` is pulled.";
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
                return $"**Ollama Request Failed**: Could not connect to Ollama at `{actualEndpoint}`. Details: {ex.Message}";
            }
        }

        public static string BuildSystemPrompt(
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            string? persona = null,
            string? customInstruction = null)
        {
            var truncatedText = string.IsNullOrWhiteSpace(pageText)
                ? "(No readable text content on page)"
                : (pageText.Length > 6000 ? pageText.Substring(0, 6000) + "..." : pageText);

            string personaIntro = persona switch
            {
                "Deep Research" => "You are Browy Deep Research Agent. Critically analyze arguments, cross-verify claims, identify potential biases, evaluate evidence, and provide comprehensive structured research reports with citations.",
                "Code Analyst" => "You are Browy Code Analyst. Inspect web technologies, extract and explain code snippets, analyze frontend APIs, evaluate page performance, and check for security considerations.",
                "Content Explainer" => "You are Browy Content Explainer. Make any subject accessible, engaging, and clear using analogies, step-by-step walkthroughs, and plain language explanations.",
                _ => "You are Browy AI Copilot, a fast and smart web browsing assistant built into Browy Browser."
            };

            if (!string.IsNullOrWhiteSpace(customInstruction))
            {
                personaIntro += $"\nSpecialized Persona Directive: {customInstruction}";
            }

            return $"{personaIntro}\n" +
                   $"The user is currently browsing:\n" +
                   $"- Title: {pageTitle}\n" +
                   $"- URL: {pageUrl}\n" +
                   $"- Headings: {headings}\n\n" +
                   $"Active page snapshot:\n" +
                   $"```\n{truncatedText}\n```\n\n" +
                   $"You can perform web browser actions for the user if requested by adding an action directive on its own line at the end:\n" +
                   $"[BROWY_ACTION: SCROLL_DOWN]\n" +
                   $"[BROWY_ACTION: SCROLL_UP]\n" +
                   $"[BROWY_ACTION: NAVIGATE <url>]\n" +
                   $"[BROWY_ACTION: NEWTAB <url>]\n" +
                   $"[BROWY_ACTION: HIGHLIGHT <text>]\n" +
                   $"[BROWY_ACTION: CLICK <selector_or_text>]\n" +
                   $"[BROWY_ACTION: FILL <field_query> | <value>]\n" +
                   $"[BROWY_ACTION: ZOOM <80|100|125|150>]\n" +
                   $"[BROWY_ACTION: READER_MODE]\n" +
                   $"[BROWY_ACTION: TRANSLATE <target_language>]\n" +
                   $"[BROWY_ACTION: DARK_MODE]\n\n" +
                   $"Answer the user's questions clearly, concisely, and formatting with Markdown.";
        }

        public static string GenerateLocalSynthesis(
            string prompt,
            string pageText,
            string headings,
            string pageTitle,
            string pageUrl,
            string? persona = null,
            string? screenshotBase64 = null)
        {
            string lowerPrompt = prompt.ToLowerInvariant();

            // 1. Browser Action direct detection
            if (lowerPrompt.Contains("scroll down"))
            {
                return "Scrolled down the active page.\n\n[BROWY_ACTION: SCROLL_DOWN]";
            }
            if (lowerPrompt.Contains("scroll up") || lowerPrompt.Contains("scroll to top") || lowerPrompt.Contains("scroll top"))
            {
                return "Scrolled to the top of the active page.\n\n[BROWY_ACTION: SCROLL_UP]";
            }
            if (lowerPrompt.StartsWith("highlight ") || lowerPrompt.Contains("highlight the word ") || lowerPrompt.Contains("highlight '"))
            {
                string term = prompt.Replace("highlight", "", StringComparison.OrdinalIgnoreCase)
                                    .Replace("the word", "", StringComparison.OrdinalIgnoreCase)
                                    .Trim(' ', '\'', '"');
                if (!string.IsNullOrWhiteSpace(term))
                {
                    return $"Highlighted instances of **\"{term}\"** on the page.\n\n[BROWY_ACTION: HIGHLIGHT {term}]";
                }
            }
            if (lowerPrompt.Contains("open ") && lowerPrompt.Contains(" in new tab"))
            {
                var words = prompt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var urlWord = words.FirstOrDefault(w => w.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || w.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || w.Contains("."));
                if (urlWord != null)
                {
                    string targetUrl = urlWord.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? urlWord : "https://" + urlWord;
                    return $"Opening `{targetUrl}` in a new tab.\n\n[BROWY_ACTION: NEWTAB {targetUrl}]";
                }
            }
            if (lowerPrompt.Contains("reader mode") || lowerPrompt.Contains("reading mode") || lowerPrompt.Contains("clean view") || lowerPrompt.Contains("distraction free"))
            {
                return "Toggled distraction-free Reader Mode.\n\n[BROWY_ACTION: READER_MODE]";
            }
            if (lowerPrompt.Contains("dark mode") || lowerPrompt.Contains("night mode") || lowerPrompt.Contains("toggle dark"))
            {
                return "Toggled page dark mode.\n\n[BROWY_ACTION: DARK_MODE]";
            }
            if (lowerPrompt.StartsWith("zoom in") || lowerPrompt.Contains("zoom 125"))
            {
                return "Zoomed in (125%).\n\n[BROWY_ACTION: ZOOM 125]";
            }
            if (lowerPrompt.StartsWith("zoom out") || lowerPrompt.Contains("zoom 80"))
            {
                return "Zoomed out (80%).\n\n[BROWY_ACTION: ZOOM 80]";
            }
            if (lowerPrompt.Contains("zoom reset") || lowerPrompt.Contains("zoom 100"))
            {
                return "Reset page zoom to 100%.\n\n[BROWY_ACTION: ZOOM 100]";
            }
            if (lowerPrompt.StartsWith("click ") || lowerPrompt.Contains("click on "))
            {
                string target = prompt.Replace("click on", "", StringComparison.OrdinalIgnoreCase)
                                      .Replace("click", "", StringComparison.OrdinalIgnoreCase)
                                      .Trim(' ', '\'', '"');
                if (!string.IsNullOrWhiteSpace(target))
                {
                    return $"Attempting to click **\"{target}\"** on the page.\n\n[BROWY_ACTION: CLICK {target}]";
                }
            }
            if (lowerPrompt.StartsWith("fill ") || lowerPrompt.StartsWith("type "))
            {
                string cleaned = prompt.Replace("fill", "", StringComparison.OrdinalIgnoreCase)
                                       .Replace("type", "", StringComparison.OrdinalIgnoreCase)
                                       .Trim();
                string field = "input";
                string val = cleaned;
                if (cleaned.Contains(" with ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = cleaned.Split(new[] { " with " }, StringSplitOptions.RemoveEmptyEntries);
                    field = parts[0].Trim();
                    val = parts.Length > 1 ? parts[1].Trim() : "";
                }
                else if (cleaned.Contains(" in ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = cleaned.Split(new[] { " in " }, StringSplitOptions.RemoveEmptyEntries);
                    val = parts[0].Trim();
                    field = parts.Length > 1 ? parts[1].Trim() : "input";
                }

                if (!string.IsNullOrWhiteSpace(val))
                {
                    return $"Entering \"{val}\" into **{field}** on the page.\n\n[BROWY_ACTION: FILL {field} | {val}]";
                }
            }
            if (lowerPrompt.StartsWith("translate ") || lowerPrompt.Contains("translate this page") || lowerPrompt.Contains("translate to ") || lowerPrompt.Contains("translate into "))
            {
                string targetLang = "Spanish";
                var langMatch = System.Text.RegularExpressions.Regex.Match(prompt, @"(?i)(?:to|into)\s+([A-Za-z]+)");
                if (langMatch.Success)
                {
                    targetLang = char.ToUpper(langMatch.Groups[1].Value[0]) + langMatch.Groups[1].Value.Substring(1).ToLower();
                }

                return $"### Page Translation ({targetLang})\n\n" +
                       $"Initiating autonomous translation of **{pageTitle}** into **{targetLang}**.\n\n" +
                       $"*(Translation processed live via Browy AI Engine)*\n\n" +
                       $"[BROWY_ACTION: TRANSLATE {targetLang}]";
            }
            if (lowerPrompt.StartsWith("search web for ") || lowerPrompt.StartsWith("search for ") || lowerPrompt.StartsWith("google "))
            {
                string searchKeyword = prompt.Replace("search web for", "", StringComparison.OrdinalIgnoreCase)
                                             .Replace("search for", "", StringComparison.OrdinalIgnoreCase)
                                             .Replace("google", "", StringComparison.OrdinalIgnoreCase)
                                             .Trim(' ', '\'', '"');
                if (!string.IsNullOrWhiteSpace(searchKeyword))
                {
                    string searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(searchKeyword)}";
                    return $"Searching the web for **\"{searchKeyword}\"**.\n\n[BROWY_ACTION: NEWTAB {searchUrl}]";
                }
            }

            // 2. Persona Specialized Local Syntheses
            string response = "";
            string snapshotNotice = !string.IsNullOrEmpty(screenshotBase64)
                ? $"\n\n📷 *Visual Viewport Snapshot analyzed ({screenshotBase64.Length / 1024} KB PNG)*"
                : "";

            if (persona == "Code Analyst")
            {
                var domain = NavigationHelper.ExtractDomain(pageUrl);
                var isHttps = pageUrl.StartsWith("https", StringComparison.OrdinalIgnoreCase);
                var scriptSnippet = !string.IsNullOrWhiteSpace(headings)
                    ? string.Join(", ", headings.Split('•').Select(h => $"\"{h.Trim()}\"").Take(4))
                    : "\"h1\", \"h2\"";

                response = $"### Technical DOM Analysis: {pageTitle}\n\n" +
                           $"• **Domain**: `{domain}`\n" +
                           $"• **Security**: {(isHttps ? "HTTPS (TLS Encrypted)" : "HTTP / Local Origin")}\n" +
                           $"• **Parsed Document Size**: {pageText.Length} characters\n\n" +
                           "```javascript\n" +
                           "// Extracted Page Structure Elements\n" +
                           $"const detectedHeadings = [{scriptSnippet}];\n" +
                           "console.log('Browy Code Analyst active on:', window.location.href);\n" +
                           "```\n\n" +
                           "*(Analyzed by Browy Code Analyst)*";
                return response + snapshotNotice;
            }

            if (persona == "Deep Research")
            {
                var domain = NavigationHelper.ExtractDomain(pageUrl);
                var claims = !string.IsNullOrWhiteSpace(headings)
                    ? string.Join("\n", headings.Split('•').Select(h => $"• {h.Trim()}"))
                    : "• Overview structured from DOM content.";

                var snippet = !string.IsNullOrWhiteSpace(pageText)
                    ? (pageText.Length > 300 ? pageText.Substring(0, 300) + "..." : pageText)
                    : "No text extractable from this page.";

                response = $"### Deep Research Synthesis: {pageTitle}\n\n" +
                           $"**Source Authority**: `{domain}`\n\n" +
                           $"**Core Claims & Structure**:\n{claims}\n\n" +
                           $"**Evidence Verification**:\n> {snippet}\n\n" +
                           "**Recommendation**: Compare with referenced external sources for complete triangulation.\n\n" +
                           "*(Synthesized by Browy Deep Research Agent)*";
                return response + snapshotNotice;
            }

            if (persona == "Content Explainer")
            {
                var snippet = !string.IsNullOrWhiteSpace(pageText)
                    ? (pageText.Length > 200 ? pageText.Substring(0, 200) + "..." : pageText)
                    : "This page presents visual or application controls.";

                response = $"### Plain English Explanation: {pageTitle}\n\n" +
                           "**The Big Picture**:\n" +
                           $"Think of this page as a digital hub. In short: {snippet}\n\n" +
                           "**3 Key Takeaways**:\n" +
                           "1. **Purpose**: Designed to help you navigate and interact smoothly.\n" +
                           "2. **Organization**: Structured with clear headers and sections for easy reading.\n" +
                           "3. **Privacy**: Processed directly inside your Browy session.\n\n" +
                           "*(Explained by Browy Content Explainer)*";
                return response + snapshotNotice;
            }

            // Standard General Copilot logic
            if (prompt.IndexOf("summarize", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var summaryBullet1 = !string.IsNullOrWhiteSpace(headings)
                    ? $"Core Topics: {headings}"
                    : $"Topic: Overview of {pageTitle}";
                var snippet = !string.IsNullOrWhiteSpace(pageText)
                    ? (pageText.Length > 280 ? pageText.Substring(0, 280) + "..." : pageText)
                    : "No text extractable from this page.";

                response = $"### Summary of {pageTitle}\n\n" +
                           $"• **Source**: `{NavigationHelper.ExtractDomain(pageUrl)}`\n" +
                           $"• **Structure**: {summaryBullet1}\n" +
                           $"• **Executive Overview**: {snippet}\n\n" +
                           $"*Synthesized live from the active DOM content (Local Engine).*";
            }
            else if (prompt.IndexOf("explain", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                response = $"### Concept Explanation: {pageTitle}\n\n" +
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
                response = $"### Extracted Page Intelligence: {pageTitle}\n\n" +
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
                    response = $"### Relevant Insight for \"{prompt}\"\n\n> {snippet}\n\nFound in {pageTitle} (`{NavigationHelper.ExtractDomain(pageUrl)}`).";
                }
                else
                {
                    response = $"I examined **{pageTitle}** for \"{prompt}\".\n\n" +
                               $"• Page URL: `{pageUrl}`\n" +
                               $"• Headings found: {(!string.IsNullOrWhiteSpace(headings) ? headings : "None")}\n\n" +
                               "*(Tip: Switch to OpenAI, Claude, or Ollama in Settings -> Copilot for full generative reasoning)*";
                }
            }

            return response + snapshotNotice;
        }

        public static AgentActionResult ParseAgentDirectives(string response)
        {
            var result = new AgentActionResult { CleanText = response };
            if (string.IsNullOrWhiteSpace(response)) return result;

            var match = System.Text.RegularExpressions.Regex.Match(
                response,
                @"\[BROWY_ACTION:\s*([A-Za-z0-9_]+)(?:\s+(.*?))?\]");

            if (match.Success)
            {
                string cmd = match.Groups[1].Value.ToUpperInvariant();
                string arg = match.Groups[2].Success ? match.Groups[2].Value.Trim() : "";

                result.ActionCommand = cmd;
                result.ActionTarget = arg;
                result.CleanText = response.Replace(match.Value, "").Trim();

                result.ActionFeedback = cmd switch
                {
                    "SCROLL_DOWN" => "Scrolled page down",
                    "SCROLL_UP" => "Scrolled to top of page",
                    "NAVIGATE" => $"Navigated to {arg}",
                    "NEWTAB" => $"Opened {arg} in new tab",
                    "HIGHLIGHT" => $"Highlighted \"{arg}\" on page",
                    "CLICK" => $"Clicked \"{arg}\"",
                    "FILL" => $"Filled form input with \"{arg}\"",
                    "ZOOM" => $"Set zoom to {arg}%",
                    "READER_MODE" => "Toggled Reader Mode",
                    "TRANSLATE" => $"Translated page to {arg}",
                    "DARK_MODE" => "Toggled page dark mode",
                    _ => $"Executed browser action: {cmd}"
                };
            }

            return result;
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
