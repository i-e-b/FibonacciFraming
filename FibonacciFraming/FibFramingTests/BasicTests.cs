using FibonacciFraming;
using NUnit.Framework;

namespace FibFramingTests;

[TestFixture]
public class BasicTests
{
    [Test]
    public void can_accept_a_byte_array()
    {
        var result = Transcoder.ToSignal("Hello, world!"u8.ToArray());

        Assert.That(result, Is.Not.Null);
    }
}