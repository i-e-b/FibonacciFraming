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
    public static bool FromSignal(Stream input, Stream output)
    {
        var src = new BitwiseStreamWrapper(input, 0);

        var sawHeader = false;
        var sawFooter = false;
        var noErrors = true;

        // TODO:
        //    - Ignore extra zeroes before frame head

        while (FibonacciEncoder.TryFibonacciDecodeOne(src, out var sample))
        {
            if (sample == FibonacciEncoder.FrameHead)
            {
                // Start of message.
                // If found after start, then we have an error
                if (sawHeader) break;
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

            // Read the padding bit
            if (!src.TryReadBit(out var zero) || zero != 0)
            {
                // Error
                noErrors = false;
            }
        }

        return sawHeader && sawFooter && noErrors;
    }
}