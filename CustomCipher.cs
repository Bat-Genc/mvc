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

    // ✅ ПЪРВИТЕ 16 NOISE СИМВОЛА - ЗА ДАННИ (0-15)
    private static readonly char[] DataNoise = 
    {
        'ا', 'ب', 'ت', 'ث', 'ج', 'ح', 'خ', 'د',  // 0-7
        'ذ', 'ر', 'ز', 'س', 'ش', 'ص', 'ض', 'ط'   // 8-15
    };

    // ✅ ВСИЧКИ NOISE СИМВОЛИ
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

    private const int EVERYONE_CODE = -1;
    
    // ✅ МАРКЕР ОТ СЕПАРАТОРИТЕ (НЕ Е ЧАСТ ОТ ШУМА)
    private const char METADATA_MARKER = '§';

    public static string GenerateCustomPasswordForUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return string.Empty;

        var normalized = username.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes("CipherX-User:" + normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    private static int ComputeAdvancedSeed(DateTime date, string senderUsername, string receiverUsername)
    {
        var sec = date.Second;
        var min = date.Minute;
        var hour = date.Hour;

        var senderBinary = Math.Abs(GetUsernameBinary(senderUsername));
        var receiverBinary = Math.Abs(GetUsernameBinary(receiverUsername));

        var combined = (sec * min * hour) + senderBinary + receiverBinary;
        return Math.Abs(combined) % 30;
    }

    private static int ComputeNotebookSeed(DateTime date, string senderUsername, string receiverUsername)
    {
        var timeSeed = date.Hour * 3600 + date.Minute * 60 + date.Second;
        var senderBinary = Math.Abs(GetUsernameBinary(senderUsername));
        var receiverBinary = Math.Abs(GetUsernameBinary(receiverUsername));
        return Math.Abs(timeSeed + senderBinary + receiverBinary);
    }

    private static int GetUsernameBinary(string username)
    {
        if (string.IsNullOrEmpty(username)) return 0;
        if (username == "everyone") return EVERYONE_CODE;

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(username);
        var hash = sha256.ComputeHash(bytes);
        return Math.Abs(BitConverter.ToInt32(hash, 0) % 1000000);
    }

    public static int GetDailySeedOffset(DateTime date)
    {
        return date.Year * 10000 + date.Month * 100 + date.Day;
    }

    private static string[][] GenerateNotebooksForDate(DateTime date, int notebookSeed)
    {
        var dailyOffset = GetDailySeedOffset(date);
        var combinedOffset = dailyOffset + notebookSeed;

        int[] baseSeeds = { 101,202,303,404,505,606,707,808,909,1010,
                            1111,1212,1313,1414,1515,1616,1717,1818,1919,2020,
                            2121,2222,2323,2424,2525,2626,2727,2828,2929,3030 };

        var notebooks = new string[30][];
        for (int n = 0; n < 30; n++)
        {
            var dailySeed = baseSeeds[n] + combinedOffset;
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

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        
        var result = new StringBuilder();
        foreach (char c in text)
        {
            if ((c >= 32 && c <= 126) || 
                (c >= 0x0400 && c <= 0x04FF) || 
                c == '\n' || c == '\r' || c == '\t' || c == ' ')
            {
                result.Append(c);
            }
        }
        return result.ToString();
    }

    // ============================================================
    // ✅ КОДИРАНЕ/ДЕКОДИРАНЕ НА МЕТАДАННИ В DATA NOISE
    // ============================================================

    private string EncodeMetadataToDataNoise(string metadata)
    {
        var result = new StringBuilder();
        foreach (char c in metadata)
        {
            int value = (int)c;
            int high = (value >> 4) & 0x0F;
            int low = value & 0x0F;
            result.Append(DataNoise[high]);
            result.Append(DataNoise[low]);
        }
        return result.ToString();
    }

    private string DecodeDataNoiseToMetadata(string dataNoise)
    {
        var bytes = new List<byte>();
        for (int i = 0; i < dataNoise.Length - 1; i += 2)
        {
            int high = Array.IndexOf(DataNoise, dataNoise[i]);
            int low = Array.IndexOf(DataNoise, dataNoise[i + 1]);
            
            if (high < 0 || low < 0) break;
            
            byte value = (byte)((high << 4) | low);
            bytes.Add(value);
        }
        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    private string GeneratePadding()
    {
        var rng = new Random();
        var result = new StringBuilder();
        int paddingLength = rng.Next(10, 30);
        for (int i = 0; i < paddingLength; i++)
        {
            result.Append(Noise[rng.Next(Noise.Length)]);
        }
        return result.ToString();
    }

    // ============================================================
    // ✅ ОСНОВНИ МЕТОДИ - МЕТАДАННИ В DATA NOISE С МАРКЕР §
    // ============================================================

    public string EncryptWithMetadata(string text, string password, DateTime date, string senderUsername, string receiverUsername = "anonymous")
    {
        var advancedSeed = ComputeAdvancedSeed(date, senderUsername, receiverUsername);
        var notebookSeed = ComputeNotebookSeed(date, senderUsername, receiverUsername);
        var receiverCode = GetUsernameBinary(receiverUsername);
        
        var modifiedPassword = password + advancedSeed.ToString();
        var encrypted = EncryptWithDate(text, modifiedPassword, date, notebookSeed);

        // ✅ КОДИРАМЕ МЕТАДАННИТЕ
        var metadata = $"{receiverCode}|{advancedSeed}|{notebookSeed}";
        var encodedMetadata = EncodeMetadataToDataNoise(metadata);
        
        // ✅ ДОБАВЯМЕ МАРКЕР § + МЕТАДАННИ + МАРКЕР § + ЗАПЪЛВАНЕ
        var padding = GeneratePadding();
        
        return encrypted + METADATA_MARKER + encodedMetadata + METADATA_MARKER + padding;
    }

    public string DecryptWithMetadata(string encryptedText, string password, DateTime date, string expectedReceiver)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedText))
                throw new Exception("Няма текст за декриптиране!");

            // ✅ ПРОВЕРКА ЗА СТАР ФОРМАТ (С |||)
            if (encryptedText.Contains("|||"))
            {
                var oldParts = encryptedText.Split(new[] { "|||" }, StringSplitOptions.None);
                if (oldParts.Length >= 4)
                {
                    var realTextOld = oldParts[0];
                    var recCodeOld = int.Parse(oldParts[1]);
                    var seedValOld = int.Parse(oldParts[2]);
                    var notebookValOld = int.Parse(oldParts[3]);

                    var modPassOld = password + seedValOld.ToString();
                    var decryptedTextOld = DecryptWithDate(realTextOld, modPassOld, date, notebookValOld);

                    if (recCodeOld == EVERYONE_CODE)
                        return CleanText(decryptedTextOld);

                    var expCodeOld = GetUsernameBinary(expectedReceiver);
                    if (recCodeOld != expCodeOld)
                        return "🔒 Съобщението не е предназначено за вас!";

                    return CleanText(decryptedTextOld);
                }
            }

            // ✅ НОВ ФОРМАТ - ТЪРСИМ МАРКЕР §
            int markerIndex = encryptedText.IndexOf(METADATA_MARKER);
            
            if (markerIndex < 0)
                throw new Exception("Не мога да намеря метаданните!");

            // ✅ ИЗВЛИЧАМЕ МЕТАДАННИТЕ (между двата маркера)
            int startIndex = markerIndex + 1;
            int endIndex = encryptedText.IndexOf(METADATA_MARKER, startIndex);
            
            if (endIndex < 0)
                throw new Exception("Невалиден формат на метаданните!");

            var encodedMetadata = encryptedText[startIndex..endIndex];
            
            // ✅ ДЕКОДИРАМЕ
            var metadata = DecodeDataNoiseToMetadata(encodedMetadata);
            var metaParts = metadata.Split('|');
            if (metaParts.Length < 3)
                throw new Exception("Невалидни метаданни!");

            var recCode = int.Parse(metaParts[0]);
            var seedVal = int.Parse(metaParts[1]);
            var notebookVal = int.Parse(metaParts[2]);

            // ✅ ПРЕМАХВАМЕ МЕТАДАННИТЕ ОТ ТЕКСТА
            var realText = encryptedText[..markerIndex];

            var modPass = password + seedVal.ToString();
            var decryptedText = DecryptWithDate(realText, modPass, date, notebookVal);

            if (recCode == EVERYONE_CODE)
                return CleanText(decryptedText);

            var expCode = GetUsernameBinary(expectedReceiver);
            if (recCode != expCode)
                return "🔒 Съобщението не е предназначено за вас!";

            return CleanText(decryptedText);
        }
        catch (Exception ex)
        {
            return $"❌ Грешка: {ex.Message}";
        }
    }

    // ============================================================
    // ✅ ЛИЧНИ МЕТОДИ
    // ============================================================

    public string EncryptWithPersonalSeedAndMetadata(string text, string password, int personalSeed, string senderUsername, string receiverUsername = "anonymous")
    {
        var date = DateTime.UtcNow.AddHours(2);
        var combinedPassword = password + personalSeed.ToString();
        return EncryptWithMetadata(text, combinedPassword, date, senderUsername, receiverUsername);
    }

    public string DecryptWithPersonalSeedAndMetadata(string encryptedText, string password, int personalSeed, string expectedReceiver)
    {
        var date = DateTime.UtcNow.AddHours(2);
        var combinedPassword = password + personalSeed.ToString();
        return DecryptWithMetadata(encryptedText, combinedPassword, date, expectedReceiver);
    }

    // ============================================================
    // ✅ БАЗОВИ МЕТОДИ
    // ============================================================

    public string EncryptWithDate(string text, string password, DateTime date, int notebookSeed = 0)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        var notebooks = GenerateNotebooksForDate(date, notebookSeed);
        var simpleMap = GenerateSimpleMapForDate(date);

        var sequence = BuildSequence(password);
        if (sequence.Count == 0) return text;

        string current = text;
        for (int s = 0; s < sequence.Count; s++)
        {
            current = s == sequence.Count - 1
                ? ApplyGroupEncryptWithData(current, sequence[s], notebooks, date)
                : ApplyCharEncryptWithData(current, sequence[s], simpleMap);
        }
        return current;
    }

    public string DecryptWithDate(string text, string password, DateTime date, int notebookSeed = 0)
    {
        if (string.IsNullOrEmpty(text)) return text;
        
        var notebooks = GenerateNotebooksForDate(date, notebookSeed);
        var simpleMapReverse = GenerateSimpleMapReverseForDate(date);

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

    // ============================================================
    // ✅ ЛИЧНИ МЕТОДИ (ОБРАТНА СЪВМЕСТИМОСТ)
    // ============================================================

    public string EncryptWithPersonalSeed(string text, string password, int personalSeed)
    {
        string modifiedPassword = password + personalSeed.ToString();
        return EncryptWithDate(text, modifiedPassword, DateTime.UtcNow.AddHours(2), personalSeed);
    }

    public string DecryptWithPersonalSeed(string text, string password, int personalSeed)
    {
        string modifiedPassword = password + personalSeed.ToString();
        return DecryptWithDate(text, modifiedPassword, DateTime.UtcNow.AddHours(2), personalSeed);
    }

    // ============================================================
    // ✅ ПРИВАТНИ МЕТОДИ
    // ============================================================

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
        var bits = new StringBuilder();
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
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            int idx = AlphabetIndex(c);
            if (idx >= 0)
            {
                sb.Append(char.IsUpper(c) ? char.ToUpper(map[idx]) : map[idx]);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string ApplyCharDecryptWithData(string text, int nbIdx, char[][] simpleMapReverse)
    {
        var map = simpleMapReverse[nbIdx];
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            int idx = AlphabetIndex(c);
            if (idx >= 0)
            {
                sb.Append(char.IsUpper(c) ? char.ToUpper(map[idx]) : map[idx]);
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private static string ApplyGroupEncryptWithData(string text, int nbIdx, string[][] notebooks, DateTime date)
    {
        var notebook = notebooks[nbIdx];
        var sb = new StringBuilder();
        var rng = new Random(nbIdx * 1337 + date.Second + date.Minute * 60 + date.Hour * 3600);
        var digitBuf = new StringBuilder();
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
        var sb = new StringBuilder();
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
                var digits = new StringBuilder();
                while (i < text.Length && char.IsDigit(text[i])) digits.Append(text[i++]);
                sb.Append(ReverseTransformDigits(digits.ToString()));
                continue;
            }
            if (AlphabetIndex(c) >= 0)
            {
                bool isUpper = char.IsUpper(c);
                var groupChars = new StringBuilder();
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
}