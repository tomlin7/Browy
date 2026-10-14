using System.Threading.Tasks;
using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class AiCopilotServiceTests
    {
        [Fact]
        public void BuildSystemPrompt_ContainsUrlAndHeadings()
        {
            string prompt = AiCopilotService.BuildSystemPrompt(
                "Sample page content about testing browsers.",
                "Intro • Overview • Features",
                "Browy Browser",
                "https://browy.org");

            Assert.Contains("Browy Browser", prompt);
            Assert.Contains("https://browy.org", prompt);
            Assert.Contains("Intro • Overview • Features", prompt);
            Assert.Contains("Sample page content", prompt);
        }

        [Fact]
        public void GenerateLocalSynthesis_HandlesSummarizePrompt()
        {
            string result = AiCopilotService.GenerateLocalSynthesis(
                "summarize this article",
                "Browy is an open-source modern web browser built for Windows 11.",
                "Overview",
                "About Browy",
                "https://github.com/tomlin7/Browy");

            Assert.Contains("Summary of About Browy", result);
            Assert.Contains("Overview", result);
            Assert.Contains("github.com", result);
        }

        [Fact]
        public void GenerateLocalSynthesis_HandlesExplainPrompt()
        {
            string result = AiCopilotService.GenerateLocalSynthesis(
                "explain what this page is doing",
                "Detailed technical specs on WebView2 integration.",
                "Architecture",
                "Architecture Guide",
                "https://github.com/tomlin7/Browy/docs");

            Assert.Contains("Concept Explanation: Architecture Guide", result);
            Assert.Contains("Key Takeaways", result);
        }

        [Fact]
        public async Task QueryCopilotAsync_FallsBackGracefullyWhenKeyMissing()
        {
            // Temporarily set OpenAI without key
            string originalProvider = BrowserSettings.Current.AiProvider;
            string originalKey = BrowserSettings.Current.OpenAiApiKey;

            try
            {
                BrowserSettings.Current.AiProvider = "OpenAI";
                BrowserSettings.Current.OpenAiApiKey = "";

                string response = await AiCopilotService.QueryCopilotAsync("hello", "text", "head", "title", "https://url.com");
                Assert.Contains("API Key Missing", response);
            }
            finally
            {
                BrowserSettings.Current.AiProvider = originalProvider;
                BrowserSettings.Current.OpenAiApiKey = originalKey;
            }
        }
    }
}
