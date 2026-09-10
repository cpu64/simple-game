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
        int oldValue;
        int newValue;

        do {
            oldValue = Volatile.Read(ref value);
            newValue = oldValue | (int)input;
        }
        while (Interlocked.CompareExchange(ref value, newValue, oldValue) != oldValue);
    }

    public void Release(InputState input)
    {
        int oldValue;
        int newValue;

        do {
            oldValue = Volatile.Read(ref value);
            newValue = oldValue & ~(int)input;
        }
        while (Interlocked.CompareExchange(ref value, newValue, oldValue) != oldValue);
    }
}
