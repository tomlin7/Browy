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

        [Fact]
        public void ParseAgentDirectives_ParsesActionCommandsAndCleansText()
        {
            string raw = "I will scroll down for you.\n\n[BROWY_ACTION: SCROLL_DOWN]";
            var action = AiCopilotService.ParseAgentDirectives(raw);

            Assert.True(action.HasAction);
            Assert.Equal("SCROLL_DOWN", action.ActionCommand);
            Assert.Equal("Scrolled page down", action.ActionFeedback);
            Assert.DoesNotContain("[BROWY_ACTION", action.CleanText);
            Assert.Equal("I will scroll down for you.", action.CleanText);
        }

        [Fact]
        public void ParseAgentDirectives_ParsesParameterizedHighlightAndNavigate()
        {
            string rawHighlight = "Found occurrences of performance.\n\n[BROWY_ACTION: HIGHLIGHT performance]";
            var hlAction = AiCopilotService.ParseAgentDirectives(rawHighlight);
            Assert.True(hlAction.HasAction);
            Assert.Equal("HIGHLIGHT", hlAction.ActionCommand);
            Assert.Equal("performance", hlAction.ActionTarget);
            Assert.Equal("Highlighted \"performance\" on page", hlAction.ActionFeedback);

            string rawNav = "Navigating to GitHub.\n\n[BROWY_ACTION: NAVIGATE https://github.com]";
            var navAction = AiCopilotService.ParseAgentDirectives(rawNav);
            Assert.True(navAction.HasAction);
            Assert.Equal("NAVIGATE", navAction.ActionCommand);
            Assert.Equal("https://github.com", navAction.ActionTarget);
            Assert.Equal("Navigated to https://github.com", navAction.ActionFeedback);
        }

        [Fact]
        public void GenerateLocalSynthesis_SpecializesForAgentPersonas()
        {
            // Code Analyst
            string codeResult = AiCopilotService.GenerateLocalSynthesis(
                "audit this page",
                "body text with scripts",
                "Header 1 • Header 2",
                "Tech Blog",
                "https://tech.io/article",
                persona: "Code Analyst");

            Assert.Contains("Code Analyst", codeResult);
            Assert.Contains("Technical DOM Analysis", codeResult);
            Assert.Contains("javascript", codeResult);

            // Deep Research
            string researchResult = AiCopilotService.GenerateLocalSynthesis(
                "analyze research claims",
                "Research paper text comparing algorithms.",
                "Abstract • Methodology • Results",
                "Paper Title",
                "https://arxiv.org/abs/1234",
                persona: "Deep Research");

            Assert.Contains("Deep Research Synthesis", researchResult);
            Assert.Contains("Source Authority", researchResult);

            // Content Explainer
            string explainerResult = AiCopilotService.GenerateLocalSynthesis(
                "explain this simply",
                "Complex quantum mechanics paper",
                "Qubits • Entanglement",
                "Quantum Computing",
                "https://nature.com/articles/qc",
                persona: "Content Explainer");

            Assert.Contains("Plain English Explanation", explainerResult);
            Assert.Contains("The Big Picture", explainerResult);
        }

        [Fact]
        public void GenerateLocalSynthesis_IncludesScreenshotNotice_WhenSnapshotPresent()
        {
            string fakeBase64 = new string('A', 4096);
            string result = AiCopilotService.GenerateLocalSynthesis(
                "summarize page",
                "Text content",
                "Headings",
                "Test Page",
                "https://example.com",
                screenshotBase64: fakeBase64);

            Assert.Contains("Visual Viewport Snapshot analyzed", result);
        }
    }
}
