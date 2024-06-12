namespace core.profanity;

using System.Text;
using System.Text.Json;

public class Profanity
{
    /// <summary>
    /// The max number of additional words forming a swear word. For example:
    /// hand job = 1
    /// this is a fish = 3
    /// </summary>
    private int maxNumberCombinations = 1;

    /// <summary>
    /// The hash set to store the censored words in
    /// </summary>
    private HashSet<string> CensorWordSet = null;

    // Characters for creating variants
    private readonly Dictionary<char, List<char>> _charsMapping =
            new()
            {
                { 'a', ['a', '@', '*', '4'] },
                { 'i', ['i', '*', 'l', '1'] },
                { 'o', ['o', '*', '0', '@'] },
                { 'u', ['u', '*', 'v'] },
                { 'v', ['v', '*', 'u'] },
                { 'l', ['l', '1'] },
                { 'e', ['e', '*', '3'] },
                { 's', ['s', '$', '5'] }
            };

    // Data directory, in case the user wants to override the data path
    private readonly string? dataDir = null;

    /// <summary>
    /// The profanity filter
    /// </summary>
    /// <param name="dataDir">Optional directory to search for alphabetic_unicode.json and profanity_wordlist.txt in</param>
    public Profanity(string? dataDir = null)
    {
        this.dataDir = dataDir;
        Utils.LoadUnicodeSymbols(dataDir);
    }

    /// <summary>
    /// This flag is used mostly for unit test compatibility.
    /// If you need exact compatibility with the Python better_profanity,
    /// you should set this to true. 
    /// </summary>
    private bool originalBehaviorMode = false;

    /// <summary>
    /// Sets the original behavior mode flag 
    /// </summary>
    /// <param name="value">originalBehaviorMode value</param>
    public void SetOriginalBehaviorMode(bool value) => originalBehaviorMode = value;

    private int CountNonAllowedCharacters(string word)
        => word.Count(c => !Utils.AllowedCharacters.Contains(c.ToString()));

    /// <summary>
    /// Adds words to the censored word set.
    /// </summary>
    /// <param name="newWords">An IEnumerable<string> of words to add</param>
    public void AddCensorWords(IEnumerable<string> newWords)
    {
        foreach (var word in newWords) CensorWordSet.Add(word);
    }

    /// <summary>
    /// Sets the censored word list.
    /// If `customWords` is null, we'll just read the word list from profanity_wordlist.txt. 
    /// </summary>
    /// <param name="customWords">An IEnumerable<string> of words to add</param>
    public void LoadCensorWords(IEnumerable<string> customWords = null)
    {
        var tempWords = customWords ?? ReadWordList();

        var allCensorWords = new List<string>();
        foreach (var word in tempWords)
        {
            var tempWord = word.ToLowerInvariant();
            var nonAllowedCharacterCount = CountNonAllowedCharacters(word);
            if (nonAllowedCharacterCount > maxNumberCombinations)
            {
                maxNumberCombinations = nonAllowedCharacterCount;
            }

            allCensorWords.AddRange(GeneratePatternsFromWord(tempWord));
        }

        CensorWordSet = allCensorWords.ToHashSet();
    }

    /// <summary>
    /// Return words from file `profanity_wordlist.txt`.
    /// </summary>
    /// <returns>A list of words</returns>
    private List<string> ReadWordList()
    {
        var path = "profanity_wordlist.txt";
        if (dataDir != null)
        {
            path = Path.Combine(dataDir + "/", path);
        }

        return File.ReadAllLines(path, Encoding.UTF8)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    private IEnumerable<string> GeneratePatternsFromWord(string word)
    {
        var combos = word.Select(c => !_charsMapping.ContainsKey(c) ? [c] : _charsMapping[c]).ToList();
        var product = combos.CartesianProduct();
        return product.Select(combo => string.Join("", combo)).ToList();
    }

    private string GetReplacementForSwearWord(char censorChar, int count = 4) =>
        // 4 is hardcoded for original behavior mode
        originalBehaviorMode ? new string(censorChar, 4) : new string(censorChar, count);

    /// <summary>
    /// Return true if the input text has any swear words.
    /// </summary>
    /// <param name="s">Input text</param>
    /// <returns>If the input text has any swear words</returns>
    public bool ContainsProfanity(string s) => s != Censor(s);

    /// <summary>
    /// Return a list of next wordsIndices after the input index.
    /// </summary>
    /// <param name="text">Input text</param>
    /// <param name="wordsIndices">Original wordsIndices</param>
    /// <param name="startIndex">The index to start searching at</param>
    /// <returns></returns>
    private List<(string, int)> UpdateNextWordsIndices(string text, List<(string, int)> wordsIndices, int startIndex)
    {
        // Python: not set() = true
        if (wordsIndices == null || wordsIndices.Count < 1)
            wordsIndices = Utils.GetNextWords(text, startIndex, maxNumberCombinations);
        else
        {
            wordsIndices.RemoveRange(0, 2);
            if (wordsIndices.Count > 0 && wordsIndices[^1].Item1 != "")
            {
                wordsIndices.AddRange(Utils.GetNextWords(text, wordsIndices[^1].Item2, 1));
            }
        }

        return wordsIndices;
    }

    /// <summary>
    /// Replace the swear words with censor characters.
    /// </summary>
    /// <param name="s">The input text</param>
    /// <param name="censorChar">The character to censor text with</param>
    /// <returns></returns>
    private string HideSwearWords(string s, char censorChar)
    {
        var nextWordStartIndex = Utils.GetStartIndexOfNextWord(s, 0);

        // If there are no words in the text, return the raw text without parsing
        if (nextWordStartIndex >= s.Length - 1)
        {
            return s;
        }

        var censoredText = "";
        var curWord = "";
        int skipIndex = -1;
        var nextWordsIndices = new List<(string, int)>();

        // Left strip the text, to avoid inaccurate parsing
        if (nextWordStartIndex > 0)
        {
            censoredText = s[..nextWordStartIndex];
            s = s[nextWordStartIndex..];
        }

        // Splitting each word in the text to compare with censored words
        for (var i = 0; i < s.Length; i++)
        {
            if (i < skipIndex)
                continue;

            char? c = s[i];
            if (Utils.AllowedCharacters.Contains(c.ToString()))
            {
                curWord += c;
                continue;
            }

            // Skip continuous non-allowed characters
            if (curWord.Trim() == "")
            {
                censoredText += c;
                curWord = "";
                continue;
            }

            // Iterate the next words combined with the current one
            // to check if it forms a swear word
            nextWordsIndices = UpdateNextWordsIndices(s, nextWordsIndices, i);
            var (containsSwearWord, endIndex) = Utils.AnyNextWordsFormSwearWord(
                curWord, s, nextWordsIndices, CensorWordSet
            );

            if (containsSwearWord)
            {
                curWord = GetReplacementForSwearWord(censorChar, curWord.Length);
                skipIndex = endIndex;
                c = null;
                nextWordsIndices.Clear();
            }

            // If the current word is a swear word
            if (CensorWordSet.Contains(curWord.ToLowerInvariant()))
            {
                curWord = GetReplacementForSwearWord(censorChar, curWord.Length);
            }

            censoredText += curWord;

            if (c.HasValue)
            {
                censoredText += c;
            }

            curWord = "";
        }

        // Final check
        if (curWord != "" && skipIndex < s.Length - 1)
        {
            if (CensorWordSet.Contains(curWord.ToLowerInvariant()))
            {
                curWord = GetReplacementForSwearWord(censorChar, curWord.Length);
            }

            censoredText += curWord;
        }

        return censoredText;
    }

    /// <summary>
    /// Replace the swear words in the text with `censorChar`.
    /// </summary>
    /// <param name="s">The input text</param>
    /// <param name="censorChar">The character to censor text with</param>
    /// <returns></returns>
    public string Censor(string s, char censorChar = '*')
    {
        if (CensorWordSet == null)
        {
            LoadCensorWords();
        }

        return HideSwearWords(s, censorChar);
    }
}



public static class Utils
{
    public static readonly HashSet<string> AllowedCharacters = [];

    /// <summary>
    /// Add ascii_letters, digits, and @$*"' to the allowed character list (AllowedCharacters).
    /// Then, load the unicode characters from categories Ll, Lu, Mc, Mn into AllowedCharacters.
    /// 
    /// More about Unicode categories can be found at
    /// https://en.wikipedia.org/wiki/Template:General_Category_(Unicode)
    /// </summary>
    /// <param name="dataDir">Optional directory to search for alphabetic_unicode.json in</param>
    public static void LoadUnicodeSymbols(string dataDir = null)
    {
        // We only want this to run once, ever.
        if (AllowedCharacters.Count > 0)
        {
            return;
        }

        // ascii_letters, digits, @$*"'
        foreach (char c in "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@$*\"'")
        {
            AllowedCharacters.Add(c.ToString());
        }

        // alphabetic_unicode.json
        var path = "alphabetic_unicode.json";
        if (dataDir != null)
        {
            path = Path.Combine(dataDir + "/", path);
        }
        var unicodeData = File.ReadAllText(path);
        var unicodeStrings = JsonSerializer.Deserialize<List<string>>(unicodeData);
        foreach (var s in unicodeStrings)
        {
            AllowedCharacters.Add(s);
        }
    }

    /// <summary>
    /// Return the index of the first character of the next word in the given text.
    /// </summary>
    /// <param name="s">The text to work with</param>
    /// <param name="startIndex">The index to start searching at</param>
    /// <returns></returns>
    public static int GetStartIndexOfNextWord(string s, int startIndex)
    {
        var result = s.Length;

        for (var i = startIndex; i < s.Length; i++)
        {
            if (!AllowedCharacters.Contains(s[i].ToString()))
            {
                continue;
            }

            result = i;
            break;
        }

        return result;
    }

    /// <summary>
    /// Return the next word in the given text, and the index of its last character.
    /// </summary>
    /// <param name="s">The text to work with</param>
    /// <param name="startIndex"></param>
    /// <returns>The index to start searching at</returns>
    private static (string, int) GetNextWordAndEndIndex(string s, int startIndex)
    {
        var nextWord = "";

        var i = startIndex;
        for (; i < s.Length; i++)
        {
            var c = s[i];
            if (AllowedCharacters.Contains(c.ToString()))
            {
                nextWord += c;
                continue;
            }

            break;
        }

        return (nextWord, i);
    }

    /// <summary>
    /// Return true and the end index of the word in the text, if any word formed in wordsIndices is in `censorWordSet`.
    /// </summary>
    /// <param name="curWord"></param>
    /// <param name="text"></param>
    /// <param name="wordsIndices"></param>
    /// <param name="censorWordSet"></param>
    /// <returns></returns>
    public static (bool, int) AnyNextWordsFormSwearWord(string curWord, string text, List<(string, int)> wordsIndices,
        HashSet<string> censorWordSet)
    {
        var fullWord = curWord.ToLowerInvariant();
        var fullWordWithSeparators = curWord.ToLowerInvariant();

        // Check both words in the pairs
        for (var i = 0; i < wordsIndices.Count; i += 2)
        {
            var (singleWord, endIndex) = wordsIndices[i];

            if (singleWord == "")
            {
                continue;
            }

            var (wordWithSeparators, _) = wordsIndices[i + 1];

            fullWord = fullWord + singleWord.ToLowerInvariant();
            fullWordWithSeparators = fullWordWithSeparators + wordWithSeparators.ToLowerInvariant();

            if (censorWordSet.Contains(fullWord) || censorWordSet.Contains(fullWordWithSeparators))
            {
                return (true, endIndex);
            }
        }

        return (false, -1);
    }

    /// <summary>
    /// Return a list of pairs of next words and next words included with separators, combined with their end indices.
    /// For example: Word `hand_job` has next words pairs: `job`, `_job`.
    /// </summary>
    /// <param name="s">Input text</param>
    /// <param name="startIndex">Index to start getting words at</param>
    /// <param name="numOfNextWords">The number of next words to get</param>
    /// <returns>A list of pairs of next words and next words included with separators</returns>
    public static List<(string, int)> GetNextWords(string s, int startIndex, int numOfNextWords = 1)
    {
        // Find the starting index of the next word
        var nextWordStartIndex = GetStartIndexOfNextWord(s, startIndex);

        // Return an empty string if there are no other words
        if (nextWordStartIndex >= s.Length - 1)
        {
            return
            [
                ("", nextWordStartIndex),
                ("", nextWordStartIndex)
            ];
        }

        // Combine the words into a list
        var (nextWord, endIndex) = GetNextWordAndEndIndex(s, nextWordStartIndex);

        var words = new List<(string, int)>()
            {
                (nextWord, endIndex),
                (s[startIndex .. nextWordStartIndex] + nextWord, endIndex)
            };

        if (numOfNextWords > 1)
        {
            words.AddRange(GetNextWords(s, endIndex, numOfNextWords - 1));
        }

        return words;
    }

    /// <summary>
    /// Gets the Cartesian product of an IEnumerable 
    /// </summary>
    /// <param name="sequences">The sequences to use to get the Cartesian product</param>
    /// <typeparam name="T">The inner type of the IEnumerable</typeparam>
    /// <returns>Cartesian product</returns>
    // https://stackoverflow.com/a/3098381
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
        IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>()};
        return sequences.Aggregate(
            emptyProduct,
            (accumulator, sequence) =>
                from accseq in accumulator
                from item in sequence
                select accseq.Concat(new[] { item })
        );
    }
}
