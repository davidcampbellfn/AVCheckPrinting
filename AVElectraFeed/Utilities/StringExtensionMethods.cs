using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
namespace AVCheckPrinting.Utilities
{
    public static class StringExtensionMethods
    {
        /// <summary>
        /// Gets lines in a string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="removeEmptyLines"></param>
        /// <returns></returns>
        public static IEnumerable<String> GetLines(this string str, bool removeEmptyLines = false)
        {
            return str.Split(new[] { "\r\n", "\r", "\n" },
                removeEmptyLines ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None);
        }

        /// <summary>
        /// Remove empty lines in a string.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static String RemoveEmptyLines(this string str)
        {
            return Regex.Replace(str, @"^\s+$[\r\n]*", "", RegexOptions.Multiline);
        }

        /// <summary>
        /// Returns a valid decimal from the string given.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static decimal ToDecimal(this string str)
        {
            decimal decimalAmt;
            if (Decimal.TryParse(str, out decimalAmt))
            {
                return decimalAmt;
            }

            throw new ArgumentException("String cannot be evaluated for decimal: " + str);
        }

        /// <summary>
        /// Converts camel/pascal cased strings to space delimited.
        /// </summary>
        /// <param name="@this">A camel-cased or pascal-cased string</param>
        /// <returns>A space-delimited string</returns>
        /// <example>Converts "AddressLine1" to "Address Line 1"</example>
        public static string SpaceDelimitCaps(this string @this)
        {
            return Regex.Replace(@this, "([a-z](?=[A-Z0-9])|[A-Z](?=[A-Z][a-z]))", "$1 ");
        }

        public static string SubstringAfter(this string @this, char value)
        {
            var index = @this.IndexOf(value);
            if (index < 0) { return String.Empty; }
            return @this.Substring(index + 1);
        }
        public static string SubstringAfter(this string @this, string value)
        {
            var index = @this.IndexOf(value, StringComparison.Ordinal);
            if (index < 0) { return String.Empty; }
            return @this.Substring(index + value.Length);
        }

        public static bool IsLike(this string @this, string pattern)
        { 
            return Regex.IsMatch(@this, pattern);
        }

        public static List<int> ParsePageNumbers(this string input, int maxNb = Int32.MaxValue)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var pageNbs = new List<int>();
            var pageRanges = ParsePageRanges(input, maxNb);

            foreach (var pageRange in pageRanges)
            {
                var start = pageRange.Item1;
                var end = pageRange.Item2;

                if (start == end)
                    pageNbs.Add(start);
                else
                    pageNbs.AddRange(Enumerable.Range(start, end - start + 1));
            }

            pageNbs.Sort();
            return pageNbs.Distinct().ToList();
        }

        public static List<Tuple<int, int>> ParsePageRanges(this string input, int maxNb = Int32.MaxValue)
        {
            if (string.IsNullOrEmpty(input))
                return null;

            var pageRanges = new List<Tuple<int, int>>();

            //Prepare string for parsing, spaces can be used as a separator
            input = Regex.Replace(input, @"\s*-\s*", "-");
            input = Regex.Replace(input, @"\s*,\s*", ",");
            input = Regex.Replace(input, @"\s+", ",");

            foreach (string pageStr in input.Split(','))
            {
                if (!pageStr.Contains("-"))
                {
                    // try and get the number
                    int num;
                    if (int.TryParse(pageStr, out num) && num <= maxNb)
                        pageRanges.Add(Tuple.Create(num, num));
                }
                else
                {
                    // otherwise we might have a range
                    // split on the range delimiter
                    string[] subs = pageStr.Split('-');
                    int start, end;

                    // now see if we can parse a start and end
                    if (subs.Length > 1 && int.TryParse(subs[0], out start) && int.TryParse(subs[1], out end))
                    {
                        end = Math.Min(end, maxNb);
                        if (end >= start)
                            pageRanges.Add(Tuple.Create(start, end));
                    }
                }
            }

            pageRanges.Sort();
            return pageRanges;
        }
    }
}