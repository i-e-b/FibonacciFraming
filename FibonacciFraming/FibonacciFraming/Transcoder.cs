using FibonacciFraming.Internal;

namespace FibonacciFraming;

/// <summary>
/// Transcoder to convert between byte data and transmission signals
/// </summary>
public static class Transcoder
{
    /// <summary>
    /// Convert a byte data into a framed transmission signal.
    /// The result is padded with trailing zeroes.
    /// </summary>
    public static void WriteMessageToStream(Stream input, Stream output)
    {
        var dst = new BitwiseStreamWrapper(output, 0);

        // Always start with the frame header
        dst.WritePattern(LookupTables.FrameHeadPattern, LookupTables.FrameHeadLength);
        dst.WriteBit(0);

        // Write all bytes
        int b;
        while ((b = input.ReadByte()) > -1)
        {
            var pattern = LookupTables.BitPatterns[b];
            var length  = LookupTables.BitLengths[b];

            dst.WritePattern(pattern, length);
            dst.WriteBit(0);
        }

        // End with the frame footer to fully close messages
        dst.WritePattern(LookupTables.FrameFootPattern, LookupTables.FrameFootLength);
        dst.WriteBit(0);

        dst.Flush();
    }

    /// <summary>
    /// Convert a byte data into a framed transmission signal.
    /// The result is padded with trailing zeroes.
    /// </summary>
    public static async Task WriteMessageToStreamAsync(Stream input, Stream output)
    {
        var dst = new BitwiseStreamWrapper(output, 0);

        // Always start with the frame header
        await dst.WritePatternAsync(LookupTables.FrameHeadPattern, LookupTables.FrameHeadLength);
        await dst.WriteBitAsync(0);

        // Write all bytes
        int b;
        while ((b = input.ReadByte()) > -1)
        {
            var pattern = LookupTables.BitPatterns[b];
            var length  = LookupTables.BitLengths[b];

            await dst.WritePatternAsync(pattern, length);
            await dst.WriteBitAsync(0);
        }

        // End with the frame footer to fully close messages
        await dst.WritePatternAsync(LookupTables.FrameFootPattern, LookupTables.FrameFootLength);
        await dst.WriteBitAsync(0);

        await dst.FlushAsync();
    }

    /// <summary>
    /// Convert a byte data into a framed transmission signal.
    /// The result is padded with trailing zeroes.
    /// </summary>
    public static void WriteMessageToStream(byte[] input, Stream output)
    {
        using var src = new MemoryStream(input);
        WriteMessageToStream(src, output);
    }

    /// <summary>
    /// Convert a byte data into a framed transmission signal.
    /// The result is padded with trailing zeroes.
    /// </summary>
    public static byte[] GetMessageBytes(Stream input)
    {
        using var mem = new MemoryStream();

        var dst = new BitwiseStreamWrapper(mem, 0);

        // Always start with the frame header
        dst.WritePattern(LookupTables.FrameHeadPattern, LookupTables.FrameHeadLength);
        dst.WriteBit(0);

        // Write all bytes
        int b;
        while ((b = input.ReadByte()) > -1)
        {
            var pattern = LookupTables.BitPatterns[b];
            var length  = LookupTables.BitLengths[b];

            dst.WritePattern(pattern, length);
            dst.WriteBit(0);
        }

        // End with the frame footer to fully close messages
        dst.WritePattern(LookupTables.FrameFootPattern, LookupTables.FrameFootLength);
        dst.WriteBit(0);
        dst.Flush();
        dst.Rewind();

        return mem.ToArray();
    }

    /// <summary>
    /// Convert transmission signal data in byte data.
    /// Returns <c>true</c> if there were no errors, <c>false</c> if any errors are detected.
    /// </summary>
    public static MessageResult ReadMessageFromStream(Stream input, Stream output)
    {
        var src    = new BitwiseStreamWrapper(input, 0);
        var result = new MessageResult();

        var sawHeader   = false;
        var sawFooter   = false;
        var noErrors    = true;
        var skipLeadIn  = true;
        var anyRealData = false;
        var runOn       = false;

        while (FibonacciEncoder.TryFibonacciDecodeOnePadded(src, skipLeadIn, out var sample))
        {
            skipLeadIn = false;
            if (sample == LookupTables.FrameHead)
            {
                // Start of message.
                // If found after start, then we have an error
                if (sawHeader && anyRealData)
                {
                    runOn = true;
                    break;
                }

                sawHeader = true;
            }
            else if (sample == LookupTables.FrameFoot)
            {
                // End of message.
                // If found before header, then we have an error
                sawFooter = true;
                break;
            }
            else if (sample >= LookupTables.BackMap.Length)
            {
                // Error, but keep going?
                Console.WriteLine($"Fault: Bad pattern = {sample};");
                noErrors = false;
            }
            else
            {
                var byteVal = LookupTables.BackMap[sample];
                if (byteVal < 0)
                {
                    Console.WriteLine($"Fault: Bad pattern = {sample};");
                    noErrors = false; // bit pattern we don't output
                }
                else
                {
                    anyRealData = true;
                    output.WriteByte((byte)byteVal);
                }
            }
        }

        result.BitsRead = src.BitsRead;
        result.SawHeader = sawHeader;
        result.SawFooter = sawFooter;
        result.RunOnDetected = runOn;
        result.CleanData = noErrors;

        return result;
    }

    /// <summary>
    /// Convert transmission signal data in byte data.
    /// Returns <c>true</c> if there were no errors, <c>false</c> if any errors are detected.
    /// </summary>
    public static async Task<MessageResult> ReadMessageFromStreamAsync(Stream input, Stream output)
    {
        var src    = new BitwiseStreamWrapper(input, 0);
        var result = new MessageResult();

        var sawHeader   = false;
        var sawFooter   = false;
        var noErrors    = true;
        var skipLeadIn  = true;
        var anyRealData = false;
        var runOn       = false;

        while (true)
        {
            var symbol = await FibonacciEncoder.TryFibonacciDecodeOnePaddedAsync(src, skipLeadIn);
            if (symbol.Failed) break;

            var sample = symbol.Sample;
            skipLeadIn = false;
            if (sample == LookupTables.FrameHead)
            {
                // Start of message.
                // If found after start, then we have an error
                if (sawHeader && anyRealData)
                {
                    runOn = true;
                    break;
                }

                sawHeader = true;
            }
            else if (sample == LookupTables.FrameFoot)
            {
                // End of message.
                // If found before header, then we have an error
                sawFooter = true;
                break;
            }
            else if (sample >= LookupTables.BackMap.Length)
            {
                // Error, but keep going?
                Console.WriteLine($"Fault: Bad pattern = {sample};");
                noErrors = false;
            }
            else
            {
                var byteVal = LookupTables.BackMap[sample];
                if (byteVal < 0)
                {
                    Console.WriteLine($"Fault: Bad pattern = {sample};");
                    noErrors = false; // bit pattern we don't output
                }
                else
                {
                    anyRealData = true;
                    output.WriteByte((byte)byteVal);
                }
            }
        }

        result.BitsRead = src.BitsRead;
        result.SawHeader = sawHeader;
        result.SawFooter = sawFooter;
        result.RunOnDetected = runOn;
        result.CleanData = noErrors;

        return result;
    }

    /// <summary>
    /// Lower-level transcoding
    /// </summary>
    public static class Raw
    {
        /// <summary>
        /// Get the pattern for a single byte as an array of boolean values
        /// </summary>
        public static bool[] GetBitPattern(byte value)
        {
            var pattern = LookupTables.BitPatterns[value];
            var length  = LookupTables.BitLengths[value];

            var result = new bool[length];

            var j = 0;
            for (int i = length - 1; i >= 0; i--)
            {
                result[j++] = ((pattern >> i) & 1) == 1;
            }

            return result;
        }
    }

}