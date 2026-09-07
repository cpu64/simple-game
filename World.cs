public class World
{
    public Player Player { get; private set; }

    public World()
    {
        Player = new Player(400, 300);
    }
}
