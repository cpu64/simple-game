using System.Threading;

public class SharedInputState
{
    private int value;

    public InputState Read()
    {
        return (InputState)Volatile.Read(ref value);
    }

    public void Press(InputState input)
    {
        Interlocked.Or(ref value, (int)input);
    }

    public void Release(InputState input)
    {
        Interlocked.And(ref value, ~(int)input);
    }
}
