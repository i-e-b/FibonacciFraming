using System.Text;
using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class TranscodingTests
{
    [Test]
    public void basic_message_transcode()
    {
        const string message = "Hello, world!";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));
        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.ToSignal(input, encoded);

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("Encoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.FromSignal(encoded, output);

        Console.WriteLine(Encoding.UTF8.GetString(output.ToArray()));

        Assert.That(valid, Is.True);
    }
}