namespace AstralBot
{
    public static class ConsoleWriter
    {
        static bool _verbose = true;

        public static void SetVerbosity(bool enabled)
        {
            _verbose = enabled;
        }

        public static void Write(string text, bool writeline = true)
        {
            if (_verbose)
            {
                if (writeline) Console.WriteLine(DateTime.Now.ToShortTimeString() + ": " + text);
                else Console.Write(DateTime.Now.ToShortTimeString() + ": " + text);
            }
        }
    }
}
