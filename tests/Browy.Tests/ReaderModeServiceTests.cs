using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class ReaderModeServiceTests
    {
        [Fact]
        public void GetReaderHtml_IncludesArticleAndReadAloudControls()
        {
            var article = new ReaderArticle
            {
                Title = "Exploring Deep Learning in C#",
                Author = "Jane Doe",
                Date = "Oct 14, 2026",
                Domain = "techblog.dev",
                Url = "https://techblog.dev/deep-learning",
                Content = "<p>Deep learning is transforming modern software development.</p>",
                WordCount = 450,
                ReadingTimeMinutes = 2
            };

            string html = ReaderModeService.GenerateReaderHtml(article);

            Assert.Contains("Exploring Deep Learning in C#", html);
            Assert.Contains("Jane Doe", html);
            Assert.Contains("techblog.dev", html);
            Assert.Contains("Deep learning is transforming modern software development.", html);
            Assert.Contains("Read Aloud", html);
            Assert.Contains("toggleReadAloud", html);
            Assert.Contains("speechSynthesis", html);
        }

        [Fact]
        public void ArticleDetectionScript_IsNonEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(ReaderModeService.ArticleDetectionScript));
            Assert.Contains("querySelectorAll", ReaderModeService.ArticleDetectionScript);
        }

        [Fact]
        public void ExtractionScript_IsNonEmpty()
        {
            Assert.False(string.IsNullOrWhiteSpace(ReaderModeService.ExtractionScript));
            Assert.Contains("extractArticleContent", ReaderModeService.ExtractionScript);
        }
    }
}
