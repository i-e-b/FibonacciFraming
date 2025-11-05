using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FibFramingTests")]

namespace FibonacciFraming.Internal;

/// <summary>
/// Deals with Fibonacci numbers
/// </summary>
internal static class FibonacciEncoder
{
    /// <summary>
    /// Start of frame 'magic number'
    /// </summary>
    internal const int FrameHead = 0x063B; // 1010101010101011

    internal const int FrameFoot = 0x0552; // 0101010101010011

    /// <summary>
    /// Fibonacci sequence, complete enough for uses in this project
    /// </summary>
    private static readonly int[] FibonacciSeq =
    [
        0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584, 4181, 6765, 10946,
        17711, 28657, 46368, 75025, 121393, 196418, 317811, 514229, 832040, 1346269, 2178309, 3524578
    ];

    /// <summary>
    /// Maximum bit position to accept as an input
    /// </summary>
    private static readonly int MaxPos = FibonacciSeq.Length - 3;


    /// <summary>
    /// Add a frame header to the current output position.
    /// </summary>
    public static void AddFrameHeader(BitwiseStreamWrapper output)
    {
        FibonacciEncodeOne(FrameHead, output);
    }

    /// <summary>
    /// Add a frame footer to the current output position.
    /// </summary>
    public static void AddFrameFooter(BitwiseStreamWrapper output)
    {
        FibonacciEncodeOne(FrameFoot, output);
    }

    /// <summary>
    /// Encode a single value to an open writable bitstream.
    /// Note that this elides the leading <c>1</c> bit,
    /// </summary>
    public static void FibonacciEncodeOne(int value, BitwiseStreamWrapper output)
    {
        var n = value + 1;

        var res = 1;
        var count = 1;

        // find the smallest fibonacci number greater than `n`
        int f = 1, k = 1;
        while (f <= n)
        {
            f = FibonacciSeq[++k];
        }

        // decompose back through the sequence
        while (--k > 1)
        {
            f = FibonacciSeq[k];
            count++;
            res <<= 1;

            if (f <= n)
            {
                res |= 1;
                n -= f;
            }
        }

        // Output bits in order (with '11' pattern last)
        for (int i = 0; i < count; i++)
        {
            output.WriteBit((res & 1) == 1);
            res >>= 1;
        }
    }

    /// <summary>
    /// Decode a single value from an open bitstream.
    /// Returns <c>-1</c> on error or end-of-stream.
    /// </summary>
    public static int FibonacciDecodeOne(BitwiseStreamWrapper input)
    {
        if (TryFibonacciDecodeOne(input, out var result)) return result;
        return -1;
    }

    /// <summary>
    /// Decode a single value from an open bitstream.
    /// </summary>
    /// <param name="input">Input to read. Will be read from current position</param>
    /// <param name="skipLeadIn">Should normally be <c>false</c>. If <c>true</c>, patterns of all zeros, all ones, or alternations will be skipped</param>
    /// <param name="result">Decoded integer value</param>
    public static bool TryFibonacciDecodeOnePadded(BitwiseStreamWrapper input, bool skipLeadIn, out int result)
    {
        const int maskWindow = 0x1FFFF; // 17 bit window for header/footer, plus lead-out
        result = -1;

        // 17 bit window, try to find 0110, guess a length (based on skipLeadIn), decode from there

        var bitsRead   = 0;
        var pattern    = 0;
        var length     = 0;
        var terminator = false;
        while (input.TryReadBit(out var f))
        {
            bitsRead++;

            pattern = ((pattern << 1) | (f & 1)) & maskWindow;

            if (pattern != 0 || !skipLeadIn) length++;

            if (!skipLeadIn && bitsRead > 32) // signal went dead
            {
                return false;
            }

            // have we got an end pattern?
            if ((pattern & 0b1111) != 0b0110) continue;

            terminator = true;
            break;
        }
        if (length > 17) length = 17;

        // back up to make `0110` -> `01`
        pattern >>= 2;
        length -= 2;

        var accum = 0;
        var pos   = 0;
        for (int i = length - 1; i >= 0; i--)
        {
            var f = (pattern >> i) & 1;

            accum += f * FibonacciSeq[pos + 2];
            pos++;
        }

        result = accum - 1;
        return terminator;
    }

    /// <summary>
    /// Decode a single value from an open bitstream.
    /// </summary>
    /// <param name="input">Input to read. Will be read from current position</param>
    /// <param name="result">Decoded integer value</param>
    public static bool TryFibonacciDecodeOne(BitwiseStreamWrapper input, out int result) {
        var lastWas1 = false;
        var endPatternFound = false;
        var accum = 0;
        var pos   = 0;
        result = -1;


        while (input.TryReadBit(out var f)) {
            if (f > 0) {
                if (lastWas1)
                {
                    endPatternFound = true;
                    break;
                }

                lastWas1 = true;
            } else lastWas1 = false;

            accum += f * FibonacciSeq[pos + 2];
            pos++;

            if (pos > MaxPos) return false;
        }

        result = accum - 1;
        return endPatternFound;
    }

    /// <summary>
    /// Encode a Fibonacci code into an int
    /// </summary>
    public static void FibonacciEncodeInt(int value, out int output, out int length)
    {
        var n = value + 1;

        var res   = 1;
        var count = 1;

        // find the smallest fibonacci number greater than `n`
        int f = 1, k = 1;
        while (f <= n)
        {
            f = FibonacciSeq[++k];
        }

        // decompose back through the sequence
        while (--k > 1)
        {
            f = FibonacciSeq[k];
            count++;
            res <<= 1;

            if (f <= n)
            {
                res |= 1;
                n -= f;
            }
        }

        // Output bits in order (with '11' pattern last)
        length = count;
        output = 0;
        for (int i = 0; i < count; i++)
        {
            output = (output << 1) | (res & 1);
            res >>= 1;
        }
    }
}