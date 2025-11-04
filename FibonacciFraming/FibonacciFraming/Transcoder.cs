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
    public static void WriteMessageToStream(Stream input, Stream output)
    {
        var dst = new BitwiseStreamWrapper(output, 0);

        // Always start with the frame header
        FibonacciEncoder.AddFrameHeader(dst);
        dst.WriteBit(0); // pad the end

        // Write all bytes
        int b;
        while ((b = input.ReadByte()) > -1)
        {
            var pattern = LookupTables.BitPatterns[b];
            var length  = LookupTables.BitLengths[b];

            dst.WritePattern(pattern, length);
            dst.WriteBit(0); // pad the end
        }

        // End with the frame footer to fully close messages
        FibonacciEncoder.AddFrameFooter(dst);

        dst.Flush();
    }

    /// <summary>
    /// Convert transmission signal data in byte data.
    /// Returns <c>true</c> if there were no errors, <c>false</c> if any errors are detected.
    /// </summary>
    public static bool ReadMessageFromStream(Stream input, Stream output)
    {
        var src = new BitwiseStreamWrapper(input, 0);

        var sawHeader  = false;
        var sawFooter  = false;
        var noErrors   = true;
        var skipLeadIn = true;

        // TODO: handle garbage data before the frame head.

        while (FibonacciEncoder.TryFibonacciDecodeOnePadded(src, skipLeadIn, out var sample))
        {
            skipLeadIn = false;
            if (sample == FibonacciEncoder.FrameHead)
            {
                // Start of message.
                // If found after start, then we have an error
                if (sawHeader) break; // TODO: need to signal that the stream could be rewound to the start of this head to try again
                sawHeader = true;
            }
            else if (sample == FibonacciEncoder.FrameFoot)
            {
                // End of message.
                // If found before header, then we have an error
                sawFooter = true;
                break;
            }
            else if (sample >= LookupTables.BackMap.Length)
            {
                // Error, but keep going?
                noErrors = false;
            }
            else
            {
                var byteVal = LookupTables.BackMap[sample];
                if (byteVal < 0) noErrors = false; // bit pattern we don't output
                else output.WriteByte((byte)byteVal);
            }
        }

        Console.WriteLine($"head:{sawHeader}; foot:{sawFooter}; clean:{noErrors};");
        return sawHeader && sawFooter && noErrors;
    }
}