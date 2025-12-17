using System.Text;

namespace AstralBot.RoleplaySystem
{
    public sealed class BbCodeStripResult
    {
        public string Text { get; init; } = "";

        // Total times [color=...] opened (valid colors only)
        public int ColorOpenCount { get; init; }

        // Per-color counts (canonical lowercase keys)
        public Dictionary<string, int> ColorOpenCounts { get; init; }
            = new(StringComparer.OrdinalIgnoreCase);

        // Optional: colors opened in order
        public List<string> OpenedColorsInOrder { get; init; } = new();
    }

    public static class BBCodeParser
    {
        private static readonly HashSet<string> AllowedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        "red","orange","yellow","green","cyan","purple","blue","pink","black","brown","white","gray"
    };

        // All tags you want stripped (opening/closing), preserving inner text.
        private static readonly HashSet<string> StripTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "noparse","spoiler","eicon","icon","user","url",
        "sub","sup","s","u","i","b","color"
    };

        public static BbCodeStripResult StripAndTrackColors(string message)
        {
            if (string.IsNullOrEmpty(message))
                return new BbCodeStripResult { Text = "" };

            var output = new StringBuilder(message.Length);
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var openedInOrder = new List<string>();
            int totalColorOpens = 0;

            int i = 0;
            while (i < message.Length)
            {
                if (message[i] == '[' && TryParseTag(message, i, out var tag))
                {
                    // Track [color=...]
                    if (!tag.IsClosing && tag.NameEquals("color"))
                    {
                        string color = (tag.Attribute ?? "").Trim();
                        if (AllowedColors.Contains(color))
                        {
                            totalColorOpens++;
                            string canonical = color.ToLowerInvariant();
                            openedInOrder.Add(canonical);
                            counts[canonical] = counts.TryGetValue(canonical, out int c) ? c + 1 : 1;
                        }
                    }

                    // Strip tag if supported
                    if (StripTags.Contains(tag.Name))
                    {
                        i = tag.EndIndex + 1; // skip the whole [tag...]
                        continue;
                    }
                    // else: unknown tag => fall through and treat as literal '['
                }

                output.Append(message[i]);
                i++;
            }

            return new BbCodeStripResult
            {
                Text = output.ToString(),
                ColorOpenCount = totalColorOpens,
                ColorOpenCounts = counts,
                OpenedColorsInOrder = openedInOrder
            };
        }

        // ----------------- Helpers -----------------

        private readonly struct ParsedTag
        {
            public ParsedTag(string name, bool isClosing, string? attribute, int endIndex)
            {
                Name = name;
                IsClosing = isClosing;
                Attribute = attribute;
                EndIndex = endIndex;
            }
            public string Name { get; }
            public bool IsClosing { get; }
            public string? Attribute { get; }
            public int EndIndex { get; } // index of ']'

            public bool NameEquals(string other) =>
                string.Equals(Name, other, StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseTag(string s, int startIndex, out ParsedTag tag)
        {
            tag = default;

            if (startIndex < 0 || startIndex >= s.Length || s[startIndex] != '[')
                return false;

            int closeBracket = s.IndexOf(']', startIndex + 1);
            if (closeBracket < 0)
                return false;

            string inside = s.Substring(startIndex + 1, closeBracket - startIndex - 1).Trim();
            if (inside.Length == 0)
                return false;

            bool isClosing = inside.StartsWith("/", StringComparison.Ordinal);
            if (isClosing)
                inside = inside.Substring(1).Trim();

            string name;
            string? attr = null;

            int eq = inside.IndexOf('=');
            if (eq >= 0)
            {
                name = inside.Substring(0, eq).Trim();
                attr = inside.Substring(eq + 1).Trim();
                attr = TrimWrappingQuotes(attr);
            }
            else
            {
                name = inside.Trim();
            }

            if (name.Length == 0)
                return false;

            tag = new ParsedTag(name, isClosing, attr, closeBracket);
            return true;
        }

        private static string TrimWrappingQuotes(string s)
        {
            if (s.Length >= 2)
            {
                if (s[0] == '"' && s[^1] == '"' || s[0] == '\'' && s[^1] == '\'')
                    return s.Substring(1, s.Length - 2);
            }
            return s;
        }
    }
}