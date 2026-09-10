using System;

public class InputCommand
{
    // Command @ N is the command that produces World @ N
    // when applied to World @ N-1.
    //
    // Therefore, when Command.Tick == World.Tick,
    // that command has already been applied to the world.
    public long Tick { get; private set; }

    public Guid PlayerId { get; private set; }

    public InputState Input { get; private set; }

    public InputCommand(
        long tick,
        Guid playerId,
        InputState input)
    {
        Tick = tick;
        PlayerId = playerId;
        Input = input;
    }
}
