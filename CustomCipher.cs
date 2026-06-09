using System.Security.Cryptography;
using System.Text;

namespace SchoolMvc;

public class CustomCipher
{
    private static readonly char[] Alphabet =
    {
        'а','б','в','г','д','е','ж','з','и','й',
        'к','л','м','н','о','п','р','с','т','у',
        'ф','х','ц','ч','ш','щ','ъ','ь','ю','я'
    };

    private static readonly HashSet<char> Preserved = new()
    {
        '.', ',', '?', '!', ':', ';', '"', '\'', '(', ')', '\n', '-'
    };

    private static readonly char[] Separators =
    {
        '@','#','$','%','^','&','*','~','|','/','\\','+','=','<','>',
        '§','°','†','‡','€','£','¥','¤','¦','±','×','÷','¬','µ','¶',
        '¿','¡','™','©','®','[',']','{','}','_'
    };

    private static readonly HashSet<char> SeparatorSet = new(Separators);

    private static readonly char[] Noise =
    {
        'ا','ب','ت','ث','ج','ح','خ','د','ذ','ر','ز','س','ش','ص','ض','ط','ظ','ع','غ','ف',
        '的','一','是','在','不','了','有','和','人','这','中','大','为','上','个','国','我','以','要','他',
        'あ','い','う','え','お','か','き','く','け','こ','さ','し','す','せ','そ','た','ち','つ','て','と',
        '가','나','다','라','마','바','사','아','자','차','카','타','파','하','갈','남','대','람','봐','삶',
        'ก','ข','ค','ง','จ','ฉ','ช','ซ','ญ','ด','ต','ถ','ท','น','บ','ป','ผ','ฝ','พ','ฟ',
        'մ','ա','ս','ն','ի','կ','հ','ե','տ','ր','բ','գ','դ','զ','թ','լ','ծ','պ','ռ','վ',
        'ა','ბ','გ','დ','ე','ვ','ზ','თ','ი','კ','ლ','მ','ნ','ო','პ','ჟ','რ','ს','ტ','უ'
    };

    private static readonly HashSet<char> NoiseSet = new(Noise);

    public static string GenerateCustomPasswordForUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return string.Empty;
        }

        var normalized = username.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes("CipherX-User:" + normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ВРЕМЕВА ЗАВИСИМОСТ - дневни seed-ове
    // ═══════════════════════════════════════════════════════════════════════════
    public static int GetDailySeedOffset(DateTime date)
    {
        // Променя се ВСЕКИ ДЕН - зависи от датата
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    private static string[][] GenerateNotebooksForDate(DateTime date)
    {
        var dailyOffset = GetDailySeedOffset(date);
        int[] baseSeeds = { 101,202,303,404,505,606,707,808,909,1010,
                            1111,1212,1313,1414,1515,1616,1717,1818,1919,2020,
                            2121,2222,2323,2424,2525,2626,2727,2828,2929,3030 };

        var notebooks = new string[30][];
        for (int n = 0; n < 30; n++)
        {
            var dailySeed = baseSeeds[n] + dailyOffset;
            var rng = new Random(dailySeed);
            int groupLen = (n % 4) + 2;
            notebooks[n] = new string[30];
            var used = new HashSet<string>();
            for (int i = 0; i < 30; i++)
            {
                string group;
                int attempts = 0;
                do
                {
                    var chars = new char[groupLen];
                    for (int k = 0; k < groupLen; k++)
                        chars[k] = Alphabet[rng.Next(30)];
                    group = new string(chars);
                    attempts++;
                    if (attempts > 50000) break;
                } while (used.Contains(group));
                used.Add(group);
                notebooks[n][i] = group;
            }
        }
        return notebooks;
    }

    private static char[][] GenerateSimpleMapForDate(DateTime date)
    {
        var dailyOffset = GetDailySeedOffset(date);
        int[] baseSeeds = { 42,84,126,168,210,252,294,336,378,420,
                            462,504,546,588,630,672,714,756,798,840,
                            882,924,966,1008,1050,1092,1134,1176,1218,1260 };
        
        var map = new char[30][];
        for (int n = 0; n < 30; n++)
        {
            var dailySeed = baseSeeds[n] + dailyOffset;
            var perm = (char[])Alphabet.Clone();
            var rng = new Random(dailySeed);
            for (int i = perm.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (perm[i], perm[j]) = (perm[j], perm[i]);
            }
            map[n] = perm;
        }
        return map;
    }

    private static char[][] GenerateSimpleMapReverseForDate(DateTime date)
    {
        var map = GenerateSimpleMapForDate(date);
        var reverseMap = new char[30][];
        for (int n = 0; n < 30; n++)
        {
            reverseMap[n] = new char[30];
            for (int i = 0; i < 30; i++)
                reverseMap[n][Array.IndexOf(Alphabet, map[n][i])] = Alphabet[i];
        }
        return reverseMap;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ОСНОВЕН МЕТОД ЗА КРИПТИРАНЕ С ДАТА
    // ═══════════════════════════════════════════════════════════════════════════
    public string EncryptWithDate(string text, string password, DateTime date)
{
    Console.WriteLine($"=== EncryptWithDate ===");
    Console.WriteLine($"text: {text}");
    Console.WriteLine($"password: {password}");
    Console.WriteLine($"date: {date:yyyy-MM-dd}");
    
    var notebooks = GenerateNotebooksForDate(date);
    var simpleMap = GenerateSimpleMapForDate(date);
    var simpleMapReverse = GenerateSimpleMapReverseForDate(date);
    
    Console.WriteLine($"Notebooks[0][0]: '{notebooks[0][0]}'");
    Console.WriteLine($"SimpleMap[0][0]: '{simpleMap[0][0]}'");
    
    if (string.IsNullOrEmpty(text)) return text;
    var sequence = BuildSequence(password);
    Console.WriteLine($"Sequence: [{string.Join(", ", sequence)}]");
    
    if (sequence.Count == 0) return text;
    
    string current = text;
    for (int s = 0; s < sequence.Count; s++)
    {
        Console.WriteLine($"Step {s}, current before: '{current}'");
        current = s == sequence.Count - 1 
            ? ApplyGroupEncryptWithData(current, sequence[s], notebooks) 
            : ApplyCharEncryptWithData(current, sequence[s], simpleMap);
        Console.WriteLine($"Step {s}, current after: '{current}'");
    }
    Console.WriteLine($"=== EncryptWithDate RESULT: '{current}'");
    return current;
}

    // ═══════════════════════════════════════════════════════════════════════════
    // ОСНОВЕН МЕТОД ЗА ДЕКРИПТИРАНЕ С ДАТА
    // ═══════════════════════════════════════════════════════════════════════════
    public string DecryptWithDate(string text, string password, DateTime date)
    {
        var notebooks = GenerateNotebooksForDate(date);
        var simpleMapReverse = GenerateSimpleMapReverseForDate(date);
        
        if (string.IsNullOrEmpty(text)) return text;
        var sequence = BuildSequence(password);
        if (sequence.Count == 0) return text;
        sequence.Reverse();
        
        string current = text;
        for (int s = 0; s < sequence.Count; s++)
        {
            current = s == 0 
                ? ApplyGroupDecryptWithData(current, sequence[s], notebooks) 
                : ApplyCharDecryptWithData(current, sequence[s], simpleMapReverse);
        }
        return current;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ПОМОЩНИ МЕТОДИ
    // ═══════════════════════════════════════════════════════════════════════════
    private static bool IsPasswordSpecial(char c) => !char.IsLetter(c) && !char.IsDigit(c);
    private static int AlphabetIndex(char c) => Array.IndexOf(Alphabet, char.ToLower(c));

    private static string TransformDigits(string d)
    {
        int len = d.Length;
        if (len <= 1) return d;
        if (len % 3 == 0) { int p = len / 3; return d[p..] + d[..p]; }
        int half = len / 2;
        return d[half..] + d[..half];
    }

    private static string ReverseTransformDigits(string d)
    {
        int len = d.Length;
        if (len <= 1) return d;
        if (len % 3 == 0) { int p = len / 3; return d[^p..] + d[..^p]; }
        int half = len / 2;
        int second = len - half;
        return d[second..] + d[..second];
    }

    private static int ComputeInitialOffset(string password)
    {
        var bits = new System.Text.StringBuilder();
        foreach (char c in password)
            bits.Append(IsPasswordSpecial(c) ? '1' : '0');
        string bitStr = bits.ToString().TrimStart('0');
        if (bitStr.Length == 0) return 0;
        if (bitStr.Length > 30) bitStr = bitStr[^30..];
        return Convert.ToInt32(bitStr, 2) % 30;
    }

    private static List<int> BuildSequence(string password)
    {
        int offset = ComputeInitialOffset(password);
        var sequence = new List<int>();
        foreach (char c in password)
        {
            if (IsPasswordSpecial(c)) continue;
            if (char.IsDigit(c)) { offset = (offset + (c - '0')) % 30; continue; }
            int baseIdx = AlphabetIndex(c);
            if (baseIdx < 0)
            {
                // Non-Cyrillic username/password characters must still affect encryption.
                sequence.Add((c % 30 + offset) % 30);
                continue;
            }
            sequence.Add((baseIdx + offset) % 30);
        }
        return sequence;
    }

    private static string ApplyCharEncryptWithData(string text, int nbIdx, char[][] simpleMap)
    {
        var map = simpleMap[nbIdx];
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            bool isUpper = char.IsUpper(c);
            int idx = AlphabetIndex(c);
            if (idx < 0) { sb.Append(c); continue; }
            sb.Append(isUpper ? char.ToUpper(map[idx]) : map[idx]);
        }
        return sb.ToString();
    }

    private static string ApplyCharDecryptWithData(string text, int nbIdx, char[][] simpleMapReverse)
    {
        var map = simpleMapReverse[nbIdx];
        var sb = new System.Text.StringBuilder(text.Length);
        foreach (char c in text)
        {
            bool isUpper = char.IsUpper(c);
            int idx = AlphabetIndex(c);
            if (idx < 0) { sb.Append(c); continue; }
            sb.Append(isUpper ? char.ToUpper(map[idx]) : map[idx]);
        }
        return sb.ToString();
    }

    private static string ApplyGroupEncryptWithData(string text, int nbIdx, string[][] notebooks)
    {
        var notebook = notebooks[nbIdx];
        var sb = new System.Text.StringBuilder();
        var rng = new Random(nbIdx * 1337);
        var digitBuf = new System.Text.StringBuilder();
        int charPos = 0;
        void FlushDigits() { if (digitBuf.Length == 0) return; sb.Append(TransformDigits(digitBuf.ToString())); digitBuf.Clear(); }
        foreach (char c in text)
        {
            if (char.IsDigit(c)) { digitBuf.Append(c); continue; }
            FlushDigits();
            if (Preserved.Contains(c)) { sb.Append(c); continue; }
            if (c == ' ')
            {
                int count = rng.Next(2, 6);
                for (int i = 0; i < count; i++) sb.Append(Separators[rng.Next(Separators.Length)]);
                continue;
            }
            bool isUpper = char.IsUpper(c);
            int idx = AlphabetIndex(c);
            if (idx < 0) { sb.Append(c); continue; }
            int rotatedIdx = (idx + charPos) % 30;
            charPos++;
            string group = notebook[rotatedIdx];
            sb.Append(isUpper ? char.ToUpper(group[0]) : group[0]);
            for (int k = 1; k < group.Length; k++) sb.Append(group[k]);
            sb.Append(Separators[rng.Next(Separators.Length)]);
            int noiseCount = rng.Next(1, 3);
            for (int n = 0; n < noiseCount; n++) sb.Append(Noise[rng.Next(Noise.Length)]);
        }
        FlushDigits();
        return sb.ToString();
    }

    private static string ApplyGroupDecryptWithData(string text, int nbIdx, string[][] notebooks)
    {
        var notebook = notebooks[nbIdx];
        int groupLen = (nbIdx % 4) + 2;
        var sb = new System.Text.StringBuilder();
        int i = 0;
        int charPos = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (Preserved.Contains(c)) { sb.Append(c); i++; continue; }
            if (NoiseSet.Contains(c)) { i++; continue; }
            if (SeparatorSet.Contains(c))
            {
                int count = 0;
                while (i < text.Length && SeparatorSet.Contains(text[i])) { count++; i++; }
                if (count >= 2) sb.Append(' ');
                continue;
            }
            if (char.IsDigit(c))
            {
                var digits = new System.Text.StringBuilder();
                while (i < text.Length && char.IsDigit(text[i])) digits.Append(text[i++]);
                sb.Append(ReverseTransformDigits(digits.ToString()));
                continue;
            }
            if (AlphabetIndex(c) >= 0)
            {
                bool isUpper = char.IsUpper(c);
                var groupChars = new System.Text.StringBuilder();
                int read = 0;
                while (i < text.Length && read < groupLen)
                {
                    char gc = text[i];
                    if (!SeparatorSet.Contains(gc) && !Preserved.Contains(gc) && !char.IsDigit(gc) && !NoiseSet.Contains(gc))
                    {
                        groupChars.Append(char.ToLower(gc));
                        read++;
                    }
                    i++;
                }
                string groupStr = groupChars.ToString();
                int found = -1;
                for (int k = 0; k < 30; k++) if (notebook[k] == groupStr) { found = k; break; }
                if (found >= 0)
                {
                    int idx = ((found - charPos) % 30 + 30) % 30;
                    charPos++;
                    sb.Append(isUpper ? char.ToUpper(Alphabet[idx]) : Alphabet[idx]);
                }
                continue;
            }
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    // ЛИЧНИ МЕТОДИ С PERSONAL SEED
    // ═══════════════════════════════════════════════════════════════════════════
    public string EncryptWithPersonalSeed(string text, string password, int personalSeed)
    {
        string modifiedPassword = password + personalSeed.ToString();
        return EncryptWithDate(text, modifiedPassword, DateTime.UtcNow.AddHours(2));
    }

    public string DecryptWithPersonalSeed(string text, string password, int personalSeed)
    {
        string modifiedPassword = password + personalSeed.ToString();
        return DecryptWithDate(text, modifiedPassword, DateTime.UtcNow.AddHours(2));
    }
}