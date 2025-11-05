namespace FibonacciFraming;

/// <summary>
/// Result metadata from an attempt to read a message from a stream
/// </summary>
public class MessageResult
{
    /// <summary>
    /// Number of bits read from the stream
    /// </summary>
    public long BitsRead { get; set; }

    /// <summary>
    /// Set to <c>true</c> if a correct message header was found
    /// </summary>
    public bool SawHeader { get; set; }

    /// <summary>
    /// Set to <c>true</c> if a correct message footer was found
    /// </summary>
    public bool SawFooter { get; set; }

    /// <summary>
    /// Set to <c>true</c> if the entire message was read without errors.
    /// </summary>
    public bool CleanData { get; set; }

    /// <summary>
    /// Returns <c>true</c> if the message was fully formed with no errors.
    /// </summary>
    public bool ValidMessage => BitsRead >= 32 && SawHeader && SawFooter && CleanData;

    /// <inheritdoc />
    public override string ToString()
    {
        return $"[Bits={BitsRead}; Header={SawHeader}; Footer={SawFooter}; Clean={CleanData};]";
    }
}