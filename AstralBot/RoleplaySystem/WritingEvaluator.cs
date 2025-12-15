using OpenNLP.Tools.SentenceDetect;

namespace AstralBot.RoleplaySystem
{
    internal class PostBreakdown()
    {
        internal string OriginalPost { get; set; } = string.Empty;
        internal int Paragraphs { get; set; } = 0;
        internal int Sentences { get; set; } = 0;
        internal int Words { get; set; } = 0;
        internal int Chars { get; set; } = 0;
        internal int Syllables { get; set; } = 0;
        internal int BaseExperience { get; set; } = 0;
        internal int LengthExperience { get; set; } = 0;
        internal double FleshKincaid { get; set; } = 0;
    }

    internal static class WritingEvaluator
    {
        internal static PostBreakdown?  EvaluatePost(string message, string character)
        {
            message = message.Replace("/me", character);

            int sentences;
            int words = 0;
            int chars = 0;
            int syllables = 0;
            int spamwords = 0;
            int paragraphs = 0;

            EnglishMaximumEntropySentenceDetector sd = new("../../../Dictionaries/EnglishSD.nbin");
            paragraphs = message.Split("\n".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Length;
            var parsedpost = sd.SentenceDetect(message);
            sentences = parsedpost.Length;

            foreach (var v in parsedpost)
            {
                var wordsInSentence = v.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).ToList();
                words += wordsInSentence.Count;

                foreach (var word in wordsInSentence)
                {
                    int syls = word.SyllableCount();
                    syllables += syls;
                    chars += word.Length;

                    if (word.Length > 10)
                    {
                        var groupedByLetter = word.GroupBy(c => c).Select(c => new { Letter = c.Key, Count = c.Count() });
                        if (groupedByLetter.Any(x => x.Count / word.Length > .5)) spamwords++;
                    }
                }
            }

            double flatA = .39;
            double flatB = 11.8;
            double flatC = 15.59;
            double wordsOverSentences = (double)words / sentences;
            double syllablesOverWords = (double)syllables / words;
            double fleshkaincaid = Math.Round(flatA * wordsOverSentences + flatB * syllablesOverWords - flatC, 2);

            double LenXp = ScoreFromLength(chars, 200, 800);
            double experience = ScoreFromFk(fleshkaincaid);

            return new PostBreakdown() { OriginalPost = message, Paragraphs = paragraphs, Sentences = sentences, Words = words, Chars = chars, Syllables = syllables, FleshKincaid = fleshkaincaid, BaseExperience = (int)Math.Round(experience, 0), LengthExperience = (int)Math.Round(LenXp, 0) };
        }

        /// <summary>
        /// Returns total length score in [1..50].
        /// Base length: 10..40 (calibrated so shortLen->10, midLen->30, capLen->40)
        /// Deviation bonus: up to +/-10 based on difference from avgLen
        /// </summary>
        public static int ScoreFromLength(
            int postChars,
            int shortLen,
            int midLen,
            int capLen = 4096)
        {
            // Clamp post to platform cap
            postChars = Math.Clamp(postChars, 0, capLen);

            // Guard rails
            if (shortLen < 0) shortLen = 0;
            if (midLen <= shortLen) midLen = shortLen + 1;
            if (capLen <= midLen) capLen = midLen + 1;

            // ---------- Base score (10..40) ----------
            double x = (postChars - shortLen) / (double)(capLen - shortLen);
            x = Clamp01(x);

            double xMid = (midLen - shortLen) / (double)(capLen - shortLen);
            xMid = Clamp01(xMid);

            double target = 2.0 / 3.0;
            double p = (xMid > 1e-6 && xMid < 0.999999)
                ? Math.Log(target) / Math.Log(xMid)
                : 1.0;

            double f = Math.Pow(x, p);
            double baseScore = 10.0 + 30.0 * f;   // 10..40

            // ---------- Scale rawScore → [1..50] ----------
            const double rawMin = 0.0;
            const double rawMax = 50.0;

            double scaled =
                1.0 + ((baseScore - rawMin) / (rawMax - rawMin)) * 49.0;

            return Math.Clamp((int)Math.Round(scaled), 1, 50);
        }

        private static double Clamp01(double v)
            => v < 0 ? 0 : (v > 1 ? 1 : v);

        public static int ScoreFromFk(
            double postFk,
            double fkMin = -3.0,
            double fkMax = 14.0)
        {
            // --- 1) Base score from raw FK: 1..40 ---
            // Normalize FK into [0..1]
            double t = (postFk - fkMin) / (fkMax - fkMin);
            t = Math.Clamp(t, 0.0, 1.0);

            // Optional: gentle curve so it doesn't grow too fast early
            // (set exp=1.0 for linear)
            const double exp = 1.0;
            t = Math.Pow(t, exp);

            double baseScore = 1.0 + 19.0 * t; // 1..40

            // --- Total: clamp to 1..50 ---
            return (int)Math.Round(baseScore);
        }
    }
}