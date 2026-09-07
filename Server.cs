public class Server
{
    public World World { get; private set; }

    public long TickNumber { get; private set; }

    private const float PlayerSpeed = 200.0f;
    private const float TickDuration = 1.0f / 120.0f;

    public Server()
    {
        World = new World();
        TickNumber = 0;
    }

    public void Tick(InputState input)
    {
        if (input.Left)
            World.Player.X -= PlayerSpeed * TickDuration;

        if (input.Right)
            World.Player.X += PlayerSpeed * TickDuration;

        TickNumber++;
    }
}
