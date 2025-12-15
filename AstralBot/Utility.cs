namespace AstralBot
{
    public static class StringExtension
    {
        public static int SyllableCount(this string word)
        {
            if (string.IsNullOrWhiteSpace(word))
                return 0;

            word = new string([.. word
                .ToLowerInvariant()
                .Where(char.IsLetter)]);

            if (word.Length == 0)
                return 0;

            static bool IsVowel(char c) => "aeiouy".Contains(c);

            int count = 0;
            bool lastWasVowel = false;

            foreach (char c in word)
            {
                bool isVowel = IsVowel(c);
                if (isVowel && !lastWasVowel) count++;
                lastWasVowel = isVowel;
            }

            // Silent 'e' (but not ...le after a consonant, e.g. "table")
            if (word.EndsWith('e'))
            {
                if (!(word.EndsWith("le") && word.Length > 2 && !IsVowel(word[^3])))
                    count--;
            }

            // -ed: often silent, except when it forms its own syllable (wanted, needed)
            if (word.EndsWith("ed") && word.Length > 2)
            {
                char beforeEd = word[^3];
                // if ...ted or ...ded -> adds syllable, so don't subtract
                if (!(beforeEd == 't' || beforeEd == 'd'))
                    count--;
            }

            // -es: often silent, except when it forms its own syllable (boxes, wishes, judges)
            if (word.EndsWith("es") && word.Length > 2)
            {
                char beforeEs = word[^3];
                // if ends with s, x, z, ch, sh -> adds syllable, so don't subtract
                bool addsSyllable =
                    beforeEs == 's' || beforeEs == 'x' || beforeEs == 'z' ||
                    word.EndsWith("ches") || word.EndsWith("shes");

                if (!addsSyllable)
                    count--;
            }

            // Clamp: English words should be at least 1 syllable for FK purposes
            if (count < 1) count = 1;

            return count;
        }
    }
}
