using System.Numerics;

public abstract class Entity
{
    public EntityId Id { get; }
    public Vector2 Position { get; set; }

    protected Entity(EntityId id, Vector2 position)
    {
        Id = id;
        Position = position;
    }

    public virtual Entity Copy()
    {
        return (Entity)MemberwiseClone();
    }

    public override string ToString()
    {
        return $"{GetType().Name}: Id={Id}, Position={Position}";
    }
}
