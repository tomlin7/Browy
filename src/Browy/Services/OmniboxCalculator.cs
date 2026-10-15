using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Browy.Services
{
    public static class OmniboxCalculator
    {
        private static readonly Regex PercentageOfRegex = new(
            @"^(\d+(\.\d+)?)\s*%\s*(of|\*)\s*(\d+(\.\d+)?)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static bool TryEvaluate(string? query, out string formattedResult, out double numericValue)
        {
            formattedResult = string.Empty;
            numericValue = 0;

            if (string.IsNullOrWhiteSpace(query))
                return false;

            string expr = query.Trim();

            // Must contain at least one math operator to qualify as a math query
            if (!ContainsMathOperators(expr))
                return false;

            // Check percentage syntax first: e.g. "15% of 200" or "20% * 80"
            var pctMatch = PercentageOfRegex.Match(expr);
            if (pctMatch.Success)
            {
                if (double.TryParse(pctMatch.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double pct) &&
                    double.TryParse(pctMatch.Groups[4].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double baseVal))
                {
                    numericValue = (pct / 100.0) * baseVal;
                    formattedResult = FormatResult(numericValue);
                    return true;
                }
            }

            // Normalize 'x' or 'X' surrounded by numbers or whitespace to '*'
            expr = NormalizeExpression(expr);

            try
            {
                var parser = new MathParser(expr);
                double res = parser.Parse();
                if (double.IsNaN(res) || double.IsInfinity(res))
                {
                    return false;
                }

                numericValue = Math.Round(res, 6);
                formattedResult = FormatResult(numericValue);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ContainsMathOperators(string expr)
        {
            if (expr.Contains('%')) return true;
            if (expr.Contains('+') || expr.Contains('/') || expr.Contains('^')) return true;
            if (expr.Contains('*')) return true;

            // For '-' or 'x', ensure it's used as an operator between numbers
            if (Regex.IsMatch(expr, @"\d+\s*[-xX*+/]\s*\d+")) return true;

            return false;
        }

        private static string NormalizeExpression(string expr)
        {
            // Replace 'x' or 'X' between numbers with '*'
            expr = Regex.Replace(expr, @"(?<=\d|\))\s*[xX]\s*(?=\d|\()", " * ");
            return expr;
        }

        private static string FormatResult(double val)
        {
            if (Math.Abs(val % 1) < 0.0000001)
            {
                return $"= {val:N0}";
            }
            return $"= {val.ToString("0.######", CultureInfo.InvariantCulture)}";
        }

        private class MathParser
        {
            private readonly string _text;
            private int _pos;

            public MathParser(string text)
            {
                _text = text;
                _pos = 0;
            }

            public double Parse()
            {
                double result = ParseExpression();
                SkipWhitespace();
                if (_pos < _text.Length)
                {
                    throw new FormatException($"Unexpected character: {_text[_pos]}");
                }
                return result;
            }

            private double ParseExpression()
            {
                double result = ParseTerm();
                while (true)
                {
                    SkipWhitespace();
                    if (_pos < _text.Length && _text[_pos] == '+')
                    {
                        _pos++;
                        result += ParseTerm();
                    }
                    else if (_pos < _text.Length && _text[_pos] == '-')
                    {
                        _pos++;
                        result -= ParseTerm();
                    }
                    else
                    {
                        break;
                    }
                }
                return result;
            }

            private double ParseTerm()
            {
                double result = ParseFactor();
                while (true)
                {
                    SkipWhitespace();
                    if (_pos < _text.Length && _text[_pos] == '*')
                    {
                        _pos++;
                        result *= ParseFactor();
                    }
                    else if (_pos < _text.Length && _text[_pos] == '/')
                    {
                        _pos++;
                        double divisor = ParseFactor();
                        if (Math.Abs(divisor) < 0.0000000001) throw new DivideByZeroException();
                        result /= divisor;
                    }
                    else
                    {
                        break;
                    }
                }
                return result;
            }

            private double ParseFactor()
            {
                SkipWhitespace();
                if (_pos >= _text.Length) throw new FormatException("Unexpected end of expression");

                if (_text[_pos] == '+')
                {
                    _pos++;
                    return ParseFactor();
                }
                if (_text[_pos] == '-')
                {
                    _pos++;
                    return -ParseFactor();
                }

                if (_text[_pos] == '(')
                {
                    _pos++;
                    double val = ParseExpression();
                    SkipWhitespace();
                    if (_pos >= _text.Length || _text[_pos] != ')')
                        throw new FormatException("Missing closing parenthesis");
                    _pos++;
                    return val;
                }

                int start = _pos;
                bool hasDot = false;
                while (_pos < _text.Length && (char.IsDigit(_text[_pos]) || (!hasDot && _text[_pos] == '.')))
                {
                    if (_text[_pos] == '.') hasDot = true;
                    _pos++;
                }

                if (start == _pos) throw new FormatException($"Invalid number at position {_pos}");

                string numStr = _text.Substring(start, _pos - start);
                if (!double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                {
                    throw new FormatException($"Could not parse number: {numStr}");
                }

                SkipWhitespace();
                if (_pos < _text.Length && _text[_pos] == '%')
                {
                    _pos++;
                    number /= 100.0;
                }

                return number;
            }

            private void SkipWhitespace()
            {
                while (_pos < _text.Length && char.IsWhiteSpace(_text[_pos]))
                {
                    _pos++;
                }
            }
        }
    }
}
