using System.Collections.Concurrent;

namespace Kaladont.App
{
    public class Program
    {
        private static Task? OrganizingTask = null;
        private static ConcurrentBag<string> OrganizedWords = new ConcurrentBag<string>();
        private static ConcurrentDictionary<string, ConcurrentBag<string>> LettersGroups = new ConcurrentDictionary<string, ConcurrentBag<string>>();
        private static ConcurrentDictionary<string, ConcurrentBag<string>> LettersGroupsLocks = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        static void Main(string[] args)
        {
            // Before startin the app, make sure you have words.dic file in the same directory as the app with property "Copy to output directory" set to "Copy always / Copy if never"
            // Words.dic file is simple text file with words separated by new line

            Task.Run(async () =>
            {
                Directory.CreateDirectory(Constants.GrouppedWordsPath);
                OrganizingTask = OrganizeWordsAsync();

                if (Constants.LogInnerMessages || Constants.LogLastLoggingMessage)
                {
                    Console.WriteLine("Loading...");
                    await OrganizingTask;
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                }

                Console.WriteLine("Dobrodosli u igru kaladont!");
                Console.WriteLine("Izaberite opciju:");
                Console.WriteLine("1. Igraj");
                Console.WriteLine("2. Pravila");
                Console.WriteLine("3. Izlaz");
                Console.ReadLine();

                while (true)
                    InputWord();

            }).Wait();
        }

        private static void InputWord()
        {

            Console.WriteLine();
            Console.Write("Enter word: ");
            var word = Console.ReadLine();

            if(string.IsNullOrWhiteSpace(word))
            {
                Console.WriteLine("You must enter a word!");
                InputWord();
                return;
            }

            Console.WriteLine();
            var t1 = DateTime.UtcNow;
            OrganizingTask!.Wait();
            var t2 = DateTime.UtcNow;
            Console.WriteLine($"Player waited: {(t2 - t1).TotalSeconds.ToString("0")} seconds!");

            var firstTwo = word.Substring(word.Length - 2, 2).ToLower();
            var fileName = GenerateFileName(firstTwo);
            var myWord = File.ReadLines(Constants.GrouppedWordsPath + fileName).First();
            Console.WriteLine($"I say: {myWord}");

            // Implementation for exact game is not done since this is just a snippet
            // showing loading and organizing mass amount of data

            // To implement exact game:
            // Remove players word from specific file so app cant use it again
            // Pick any word from file which is associated with last 2 letters of players words
            // Remove that word from that file so it can't be used again
            // Track all used words in memory to inform player if he/app used same word twice
        }

        private static Task OrganizeWordsAsync()
        {
            return Task.Run(() =>
            {
                var t4 = DateTime.UtcNow;
                var t5 = DateTime.UtcNow;
                var totalItterations = 0;

                var parallelDegree = 20;
                var segmentSize = Constants.MaximumLinesToRead / parallelDegree;

                Parallel.For(0, parallelDegree, new ParallelOptions() { MaxDegreeOfParallelism = parallelDegree }, (i) =>
                {
                    var c = 0;
                    IEnumerable<string> segmentLines = File.ReadLines(Constants.WordsFilePath).Skip(i * segmentSize + c).Take(segmentSize);

                    Parallel.ForEach(segmentLines, new ParallelOptions() { MaxDegreeOfParallelism = 20 }, (line) =>
                    {
                        c++;
                        OrganizedWords.Add(line);

                        if (line.Length < 4 || line.Any(x => !char.IsLetter(x)))
                            return;

                        AppendToLetterGroup(line);
                        if (Constants.LogInnerMessages && DateTime.UtcNow - t4 > TimeSpan.FromSeconds(1))
                        {
                            if (Monitor.TryEnter(Constants.LoggingLock))
                            {
                                lock (Constants.LoggingLock)
                                {
                                    totalItterations++;
                                    Log(t4);
                                    t4 = DateTime.UtcNow;
                                }
                            }
                        }
                    });
                });

                if (Constants.LogLastLoggingMessage)
                    Log(t5, true);

                Parallel.ForEach(LettersGroups.Keys, (key) =>
                {
                    WriteWordsFromMemoryToFileWithoutLock(key);
                });
            });
        }

        private static void Log(DateTime interval, bool logWps = false)
        {
            var tms = (DateTime.UtcNow - interval).TotalMilliseconds;
            Console.WriteLine($"After {tms.ToString("#,##0")}ms I organized {OrganizedWords.Count.ToString("#,##0")} words!");

            if(logWps)
                Console.WriteLine($"WPS: {(OrganizedWords.Count / tms * 1000).ToString("#,##0")}");
        }

        private static void AppendToLetterGroup(string word)
        {
            var firstTwoLetters = word.Substring(0, 2).ToLower();
            if (!LettersGroups.ContainsKey(firstTwoLetters))
                LettersGroups.TryAdd(firstTwoLetters, new ConcurrentBag<string>());

            if (!LettersGroupsLocks.ContainsKey(firstTwoLetters))
                LettersGroupsLocks.TryAdd(firstTwoLetters, new ConcurrentBag<string>());

            LettersGroups[firstTwoLetters].Add(word);

            if (Monitor.TryEnter(LettersGroupsLocks[firstTwoLetters]) && LettersGroups[firstTwoLetters].Count > Constants.MaximumInMemoryWordsPerGroup)
                WriteWordsFromMemoryToFile(firstTwoLetters);
        }

        private static void WriteWordsFromMemoryToFile(string firstTwoLetters)
        {
            lock (LettersGroupsLocks[firstTwoLetters])
            {
                WriteWordsFromMemoryToFileWithoutLock(firstTwoLetters);
            }
        }

        private static void WriteWordsFromMemoryToFileWithoutLock(string firstTwoLetters)
        {
            var copy = new ConcurrentBag<string>(LettersGroups[firstTwoLetters]);
            LettersGroups[firstTwoLetters].Clear();

            using (var sw = new StreamWriter(Constants.GrouppedWordsPath + GenerateFileName(firstTwoLetters)))
                foreach (var w in copy)
                    sw.WriteLine(w);
        }

        private static string GenerateFileName(string letters) =>
            string.Format(Constants.GeneratedFileNameFormat, letters.ToLower());
    }
}