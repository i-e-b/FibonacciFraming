using System.Text;
using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class CodePointGenerationTests
{
    [Test]
    [TestCase("header", "010101010101011")] // header pattern
    [TestCase("footer", "101010101010011")] // footer pattern
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

        Assert.That(encoded, Does.StartWith("010101010101011"), "Make sure we have the 01 repeating pattern");

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

        Assert.That(encoded, Does.StartWith("101010101010011"), "Make sure we have the 01 repeating pattern");

        src.Rewind();

        var result = FibonacciEncoder.FibonacciDecodeOne(src);

        Assert.That(result, Is.EqualTo(input));
    }


    [Test(Description = "This generates the main look-up tables for encode/decode")]
    public void fib_coding_mapping_tables()
    {
        Console.WriteLine("// dec -> fib binary");

        var bitsArray    = new StringBuilder();
        var lengthsArray = new StringBuilder();
        var backmapArray      = new StringBuilder();

        bitsArray.AppendLine   ("private static readonly int[] BitPatterns = [");
        lengthsArray.AppendLine("private static readonly int[] BitLengths = [");

        var backMap      = new int[500];
        var codePoints   = 0;
        var maxCodePoint = 0;
        for (var input = 0; input < 500; input++)
        {
            var data = new MemoryStream();
            var src  = new BitwiseStreamWrapper(data, 0);
            FibonacciEncoder.FibonacciEncodeOne(input, src);

            src.Flush();
            src.Rewind();

            var runningZero = 0;
            var runningOne  = 0;
            var ok          = true;
            var sb          = new StringBuilder();

            while (src.TryReadBit(out var b))
            {
                if (b == 1)
                {
                    runningOne++;
                    runningZero = 0;
                }
                else
                {
                    runningZero++;
                    runningOne = 0;
                }

                if (runningZero > 4) ok = false;

                sb.Append(b == 1 ? '1' : '0');
                if (runningOne > 1) break;
            }

            if (ok)
            {
                FibonacciEncoder.FibonacciEncodeInt(input, out var bits, out var length);
                bitsArray.AppendLine($"    {bits}, // {codePoints:000} -> {sb} ({input})");
                lengthsArray.AppendLine($"    {length}, // {codePoints:000} -> {sb} ({input})");
                backMap[input] = codePoints;
                maxCodePoint = input;

                codePoints++;
            }
            else
            {
                backMap[input] = -1;
            }

            src.Rewind();

            var result = FibonacciEncoder.FibonacciDecodeOne(src);

            Assert.That(result, Is.EqualTo(input));

            if (codePoints >= 256) break;
        }
        bitsArray.AppendLine("];");
        lengthsArray.AppendLine("];");

        backmapArray.AppendLine("private static readonly int[] BackMap = [");
        for (int i = 0; i <= maxCodePoint; i++)
        {
            backmapArray.AppendLine($"    {backMap[i]},");

        }
        backmapArray.AppendLine("];");

        Console.WriteLine(bitsArray);
        Console.WriteLine(lengthsArray);
        Console.WriteLine(backmapArray);
    }
}