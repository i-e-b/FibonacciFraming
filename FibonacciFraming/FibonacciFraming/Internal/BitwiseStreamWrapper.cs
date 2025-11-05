using System.Text;

namespace FibonacciFraming.Internal;

/// <summary>
/// A bitwise read/write wrapper around a byte stream.
/// Also provides run-out for reading
/// </summary>
public class BitwiseStreamWrapper
{
    private readonly Stream _original;
    private          int    _runoutBits;

    private bool _inRunOut;
    private byte _readMask, _writeMask;
    private byte  _nextOut;
    private int  _currentIn;
    private long _bitsRead;

    /// <summary>
    ///
    /// </summary>
    /// <param name="original"></param>
    /// <param name="runoutBits"></param>
    /// <exception cref="Exception"></exception>
    public BitwiseStreamWrapper(Stream original, int runoutBits)
    {
        _original = original ?? throw new Exception("Must not wrap a null stream");
        _runoutBits = runoutBits;

        _inRunOut = false;
        _readMask = 1;
        _writeMask = 0x80;
        _nextOut = 0;
        _currentIn = -1;
    }

    /// <summary>
    /// Number of bits read since the last rewind.
    /// </summary>
    // ReSharper disable once ConvertToAutoProperty
    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
    public long BitsRead => _bitsRead;

    /// <summary>
    /// Write the current pending output byte (if any)
    /// </summary>
    public void Flush()
    {
        if (_writeMask == 0x80) return; // no pending byte
        _original.WriteByte(_nextOut);
        _writeMask = 0x80;
        _nextOut = 0;
        _original.Flush();
    }

    /// <summary>
    /// Write the current pending output byte (if any)
    /// </summary>
    public async Task FlushAsync()
    {
        if (_writeMask == 0x80) return; // no pending byte
        var b0 = new[] { _nextOut };
        _writeMask = 0x80;
        _nextOut = 0;
        await _original.WriteAsync(b0.AsMemory(0, 1));
        await _original.FlushAsync();
    }

    /// <summary>
    /// Write a single bit value to the stream
    /// </summary>
    public void WriteBit(bool value)
    {
        if (value) _nextOut |= _writeMask;
        _writeMask >>= 1;

        if (_writeMask != 0) return;

        _original.WriteByte(_nextOut);
        _writeMask = 0x80;
        _nextOut = 0;
    }

    /// <summary>
    /// Write a single bit value to the stream
    /// </summary>
    public void WriteBit(int value)
    {
        if (value != 0) _nextOut |= _writeMask;
        _writeMask >>= 1;

        if (_writeMask != 0) return;

        _original.WriteByte(_nextOut);
        _writeMask = 0x80;
        _nextOut = 0;
    }

    /// <summary>
    /// Write a single bit value to the stream
    /// </summary>
    public async Task WriteBitAsync(int value)
    {
        if (value != 0) _nextOut |= _writeMask;
        _writeMask >>= 1;

        if (_writeMask == 0)
        {
            var b0 = new[] { _nextOut };
            _writeMask = 0x80;
            _nextOut = 0;
            await _original.WriteAsync(b0.AsMemory(0, 1));
        }
    }

    /// <summary>
    /// Write a bit pattern from an integer.
    /// </summary>
    /// <param name="pattern">Bits to set. Read in most-significant-first order, with least significant bit as last output.</param>
    /// <param name="length">Number of bits to write. These should be at the least-significant end of the int</param>
    public void WritePattern(int pattern, int length)
    {
        for (var i = length - 1; i >= 0; i--)
        {
            WriteBit((pattern >> i) & 1);
        }
    }

    /// <summary>
    /// Write a bit pattern from an integer.
    /// </summary>
    /// <param name="pattern">Bits to set. Read in most-significant-first order, with least significant bit as last output.</param>
    /// <param name="length">Number of bits to write. These should be at the least-significant end of the int</param>
    public async Task WritePatternAsync(int pattern, int length)
    {
        for (int i = length - 1; i >= 0; i--)
        {
            await WriteBitAsync((pattern >> i) & 1);
        }
    }

    /// <summary>
    /// Read a single bit value from the stream.
    /// Returns 1 or 0. Will return all zeros during run-out.
    /// </summary>
    public int ReadBit()
    {
        if (_inRunOut)
        {
            if (_runoutBits-- > 0) return 0;
            throw new Exception("End of input stream");
        }

        if (_readMask == 1)
        {
            _currentIn = _original.ReadByte();
            if (_currentIn < 0)
            {
                _inRunOut = true;
                if (_runoutBits-- > 0) return 0;
                throw new Exception("End of input stream");
            }

            _readMask = 0x80;
        }
        else
        {
            _readMask >>= 1;
        }

        _bitsRead++;
        return ((_currentIn & _readMask) != 0) ? 1 : 0;
    }

    /// <summary>
    /// Read a single bit value from the stream.
    /// Returns true if data can be read. Does not include run-out
    /// </summary>
    public bool TryReadBit(out int b)
    {
        b = 0;
        if (_inRunOut) return false;

        if (_readMask == 1 || _currentIn < 0)
        {
            _currentIn = _original.ReadByte();
            if (_currentIn < 0)
            {
                _inRunOut = true;
                return false;
            }

            _readMask = 0x80;
        }
        else
        {
            _readMask >>= 1;
        }

        _bitsRead++;
        b = ((_currentIn & _readMask) != 0) ? 1 : 0;
        return true;
    }

    /// <summary>
    /// Read a single bit value from the stream.
    /// Returns <c>1</c> or <c>0</c> on success.
    /// Returns <c>-1</c> on failure.
    /// </summary>
    public async Task<int> TryReadBitAsync()
    {
        if (_inRunOut) return -1;

        if (_readMask == 1 || _currentIn < 0)
        {
            var buffer = new byte[1];
            var read   = await _original.ReadAsync(buffer.AsMemory(0, 1));
            if (read < 1)
            {
                _inRunOut = true;
                return -1;
            }

            _readMask = 0x80;
        }
        else
        {
            _readMask >>= 1;
        }

        _bitsRead++;
        return ((_currentIn & _readMask) != 0) ? 1 : 0;
    }

    /// <summary>
    /// Seek underlying stream to start
    /// </summary>
    public void Rewind()
    {
        _original.Seek(0, SeekOrigin.Begin);

        _inRunOut = false;
        _readMask = 1;
        _writeMask = 0x80;
        _nextOut = 0;
        _currentIn = 0;
        _bitsRead = 0;
    }

    /// <summary>
    /// Represent this stream as a binary string.
    /// This will read the base stream from current position to the end.
    /// </summary>
    public string ToBitString()
    {
        var sb = new StringBuilder();

        while (TryReadBit(out var b))
        {
            sb.Append(b == 1 ? '1' : '0');
        }

        return sb.ToString();
    }


}