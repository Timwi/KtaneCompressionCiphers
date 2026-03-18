using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompressionCiphersLib;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class ArithmeticCipherModule : CipherModuleBase
{
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    protected override string loggingTag => $"Arithmetic Cipher #{_moduleId}";

    const int ArithmeticNumBits = 20;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        _answer = new Data().ChooseWord(4, 6);
        tryAgain:
        var freq = Enumerable.Range(0, 27).Select(_ => Rnd.Range(1, 27)).ToArray();
        var encoded = Encode(_answer, freq);
        if (encoded.Length > 9)
            goto tryAgain;

        _pages[0][0] = encoded;
        var freqStr = freq.Select(i => (char) ('A' + i - 1)).JoinString();
        for (var i = 0; i < 5; i++)
            _pages[(i + 1) / 3][(i + 1) % 3] = i == 4 ? $"{freqStr.Substring(6 * i)} {freq.Sum()}" : freqStr.Substring(6 * i, 6);

        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Encoded word: {encoded}");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Letters on module: {freqStr}");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Solution: {_answer}");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] ─────────────────────────────────────────────────");

        var decoded = Decode(encoded, freq);
        if (decoded != _answer)
            Debug.LogError($"[Arithmetic Cipher #{_moduleId}] Reconstruction does not match");
    }

    private static readonly Dictionary<char, string> dic = new Dictionary<char, string> { ['A'] = "00000", ['B'] = "00001", ['C'] = "00010", ['D'] = "00011", ['E'] = "00100", ['F'] = "00101", ['G'] = "00110", ['H'] = "00111", ['I'] = "01000", ['J'] = "01001", ['K'] = "01010", ['L'] = "01011", ['M'] = "01100", ['N'] = "01101", ['O'] = "01110", ['P'] = "01111", ['Q'] = "10000", ['R'] = "10001", ['S'] = "10010", ['T'] = "10011", ['U'] = "1010", ['V'] = "1011", ['W'] = "1100", ['X'] = "1101", ['Y'] = "1110", ['Z'] = "1111" };

    private static string Encode(string word, int[] freq)
    {
        var total = freq.Sum();
        var outputBits = "";
        var high = ~(-1 << ArithmeticNumBits);
        var low = 0;
        var underflow = 0;
        foreach (var symbol in word.Select(ltr => ltr - 'A').Concat(new[] { 26 }))
        {
            var symbolPos = freq.Take(symbol).Sum();
            var symbolFreq = freq[symbol];

            // Set high and low to the new values
            var newlow = checked((high - low + 1) * symbolPos / total + low);
            high = checked((high - low + 1) * (symbolPos + symbolFreq) / total + low - 1);
            low = newlow;

            // While most significant bits match, shift them out and output them
            while ((high & (1 << (ArithmeticNumBits - 1))) == (low & (1 << (ArithmeticNumBits - 1))))
            {
                outputBits += (high & (1 << (ArithmeticNumBits - 1))) != 0 ? "1" : "0";

                while (underflow > 0)
                {
                    outputBits += (high & (1 << (ArithmeticNumBits - 1))) == 0 ? "1" : "0";
                    underflow--;
                }
                high = ((high << 1) & ~(-1 << ArithmeticNumBits)) | 1;
                low = (low << 1) & ~(-1 << ArithmeticNumBits);
            }

            // If underflow is imminent, shift it out
            while (((low & (1 << (ArithmeticNumBits - 2))) != 0) && ((high & (1 << (ArithmeticNumBits - 2))) == 0))
            {
                underflow++;
                high = ((high & ~(-1 << (ArithmeticNumBits - 1))) << 1) | ((1 << (ArithmeticNumBits - 1)) | 1);
                low = (low << 1) & ~(-1 << (ArithmeticNumBits - 1));
            }
        }

        outputBits += (low & (1 << (ArithmeticNumBits - 2))) != 0 ? "1" : "0";
        underflow++;
        while (underflow > 0)
        {
            outputBits += (low & (1 << (ArithmeticNumBits - 2))) == 0 ? "1" : "0";
            underflow--;
        }
        while (low > 0)
        {
            outputBits += (low & (1 << (ArithmeticNumBits - 1))) == 0 ? "1" : "0";
            low = (low << 1) & ~(-1 << (ArithmeticNumBits - 1));
        }

        outputBits = outputBits.TrimEnd('1');

        var output = "";
        while (true)
        {
            foreach (var kvp in dic)
                if (outputBits.StartsWith(kvp.Value))
                {
                    output += kvp.Key;
                    outputBits = outputBits.Substring(kvp.Value.Length);
                    goto next;
                }
            break;
            next:;
        }
        if (outputBits.Length > 0)
        {
            while (!dic.Any(kvp => kvp.Value == outputBits))
                outputBits += "1";
            output += dic.First(kvp => kvp.Value == outputBits).Key;
        }

        return output;
    }

    private static string binary(int value)
    {
        var sb = new StringBuilder();
        for (var i = 19; i >= 0; i--)
            sb.Append((value & (1 << i)) != 0 ? '1' : '0');
        return sb.ToString();
    }

    private string Decode(string encoded, int[] freq)
    {
        var inputBits = encoded.Select(c => dic[c]).JoinString();
        var inputIx = 0;
        var high = ~(-1 << ArithmeticNumBits);
        var low = 0;
        var code = 0;
        var total = freq.Sum();

        int readBit() => inputIx >= inputBits.Length || inputBits[inputIx++] == '1' ? 1 : 0;
        for (var i = 0; i < ArithmeticNumBits; i++)
            code = (code << 1) | readBit();

        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Binary stream: {inputBits}");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Frequency table:");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] {freq.JoinString(",")},");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] {Enumerable.Range(0, 28).Select(n => freq.Take(n).Sum()).JoinString(",")}");

        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Start values:");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] High: {binary(high)}");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Low: {binary(low)}");
        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Code: {binary(code)}");

        var result = new StringBuilder();
        while (true)
        {
            // Find out what the next symbol is from the contents of ‘code’
            var pos = checked(((code - low + 1) * total - 1) / (high - low + 1));

            var symbol = 0;
            var symbolPos = 0;
            while (symbol < freq.Length && symbolPos + freq[symbol] <= pos)
            {
                symbolPos += freq[symbol];
                symbol++;
            }

            Debug.Log($"[Arithmetic Cipher #{_moduleId}] Obtained value: {pos} ⇒ {(symbol == 26 ? "EOF" : ((char) ('A' + symbol)).ToString())}");

            if (symbol == 26)
                break;
            result.Append((char) ('A' + symbol));

            // Set high and low to the new values
            var newlow = (high - low + 1) * symbolPos / total + low;
            high = (high - low + 1) * (symbolPos + freq[symbol]) / total + low - 1;
            low = newlow;

            Debug.Log($"[Arithmetic Cipher #{_moduleId}] After formulas:");
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] High: {binary(high)}");
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] Low: {binary(low)}");
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] Code: {binary(code)}");

            // While most significant bits match, shift them out
            var shifts1 = 0;
            while ((high & (1 << (ArithmeticNumBits - 1))) == (low & (1 << (ArithmeticNumBits - 1))))
            {
                high = ((high << 1) & ~(-1 << ArithmeticNumBits)) | 1;
                low = (low << 1) & ~(-1 << ArithmeticNumBits);
                code = ((code << 1) & ~(-1 << ArithmeticNumBits)) | readBit();
                shifts1++;
            }

            // If underflow is imminent, shift it out
            var shifts2 = 0;
            while (((low & (1 << (ArithmeticNumBits - 2))) != 0) && ((high & (1 << (ArithmeticNumBits - 2))) == 0))
            {
                high = ((high & ~(-1 << (ArithmeticNumBits - 1))) << 1) | ((1 << (ArithmeticNumBits - 1)) | 1);
                low = (low << 1) & ~(-1 << (ArithmeticNumBits - 1));
                code = (((code & ~(-1 << (ArithmeticNumBits - 1))) ^ (1 << (ArithmeticNumBits - 2))) << 1) | readBit();
                shifts2++;
            }
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] Shifts: {shifts1},{shifts2}");

            Debug.Log($"[Arithmetic Cipher #{_moduleId}] After column removals:");
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] High: {binary(high)}");
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] Low: {binary(low)}");
            Debug.Log($"[Arithmetic Cipher #{_moduleId}] Code: {binary(code)}");
        }

        Debug.Log($"[Arithmetic Cipher #{_moduleId}] Done! Decoded word: {result}");
        return result.ToString();
    }
}