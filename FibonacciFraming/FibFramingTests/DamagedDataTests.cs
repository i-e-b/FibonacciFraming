using System.Text;
using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class DamagedDataTests
{
    [Test(Description = "Ensure the transcoder can correctly skip quiet signal before a message")]
    public void all_zero_quiet_lead_in_can_be_skipped()
    {
        const string message = "Hello, world!";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        // Add quiet signal before
        for (int i = 0; i < 16; i++) { encoded.WriteByte(0); }

        Transcoder.WriteMessageToStream(input, encoded);

        // Add quiet signal after
        for (int i = 0; i < 16; i++) { encoded.WriteByte(0); }

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes;");

        Assert.That(valid, Is.True);
    }


    // This one is easy, as the idle is in phase with the signal, without adding a `0110` pattern
    [Test(Description = "Ensure the transcoder can correctly skip idle signal before a message")]
    public void odd_alternation_lead_in_can_be_skipped()
    {
        const string message = "Hello, world!";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        // Add quiet signal before
        for (int i = 0; i < 16; i++) { encoded.WriteByte(0xAA); }

        Transcoder.WriteMessageToStream(input, encoded);

        // Add quiet signal after
        for (int i = 0; i < 16; i++) { encoded.WriteByte(0xAA); }

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes;");

        Assert.That(valid, Is.True);
    }


    // This one is tricky, as the phase switch introduces a `0110` pattern
    [Test(Description = "Ensure the transcoder can correctly skip idle signal before a message")]
    public void even_alternation_lead_in_can_be_skipped()
    {
        const string message = "Hello, world!";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        // Add quiet signal before
        //for (int i = 0; i < 16; i++) { encoded.WriteByte(0x55); }
        encoded.WriteByte(0x55);

        Transcoder.WriteMessageToStream(input, encoded);

        // Add quiet signal after
        for (int i = 0; i < 16; i++) { encoded.WriteByte(0x55); }

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes;");

        Assert.That(valid, Is.True);
    }
}