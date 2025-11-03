using System.Text;
using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class BasicTests
{
    [Test]
    public void can_accept_a_byte_array()
    {
        //var result = Transcoder.ToSignal("Hello, world!"u8.ToArray());

        //Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void frame_head_pattern_is_correct()
    {
        var pattern = "010101010101011";

        var data = new MemoryStream();
        var dst  = new BitwiseStreamWrapper(data, 0);

        for (int i = 0; i < pattern.Length; i++)
        {
            var c = pattern[i];
            if (c == '1') dst.WriteBit(1);
            else dst.WriteBit(0);
        }

        dst.Flush();
        dst.Rewind();

        var result = FibonacciEncoder.FibonacciDecodeOne(dst);
        Console.WriteLine($"Encoded result = {result} (0x{result:X})");
    }


    [Test]
    public void fib_coding_frame_header()
    {
        var input = FibonacciEncoder.FrameHead;
        var data  = new MemoryStream();
        var src   = new BitwiseStreamWrapper(data, 0);

        FibonacciEncoder.FibonacciEncodeOne(input, src);

        src.Flush();
        src.Rewind();

        var sb = new StringBuilder();
        while (src.TryReadBit(out var b))
        {
            sb.Append(b == 1 ? '1' : '0');
        }

        var encoded = sb.ToString();
        Console.WriteLine("Encoded: "+encoded);

        Assert.That(encoded, Does.StartWith("0101010101011"), "Make sure we have the 01 repeating pattern");

        src.Rewind();

        var result = FibonacciEncoder.FibonacciDecodeOne(src);

        Assert.That(result, Is.EqualTo(input));
    }


    [Test]
    public void fib_coding_full_spread_of_byte_values()
    {
        Console.WriteLine("dec -> fib binary");

        var codePoints = 0;
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
                Console.WriteLine($"{codePoints:000} -> {sb} ({input})");
                codePoints++;
            }

            src.Rewind();

            var result = FibonacciEncoder.FibonacciDecodeOne(src);

            Assert.That(result, Is.EqualTo(input));

            if (codePoints >= 256) break;
        }
    }
}