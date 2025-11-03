namespace FibonacciFraming;

/// <summary>
/// Transcoder to convert between byte data and transmission signals
/// </summary>
public class Transcoder
{

    /// <summary>
    /// Convert a byte data into a framed transmission signal.
    /// The result is padded with trailing zeroes.
    /// </summary>
    public static void ToSignal(Stream input, Stream output)
    {
        var dst = new BitwiseStreamWrapper(output, 0);

        // Always start with the frame header
        FibonacciEncoder.AddFrameHeader(dst);

        var b = 0;
        while ((b = input.ReadByte()) > -1)
        {
            FibonacciEncoder.FibonacciEncodeOne(b, dst); // Write the byte
            dst.WriteBit(0); // pad the end
        }
    }

    /// <summary>
    /// Convert transmission signal data in byte data.
    /// </summary>
    public static void FromSignal(Stream input, Stream output)
    {
        var src = new BitwiseStreamWrapper(input, 0);

        while (FibonacciEncoder.TryFibonacciDecodeOne(src, out var sample))
        {
            if (sample == FibonacciEncoder.FrameHead)
            {
                // Ignore frame headers at start.
                // If found after start, this is a new message. TODO: rewind?
            }
            else if (sample is > 255 or < 0)
            {
                // Error!
            }
            else
            {
                output.WriteByte((byte)sample);
            }

        }
    }
}