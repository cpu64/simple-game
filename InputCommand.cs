public class InputCommand
{
    public long Tick { get; private set; }
    public InputState Input { get; private set; }

    public InputCommand(long tick, InputState input)
    {
        Tick = tick;
        Input = input;
    }
}
