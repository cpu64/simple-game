using System;

public class Player
{
    public Guid Id { get; private set; }
    public double X { get; set; }
    public double Y { get; set; }

    public Player(Guid id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
    }
}
