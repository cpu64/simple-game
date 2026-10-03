using System.Numerics;

public interface IMobFactory
{
    Mob CreateSkyMob(EntityId id, Vector2 position);
    Mob CreateGroundMob(EntityId id, Vector2 position);
    Mob CreateUndergroundMob(EntityId id, Vector2 position);
}
