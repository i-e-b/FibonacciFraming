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
        var result = Transcoder.ToSignal("Hello, world!"u8.ToArray());

        Assert.That(result, Is.Not.Null);
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
        for (var input = 0; input < 256; input++)
        {
            var data = new MemoryStream();
            var src  = new BitwiseStreamWrapper(data, 0);
            FibonacciEncoder.FibonacciEncodeOne(input, src);

            src.Flush();
            src.Rewind();

            Console.Write($"{input:000} -> ");

            while (src.TryReadBit(out var b))
            {
                Console.Write(b == 1 ? "1" : "0");
            }

            Console.WriteLine();

            src.Rewind();

            var result = FibonacciEncoder.FibonacciDecodeOne(src);

            Assert.That(result, Is.EqualTo(input));
        }
    }
}