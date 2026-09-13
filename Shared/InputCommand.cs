using System;

public class InputCommand
{
    public long Sequence { get; private set; }

    public Guid PlayerId { get; private set; }

    public InputState Input { get; private set; }

    public InputCommand(long sequence, Guid playerId, InputState input)
    {
        Sequence = sequence;
        PlayerId = playerId;
        Input = input;
    }
}
