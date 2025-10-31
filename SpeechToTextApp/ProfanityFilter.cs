using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SpeechToTextApp
{
    public class ProfanityFilter
    {
        private static readonly string[] PatternStrings = new[]
        {
            @"\bdamn(?:ed|ing)?\b",
            @"\bhell\b",
            @"\bshit(?:ty|ting|ted|s)?\b",
            @"\bfuck(?:er|ers|ing|ed|s)?\b",
            @"\bmotherf(?:ucker|uckers|ucking|uckin|ucks?)\b",
            @"\bbitch(?:es|ing|y)?\b",
            @"\basshole(?:s)?\b",
            @"\bbastard(?:s)?\b",
            @"\bdick(?:head|heads|s)?\b",
            @"\bpiss(?:ed|ing|es)?\b",
            @"\bcrap(?:py|s)?\b"
        };

        private readonly List<Regex> _patterns;

        public ProfanityFilter()
        {
            _patterns = PatternStrings
                .Select(pattern => new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .ToList();
        }

        public string Clean(string input)
        {
            var result = input;
            foreach (var rx in _patterns)
            {
                result = rx.Replace(result, m => new string('*', m.Value.Length));
            }
            return result;
        }
    }
}

