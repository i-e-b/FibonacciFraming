using System.Text;
using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class TranscodingTests
{
    [Test]
    public void short_message_transcode()
    {
        const string message = "Hello, world!";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));
        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.ToSignal(input, encoded);

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.FromSignal(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));

        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Encoded = {encoded.Length} bytes; {percent:0.0}%");

        Assert.That(valid, Is.True);
    }


    [Test]
    public void longer_text_message_transcode()
    {
        const string message = "CHAPTER 106. Ahab’s Leg. The precipitating manner in which Captain Ahab had quitted the Samuel Enderby of London, had not been unattended with some small violence to his own person. He had lighted with such energy upon a thwart of his boat that his ivory leg had received a half-splintering shock.\r\n";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));
        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.ToSignal(input, encoded);

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.FromSignal(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));

        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Encoded = {encoded.Length} bytes; {percent:0.0}%");

        Assert.That(valid, Is.True);
    }
}