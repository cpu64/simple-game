using System;

public class Player
{
    public Guid Id { get; private set; }
    public double X { get; set; }
    public double Y { get; set; }
    public long LastCommand { get; private set; }

    public Player(Guid id, double x, double y)
    {
        Id = id;
        X = x;
        Y = y;
        LastCommand = -1;
    }

    public Player(Player other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        Id = other.Id;
        X = other.X;
        Y = other.Y;
        LastCommand = other.LastCommand;
    }
}
