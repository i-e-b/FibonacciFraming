using System.Text;
using FibonacciFraming;
using FibonacciFraming.Internal;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class DamagedDataTests
{
    [Test(Description = "Ensure the transcoder can correctly skip quiet signal before a message")]
    [TestCase(0x00)]
    [TestCase(0xFF)]
    [TestCase(0xAA)]
    [TestCase(0x55)]
    public void lead_in_can_be_skipped(byte quietSignal)
    {
        const string message = "Hello, world!";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        // Add quiet signal before
        for (int i = 0; i < 16; i++) { encoded.WriteByte(quietSignal); }

        Transcoder.WriteMessageToStream(input, encoded);

        // Add quiet signal after
        for (int i = 0; i < 16; i++) { encoded.WriteByte(quietSignal); }

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        var result = Encoding.UTF8.GetString(output.ToArray());
        Console.WriteLine("\r\nDecoded: "+result);
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes; Fully valid = {valid};");

        Assert.That(result, Is.EqualTo(message));
    }

    [Test(Description = "Simulating a disconnected line being restored")]
    [TestCase(0x00)]
    [TestCase(0xFF)]
    [TestCase(0xAA)]
    [TestCase(0x55)]
    public void recovered_line_captures_some_data(byte deadLineSignal)
    {
        const string message = "Where sits our sulky sullen dame, Gathering her brows like gathering storm, Nursing her wrath to keep it warm.";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.WriteMessageToStream(input, encoded);

        var damaged = encoded.ToArray();

        // Cut start of transmission
        var end = damaged.Length / 2;
        if (deadLineSignal == 0x55) end++; // hack to stop the alternating signal looking like a header
        for (int i = 0; i < end; i++)
        {
            damaged[i] = deadLineSignal;
        }

        using var damagedStream = new MemoryStream(damaged);

        // Show the damaged result
        var dmgBs = new BitwiseStreamWrapper(damagedStream, 0);
        dmgBs.Rewind();
        Console.WriteLine("\r\nEncoded: "+dmgBs.ToBitString());
        dmgBs.Rewind();

        damagedStream.Seek(0, SeekOrigin.Begin);

        var valid = Transcoder.ReadMessageFromStream(damagedStream, output);

        var result = Encoding.UTF8.GetString(output.ToArray());
        Console.WriteLine("\r\nDecoded: "+result);
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes; Info = {valid};");

        Assert.That(valid.ValidMessage, Is.False, "Damage should have been detected");
        Assert.That(valid.BitsRead, Is.GreaterThan(0), "Decoding should have started");
    }

    [Test(Description = "Simulating a line being disconnected mid-message")]
    [TestCase(0x00)]
    [TestCase(0xFF)]
    [TestCase(0xAA)]
    [TestCase(0x55)]
    public void dead_line_is_marked_as_end_of_message(byte deadLineSignal)
    {
        const string message = "Where sits our sulky sullen dame, Gathering her brows like gathering storm, Nursing her wrath to keep it warm.";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.WriteMessageToStream(input, encoded);

        var damaged = encoded.ToArray();

        // Cut transmission
        for (int i = damaged.Length / 2; i < damaged.Length; i++)
        {
            damaged[i] = deadLineSignal;
        }

        using var damagedStream = new MemoryStream(damaged);

        // Show the damaged result
        var dmgBs = new BitwiseStreamWrapper(damagedStream, 0);
        dmgBs.Rewind();
        Console.WriteLine("\r\nEncoded: "+dmgBs.ToBitString());
        dmgBs.Rewind();

        damagedStream.Seek(0, SeekOrigin.Begin);

        var valid = Transcoder.ReadMessageFromStream(damagedStream, output);

        var result = Encoding.UTF8.GetString(output.ToArray());
        Console.WriteLine("\r\nDecoded: "+result);
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes; Fully valid = {valid};");

        Assert.That(valid.ValidMessage, Is.False, "Damage should have been detected");
        Assert.That(valid.BitsRead, Is.LessThan(damaged.Length * 8), "Decoding should have stopped before end of stream");
    }

    [Test(Description = "No guarantee to find bit damage, but the likelihood is pretty good.")]
    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    [TestCase(10)]
    [TestCase(20)]
    public void slight_random_damage_can_likely_be_detected(int bitsOfDamage)
    {
        const string message = "I kiss thy hand, but not in flattery, Caesar;";

        using var input = new MemoryStream(Encoding.UTF8.GetBytes(message));

        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.WriteMessageToStream(input, encoded);

        var damaged = encoded.ToArray();

        // Do some damage
        for (int i = 0; i < bitsOfDamage; i++)
        {
            var idx = Random.Shared.Next(0, damaged.Length);
            var bit = Random.Shared.Next(0, 8);
            damaged[idx] = (byte)(damaged[idx] ^ (1 << bit));
        }

        using var damagedStream = new MemoryStream(damaged);

        // Show the damaged result
        var dmgBs = new BitwiseStreamWrapper(damagedStream, 0);
        dmgBs.Rewind();
        Console.WriteLine("\r\nEncoded: "+dmgBs.ToBitString());
        dmgBs.Rewind();

        damagedStream.Seek(0, SeekOrigin.Begin);

        var valid = Transcoder.ReadMessageFromStream(damagedStream, output);

        var result = Encoding.UTF8.GetString(output.ToArray());
        Console.WriteLine("\r\nDecoded: "+result);
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Recovered = {output.Length} bytes; Meta: {valid};");

        if (bitsOfDamage < 5 && valid.ValidMessage) Assert.Inconclusive("Very low damage was missed");
        else Assert.That(valid.ValidMessage, Is.False, "Damage should have been detected");
    }
}