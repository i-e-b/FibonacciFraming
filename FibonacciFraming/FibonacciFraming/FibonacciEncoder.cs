using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FibFramingTests")]

namespace FibonacciFraming;

/// <summary>
/// Deals with Fibonacci numbers
/// </summary>
internal static class FibonacciEncoder
{
    /// <summary>
    /// Start of frame 'magic number'
    /// </summary>
    internal const int FrameHead = 0x03D9; // 010101010101011000

    /// <summary>
    /// Fibonacci sequence, complete enough for uses in this project
    /// </summary>
    private static readonly int[] FibonacciSeq =
    [
        0, 1, 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233, 377, 610, 987, 1597, 2584, 4181, 6765, 10946, 17711, 28657, 46368, 75025, 121393, 196418
    ];


    /// <summary>
    /// Add a frame header to the current output position.
    /// </summary>
    public static void AddFrameHeader(BitwiseStreamWrapper output)
    {
        FibonacciEncodeOne(FrameHead, output);
    }

    /// <summary>
    /// Encode a single value to an open writable bitstream.
    /// Note that this elides the leading <c>1</c> bit,
    /// </summary>
    public static void FibonacciEncodeOne(int value, BitwiseStreamWrapper output)
    {
        // TODO: code points to skip:
        //     12, 20, 21, 33, 34, 35, 46, 54, 55, 56, 57, 58, 67, 75, 76, 88..95,
        //     101, 109, 110, 122, 123, 124, 135, 143..156, 164, 165, 177..179,
        //     190, 198..202, 211, 219, 220, 232..254, 266..268, 279, 287..291,
        //     300, 308, 309, 321..328, 334, 342, 343.
        //     ...
        //     Might be worth just having a byte:(bits,length) lookup.
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
    /// Decode a single value from an open bitstream
    /// </summary>
    public static int FibonacciDecodeOne(BitwiseStreamWrapper input) {
        var lastWas1 = false;
        var accum    = 0;
        var pos      = 0;

        while (!input.IsEmpty()) {
            if (!input.TryReadBit(out var f)) break;
            if (f > 0) {
                if (lastWas1) break;
                lastWas1 = true;
            } else lastWas1 = false;

            accum += f * FibonacciSeq[pos + 2];
            pos++;
        }

        return accum - 1;
    }

    /// <summary>
    /// Decode a single value from an open bitstream
    /// </summary>
    public static bool TryFibonacciDecodeOne(BitwiseStreamWrapper input, out int result) {
        var lastWas1 = false;
        var accum    = 0;
        var pos      = 0;

        var endPatternFound = false;
        result = -1;

        while (!input.IsEmpty()) {
            if (!input.TryReadBit(out var f)) return false;

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
        }

        result = accum - 1;
        return endPatternFound;
    }
}