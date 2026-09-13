public class WorldMessage : IMessage
{
    public World World { get; set; }

    public WorldMessage() { }

    public WorldMessage(World world)
    {
        World = world;
    }
}
