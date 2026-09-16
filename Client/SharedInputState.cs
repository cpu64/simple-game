using System.Threading;

public class SharedInputState
{
    private int value;
    private readonly object sync = new();

    private float x;
    private float y;

    public (float X, float Y) ReadXY()
    {
        lock (sync)
        {
            return (x, y);
        }
    }

    public void Write(float x, float y)
    {
        lock (sync)
        {
            this.x = x;
            this.y = y;
        }
    }

    public KeyState Read()
    {
        return (KeyState)Volatile.Read(ref value);
    }

    public void Press(KeyState input)
    {
        Interlocked.Or(ref value, (int)input);
    }

    public void Release(KeyState input)
    {
        Interlocked.And(ref value, ~(int)input);
    }
}
