using System.Numerics;
using System.Threading;

public class SharedInputState
{
    private sealed record Snapshot(KeyState Keys, Vector2 Pointer);

    private Snapshot state = new(KeyState.None, default);

    public (KeyState Keys, Vector2 Pointer) Read()
    {
        var snapshot = Volatile.Read(ref state);
        return (snapshot.Keys, snapshot.Pointer);
    }

    public void Write(KeyState keys, Vector2 pointer)
    {
        Volatile.Write(ref state, new Snapshot(keys, pointer));
    }
}
