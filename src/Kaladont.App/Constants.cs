namespace Kaladont.App
{
    public static class Constants
    {
        public const bool LogInnerMessages = true;
        public const bool LogLastLoggingMessage = true;
        public static object LoggingLock = new object();
        public const string WordsFileName = "words.dic";
        public const int MaximumLinesToRead = Int32.MaxValue;
        public const int MaximumInMemoryWordsPerGroup = 100000;
        public static string GrouppedWordsPath = Path.Combine(Directory.GetCurrentDirectory(), "words");
        public static string WordsFilePath = Path.Combine(Directory.GetCurrentDirectory(), WordsFileName);
        public const string GeneratedFileNameFormat = "{0}.w";
    }
}
