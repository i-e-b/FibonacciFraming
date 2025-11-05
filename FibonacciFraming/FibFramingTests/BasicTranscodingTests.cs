using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using FibonacciFraming;
using FibonacciFraming.Internal;
using NUnit.Framework;
using Encoder = System.Text.Encoder;

namespace FibFramingTests;

[TestFixture]
public class BasicTranscodingTests
{
    [Test]
    public void short_binary_message_transcode()
    {
        const string message = "Hello, \0\0\0world!";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));
        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.WriteMessageToStream(input, encoded);

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));

        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Encoded = {encoded.Length} bytes; {percent:0.0}%");

        Assert.That(valid.ValidMessage, Is.True);
    }

    [Test]
    public void short_text_message_transcode()
    {
        const string message = "Hello, world!";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));
        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.WriteMessageToStream(input, encoded);

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        Console.WriteLine("\r\nEncoded: "+encodeBs.ToBitString());
        encodeBs.Rewind();

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));

        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Encoded = {encoded.Length} bytes; {percent:0.0}%");

        Assert.That(valid.ValidMessage, Is.True);
    }

    [Test]
    public void longer_text_message_transcode()
    {
        const string message = "CHAPTER 106. Ahab’s Leg. The precipitating manner in which Captain Ahab had quitted the Samuel Enderby of London, had not been unattended with some small violence to his own person. He had lighted with such energy upon a thwart of his boat that his ivory leg had received a half-splintering shock.\r\n";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));
        input.Seek(0, SeekOrigin.Begin);

        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        Transcoder.WriteMessageToStream(input, encoded);

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        var encodedBitString = encodeBs.ToBitString().TrimEnd('0');
        Console.WriteLine("\r\nEncoded: " + encodedBitString);
        encodeBs.Rewind();

        Assert.That(encodedBitString, Does.Not.Contain("00000"), "Must have no more than 4 consecutive zeros");
        Assert.That(encodedBitString, Does.Not.Contain("111"), "Must have no more than 2 consecutive ones");

        var valid = Transcoder.ReadMessageFromStream(encoded, output);

        Console.WriteLine("\r\nDecoded: "+Encoding.UTF8.GetString(output.ToArray()));

        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Encoded = {encoded.Length} bytes; {percent:0.0}%");

        Assert.That(valid.ValidMessage, Is.True);
    }

    [Test]
    public void file_data_transcode()
    {
        var bytes = File.ReadAllBytes("FibonacciFraming.dll");

        using var input   = new MemoryStream(bytes);
        using var encoded = new MemoryStream();
        using var output  = new MemoryStream();

        var encodeTime = Stopwatch.StartNew();
        Transcoder.WriteMessageToStream(input, encoded);
        encodeTime.Stop();

        var encodeBs = new BitwiseStreamWrapper(encoded, 0);
        encodeBs.Rewind();

        var encodedBitString = encodeBs.ToBitString().TrimEnd('0');
        encodeBs.Rewind();

        Assert.That(encodedBitString, Does.Not.Contain("00000"), "Must have no more than 4 consecutive zeros");
        Assert.That(encodedBitString, Does.Not.Contain("111"), "Must have no more than 2 consecutive ones");

        var decodeTime = Stopwatch.StartNew();
        var valid = Transcoder.ReadMessageFromStream(encoded, output);
        decodeTime.Stop();


        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"\r\n Original data = {input.Length} bytes; Encoded = {encoded.Length} bytes ({percent:0.0}%);" +
                          $" Encode: {encodeTime.ElapsedMilliseconds} ms; Decode: {decodeTime.ElapsedMilliseconds} ms;");

        Assert.That(valid.ValidMessage, Is.True);
    }

    [Test]
    public void array_output_matches_stream_output()
    {
        const string message = "O Frugality! thou mother of ten thousand blessings--thou cook of fat beef and dainty greens!";

        using var input   = new MemoryStream(Encoding.UTF8.GetBytes(message));

        using var encoded = new MemoryStream();

        input.Seek(0, SeekOrigin.Begin);
        Transcoder.WriteMessageToStream(input, encoded);

        input.Seek(0, SeekOrigin.Begin);
        var bytes = Transcoder.GetMessageBytes(input);

        var expected = encoded.ToArray();

        Assert.That(bytes, Is.EqualTo(expected).AsCollection);
    }

    [Test]
    public void data_visualisation()
    {
        var bytes = File.ReadAllBytes("nunit.framework.dll");

        using var input   = new MemoryStream(bytes);
        using var encoded = new MemoryStream();

        var sw = Stopwatch.StartNew();
        Transcoder.WriteMessageToStream(input, encoded);
        sw.Stop();

        var percent = 100.0 * encoded.Length / input.Length;
        Console.WriteLine($"Input: {input.Length} bytes; Encoded: {encoded.Length} bytes ({percent:0.0}%); Took {sw.ElapsedMilliseconds} ms;");

        const int size = 512;

        using var fileBmp = new Bitmap(size, size, PixelFormat.Format24bppRgb);
        using var encBmp  = new Bitmap(size, size, PixelFormat.Format24bppRgb);

        var fileBits = new BitwiseStreamWrapper(input, 10240); fileBits.Rewind();
        var encBits  = new BitwiseStreamWrapper(encoded, 10240); encBits.Rewind();

        int repeatedOnes = 0;
        int repeatedZeros = 0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                var fileBit = fileBits.ReadBit() == 0;
                var encBit  = encBits.ReadBit() == 0;

                if (encBit)
                {
                    repeatedZeros++;
                    repeatedOnes = 0;
                }
                else
                {
                    repeatedOnes++;
                    repeatedZeros = 0;
                }

                if (repeatedZeros > 4) throw new Exception("Invariant failed: More than 4 zeros");
                if (repeatedOnes > 2) throw new Exception("Invariant failed: More than 2 ones");

                fileBmp.SetPixel(x, y, fileBit ? Color.BlueViolet : Color.AliceBlue);
                encBmp.SetPixel(x, y, encBit ? Color.DarkBlue : Color.Azure);
            }
        }

        fileBmp.SaveBmp("FilePattern.bmp");
        encBmp.SaveBmp("EncodedPattern.bmp");
    }

    [Test]
    public void barcode_demo()
    {
        // Spit out a bar-code using the unframed data.
        // This is just a little toy.


        var bars   = new List<bool>();
        bars.AddRange(Transcoder.Raw.GetBitPattern(4));
        bars.AddRange(Transcoder.Raw.GetBitPattern(2));
        bars.AddRange(Transcoder.Raw.GetBitPattern(1));
        bars.AddRange(Transcoder.Raw.GetBitPattern(0));
        bars.AddRange(Transcoder.Raw.GetBitPattern(1));
        bars.AddRange(Transcoder.Raw.GetBitPattern(3));
        bars.AddRange(Transcoder.Raw.GetBitPattern(1));

        Console.WriteLine(string.Join("", bars.Select(b => b ? '█' : ' ')));
        Console.WriteLine(string.Join("", bars.Select(b => b ? '▚' : '▞')));

        var width  = bars.Count;
        var height = 32;

        using var bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

        for (int x = 0; x < width; x++)
        {
            var color = bars[x] ? Color.Black : Color.White;
            for (int y = 0; y < height; y++)
            {
                bmp.SetPixel(x, y, color);
            }
        }

        bmp.SaveBmp("Barcode.bmp");
    }
}