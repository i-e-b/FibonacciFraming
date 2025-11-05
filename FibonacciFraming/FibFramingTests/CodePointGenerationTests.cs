using System.Text;
using FibonacciFraming;
using FibonacciFraming.Internal;
using NUnit.Framework;

namespace FibFramingTests;

/// <summary>
/// These tests generate and check the various bit patterns and look-up tables.
/// </summary>
[TestFixture]
public class CodePointGenerationTests
{
    [Test]
    [TestCase("header", "1010101010101011")] // header pattern
    [TestCase("footer", "0101010101010011")] // footer pattern
    public void frame_header_and_footer_generation(string name, string pattern)
    {
        var data = new MemoryStream();
        var dst  = new BitwiseStreamWrapper(data, 0);

        foreach (var c in pattern)
        {
            dst.WriteBit(c == '1' ? 1 : 0);
        }

        dst.Flush();
        dst.Rewind();

        var result = FibonacciEncoder.FibonacciDecodeOne(dst);
        Console.WriteLine($"{name} result = {result} (0x{result:X4})");
    }


    [Test(Description = "Ensure the frame header has the correct pattern")]
    public void fib_coding_frame_header()
    {
        var input = FibonacciEncoder.FrameHead;
        var data  = new MemoryStream();
        var src   = new BitwiseStreamWrapper(data, 0);

        FibonacciEncoder.FibonacciEncodeOne(input, src);

        src.Flush();
        src.Rewind();

        var encoded = src.ToBitString();
        Console.WriteLine("Encoded: "+encoded);

        Assert.That(encoded, Does.StartWith("1010101010101011"), "Make sure we have the 01 repeating pattern");

        src.Rewind();

        var result = FibonacciEncoder.FibonacciDecodeOne(src);

        Assert.That(result, Is.EqualTo(input));
    }

    [Test(Description = "Ensure the frame footer has the correct pattern")]
    public void fib_coding_frame_footer()
    {
        var input = FibonacciEncoder.FrameFoot;
        var data  = new MemoryStream();
        var src   = new BitwiseStreamWrapper(data, 0);

        FibonacciEncoder.FibonacciEncodeOne(input, src);

        src.Flush();
        src.Rewind();

        var encoded = src.ToBitString();
        Console.WriteLine("Encoded: "+encoded);

        Assert.That(encoded, Does.StartWith("0101010101010011"), "Make sure we have the 01 repeating pattern");

        src.Rewind();

        var result = FibonacciEncoder.FibonacciDecodeOne(src);

        Assert.That(result, Is.EqualTo(input));
    }


    [Test(Description = "This generates the main look-up tables for encode/decode")]
    public void fib_coding_mapping_tables()
    {
        var bits    = new int[256]; // bit patterns for byte values
        var lengths = new int[256]; // pattern lengths for bit patterns

        var priorityMap = BuildPriorityMap();

        const int searchRange = 500;

        var backMap      = new int[searchRange];
        var codePoint    = 0;
        var maxCodePoint = 0;
        for (var input = 0; input < searchRange; input++) // For each Fibonacci code in the search range
        {

            // Output the cod
            var data = new MemoryStream();
            var src  = new BitwiseStreamWrapper(data, 0);
            FibonacciEncoder.FibonacciEncodeOne(input, src);

            src.Flush();
            src.Rewind();

            var haveOne     = false;
            var runningZero = 0;
            var runningOne  = 0;
            var ok          = true;

            // Check if the code is acceptable for our constraints
            while (src.TryReadBit(out var b))
            {
                if (b == 1)
                {
                    haveOne = true;
                    runningOne++;
                    runningZero = 0;
                }
                else
                {
                    runningZero++;
                    runningOne = 0;
                }

                if (!haveOne && runningZero > 3) ok = false; // up to 3 leading zeros (so that with padding it's a run of 4 max)
                if (runningZero > 4) ok = false; // up to 4 zeros after a one.

                if (runningOne > 1) break;
            }

            // If acceptable, place in the mapping tables (smaller codes in higher priority slots)
            if (ok)
            {
                FibonacciEncoder.FibonacciEncodeInt(input, out var bitPattern, out var length);

                bits[priorityMap[codePoint]] = bitPattern;
                lengths[priorityMap[codePoint]] = length;

                backMap[input] = priorityMap[codePoint];
                maxCodePoint = input;

                codePoint++;
            }
            // If not acceptable, mark it as unused in the backmap table
            else
            {
                backMap[input] = -1;
            }


            // Check that the decoder reads the code correctly
            src.Rewind();
            var result = FibonacciEncoder.FibonacciDecodeOne(src);

            Assert.That(result, Is.EqualTo(input));

            if (codePoint >= 256) break;
        }

        Assert.That(codePoint, Is.EqualTo(256), "Must fill all byte value slots");

        WriteGeneratedCode(bits, lengths, maxCodePoint, backMap);
    }

    private static void WriteGeneratedCode(int[] bits, int[] lengths, int maxCodePoint, int[] backMap)
    {
        var bitsArray    = new StringBuilder();
        var lengthsArray = new StringBuilder();
        var backmapArray = new StringBuilder();
        var sb           = new StringBuilder();

        bitsArray.AppendLine("/// <summary>\n    /// Encoded bit patterns for bytes. These can have leading zeroes, so must be paired\n    /// with the matching <see cref=\"BitLengths\"/> entry.\n    /// </summary>\n    public static readonly int[] BitPatterns = [");
        lengthsArray.AppendLine("/// <summary>\n    /// Encoded bit patterns lengths for bytes. These pair with the <see cref=\"BitPatterns\"/> table.\n    /// </summary>\n    public static readonly int[] BitLengths = [");
        for (int i = 0; i < 256; i++)
        {
            var pattern = bits[i];
            var length  = lengths[i];
            sb.Clear();
            for (int j = length - 1; j >= 0; j--)
            {
                sb.Append((((pattern >> j) & 1) == 1) ? '1' : '0');
            }

            bitsArray.AppendLine($"    0x{pattern:X4}, // {i:000} -> {sb}");
            lengthsArray.AppendLine($"    0x{length:X2}, // {i:000} -> {sb}");
        }
        bitsArray.AppendLine("];");
        lengthsArray.AppendLine("];");

        backmapArray.AppendLine("/// <summary>\n    /// Mapping from raw Fibonacci encoded value to the appropriate <see cref=\"BitPatterns\"/> entry.\n    /// A value of <c>-1</c> indicates an invalid code point.\n    /// This is for decoding.\n    /// </summary>\n    public static readonly int[] BackMap = [");
        for (int i = 0; i <= maxCodePoint; i++)
        {
            backmapArray.AppendLine($"    {backMap[i]}, // {i}");

        }
        backmapArray.AppendLine("];");

        Console.WriteLine("namespace FibonacciFraming;\n\n/// <summary>\n/// Look-up tables for encoding and decoding\n/// </summary>\ninternal static class LookupTables\n{");
        Console.WriteLine(bitsArray);
        Console.WriteLine(lengthsArray);
        Console.WriteLine(backmapArray);
        Console.WriteLine("\n}");
    }

    private static int[] BuildPriorityMap()
    {
        var priorityBytes  = "__ etaoinsErThAlOdIcNuSmRfHgLpDwCyUbMvFkGjPqWxYzBVKJQXZ\r\n,.-\"'(){}"u8.ToArray();
        priorityBytes[0] = 0x00;
        priorityBytes[1] = 0xFF;
        var remainingBytes = new Dictionary<int, bool>();

        for (int i = 0; i < 256; i++) remainingBytes.Add(i, true);

        var priorityMap = new int[256]; // Order the byte values. Shortest codes first

        var pos = 0;
        foreach (var b in priorityBytes)
        {
            priorityMap[pos++] = b;
            remainingBytes[b] = false;
        }

        foreach (var b in remainingBytes)
        {
            if (b.Value) priorityMap[pos++] = b.Key;
        }

        return priorityMap;
    }
}