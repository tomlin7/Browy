using Browy.Services;
using Xunit;

namespace Browy.Tests
{
    public class OmniboxCalculatorTests
    {
        [Theory]
        [InlineData("2 + 2", "= 4", 4)]
        [InlineData("100 - 37", "= 63", 63)]
        [InlineData("12 * 12", "= 144", 144)]
        [InlineData("15 x 10", "= 150", 150)]
        [InlineData("500 / 4", "= 125", 125)]
        [InlineData("(10 + 20) * 3", "= 90", 90)]
        [InlineData("2.5 + 3.75", "= 6.25", 6.25)]
        public void TryEvaluate_CalculatesArithmeticAccurately(string expression, string expectedFormat, double expectedVal)
        {
            bool success = OmniboxCalculator.TryEvaluate(expression, out string formatted, out double val);
            Assert.True(success);
            Assert.Equal(expectedFormat, formatted);
            Assert.Equal(expectedVal, val, precision: 4);
        }

        [Theory]
        [InlineData("15% of 200", "= 30", 30)]
        [InlineData("25% * 80", "= 20", 20)]
        [InlineData("50% of 150", "= 75", 75)]
        public void TryEvaluate_HandlesPercentageExpressions(string expression, string expectedFormat, double expectedVal)
        {
            bool success = OmniboxCalculator.TryEvaluate(expression, out string formatted, out double val);
            Assert.True(success);
            Assert.Equal(expectedFormat, formatted);
            Assert.Equal(expectedVal, val, precision: 4);
        }

        [Theory]
        [InlineData("https://google.com")]
        [InlineData("hello world")]
        [InlineData("!yt lo-fi music")]
        [InlineData("browy://settings")]
        [InlineData("")]
        [InlineData(null)]
        public void TryEvaluate_IgnoresNonMathQueries(string? query)
        {
            bool success = OmniboxCalculator.TryEvaluate(query, out _, out _);
            Assert.False(success);
        }

        [Fact]
        public void TryEvaluate_HandlesDivisionByZeroGracefully()
        {
            bool success = OmniboxCalculator.TryEvaluate("42 / 0", out _, out _);
            Assert.False(success);
        }
    }
}
