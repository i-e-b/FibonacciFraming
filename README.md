# FibonacciFraming

An experimental fault resilient binary signal format for byte data

## Purpose

A digital transmission signal for packet data over any carrier capable of binary encoding.
The signal should be dense, self-synchronising and as error-resilient as possible.

## Implementation

All bytes from the original data are encoded as Fibonacci universal codes, between 2 and 13 bits long.
This gives around 1.6 output samples per bit, with a total of around 2.2 samples per input bit
for short messages when including framing and encoding.

Each frame starts with at least one instance of the 15 bit pattern `010101010101011`,
which represents decimal 985 (hex 0x03D9, never valid input from a byte).
This gives a long sequence of alternating values to latch timing.

An extra `0` is added after each byte pattern

A stream reader can synchronise by looking for `0110`, which should occur at the end of every 'byte'.
The frame header gives both timing signal and start-of-frame in minimal space.
The receiver can also expect to never see the sequence `111` or any more than 2 consecutive `1`s

Any time the decoder sees an invalid sequence, it can mark the output as invalid.
Extra `1` bits in the data are likely to trigger the inter-byte pattern `0110`,
and missing `1` bits can merge two byte patterns together;
so the final length of damaged data is not likely to be the same as the input.

The resulting signal is highly patterned.
`1` states are always either 1 or 2 samples long, `0` states are between 1 and 11 samples long.


## To-do

- [x] Arrange code-point mapping to remove long `0` sample states, ideally no more than 4. Maybe skip values with large `0` runs, as we have spare before hitting 14 bits
- [ ] Rearrange the code points to give shortest codes to inputs `0x00`, `0xFF`, and longest codes into `0x01..0x1F` (to encode ASCII slightly more efficiently)
- [ ] Add Reed-Solomon correction
