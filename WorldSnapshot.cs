public class WorldSnapshot
{
    public Player Player { get; private set; }

    public WorldSnapshot(Player player)
    {
        Player = new Player(player.X, player.Y);
    }
}
