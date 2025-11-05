using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class RawGenerationTests
{
    [Test]
    public void get_bool_array_for_byte()
    {
        for (int i = 0; i < 256; i++)
        {
            var result = Transcoder.Raw.GetBitPattern((byte)i);
            Console.WriteLine($"{i} -> {string.Join("", result.Select(b => b ? 'X' : ' '))}");
        }
    }
}