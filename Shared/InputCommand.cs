using System;

public class InputCommand
{
    public long Sequence { get; private set; }

    public long LocalTick { get; private set; }

    public Guid PlayerId { get; private set; }

    public InputState Input { get; private set; }

    public InputCommand(long sequence, long localTick, Guid playerId, InputState input)
    {
        Sequence = sequence;
        LocalTick = localTick;
        PlayerId = playerId;
        Input = input;
    }
}
