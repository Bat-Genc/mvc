using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SchoolMvc
{
    public sealed class CustomCipher
    {
        private static readonly Dictionary<char, string> CipherMap = new()
        {
            {'a', "!ab"}, {'b', "!bc"}, {'c', "!cd"}, {'d', "!de"}, {'e', "!ef"},
            {'f', "!fg"}, {'g', "@gh"}, {'h', "@hi"}, {'i', "@ij"}, {'j', "@jk"},
            {'k', "#kl"}, {'l', "#lm"}, {'m', "#mn"}, {'n', "#no"}, {'o', "#op"},
            {'p', "$pq"}, {'q', "$qr"}, {'r', "$rs"}, {'s', "$st"}, {'t', "$tu"},
            {'u', "%uv"}, {'v', "%vw"}, {'w', "%wx"}, {'x', "%xy"}, {'y', "%yz"},
            {'z', "^za"}
        };

        private static readonly Dictionary<string, char> ReverseCipherMap = CipherMap
            .ToDictionary(kv => kv.Value, kv => kv.Key, StringComparer.Ordinal);

    // 🔒 Encrypt
    public string Encrypt(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var result = new StringBuilder(input.Length * 3);

        foreach (char c in input.ToLowerInvariant())
        {
            if (CipherMap.TryGetValue(c, out var code))
                result.Append(code);
            else
                result.Append(c);
        }

        return result.ToString();
    }

    // 🔓 Decrypt
    public string Decrypt(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var result = new StringBuilder(input.Length);
        int i = 0;

        while (i < input.Length)
        {
            bool found = false;
            if (i + 3 <= input.Length)
            {
                var token = input.Substring(i, 3);
                if (ReverseCipherMap.TryGetValue(token, out var decoded))
                {
                    result.Append(decoded);
                    i += 3;
                    found = true;
                }
            }

            if (!found)
            {
                result.Append(input[i]);
                i++;
            }
        }

        return result.ToString();
    }
}
}