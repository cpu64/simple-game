using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using Raylib_cs;

public sealed class RenderWorldUpdater
{
    private RenderInput? _latest;

    // Called by the simulation thread.
    public void UpdateData(
        World oldWorld,
        World newWorld,
        IReadOnlyDictionary<Guid, Vector2> previousRemotePlayerPositions,
        IReadOnlyDictionary<Guid, Vector2> currentRemotePlayerPositions
    )
    {
        var snapshot = new RenderInput(oldWorld, newWorld, previousRemotePlayerPositions, currentRemotePlayerPositions);

        Volatile.Write(ref _latest, snapshot);
    }

    // Called by the render thread.
    public RenderWorld UpdateRenderWorld(RenderWorld renderWorld)
    {
        RenderInput? snapshot = Volatile.Read(ref _latest);

        if (snapshot == null)
            return renderWorld;

        if (renderWorld.Tick >= snapshot.New.Tick)
            return renderWorld;

        World oldWorld = snapshot.Old;
        World newWorld = snapshot.New;
        IReadOnlyDictionary<Guid, Vector2> previousRemotePlayerPositions = snapshot.PreviousRemotePlayerPositions;
        IReadOnlyDictionary<Guid, Vector2> currentRemotePlayerPositions = snapshot.CurrentRemotePlayerPositions;

        renderWorld.Tick = newWorld.Tick;
        renderWorld.Alpha = 0;

        // Copy the block list so the render world owns its collection.
        renderWorld.Blocks = newWorld.Blocks;

        // Index the previous simulation state by entity ID.
        var oldEntities = oldWorld.Entities.ToDictionary(entity => entity.Id);

        // Track entities that should remain in the render world.
        var activeEntityIds = new HashSet<EntityId>();

        foreach (Entity entity in newWorld.Entities)
        {
            EntityId id = entity.Id;
            activeEntityIds.Add(id);

            Vector2 currentPosition = entity.Position;

            // Default to simulation positions.
            Vector2 previousPosition = oldEntities.TryGetValue(id, out Entity? oldEntity) ? oldEntity.Position : currentPosition;

            // Override positions for remote players.
            if (
                entity is Player player
                && previousRemotePlayerPositions.TryGetValue(player.UserId, out Vector2 previousRemotePlayerPosition)
                && currentRemotePlayerPositions.TryGetValue(player.UserId, out Vector2 currentRemotePlayerPosition)
            )
            {
                previousPosition = previousRemotePlayerPosition;
                currentPosition = currentRemotePlayerPosition;
            }

            Vector2 size = entity switch
            {
                ICollidable collidable => collidable.CollisionSize * 20,
                IDamaging damaging => damaging.DamageBox * 20,
                IHealth health => health.HitBox * 20,
                _ => Vector2.Zero,
            };

            Color color = entity switch
            {
                Harpy => new Color(220, 80, 80, 230),
                Slime => new Color(50, 150, 255, 230),
                Spider => new Color(130, 60, 40, 230),
                Eagle => new Color(220, 180, 60, 230),
                Cow => new Color(160, 100, 70, 230),
                Bat => new Color(100, 70, 150, 230),
                _ => new Color(200, 200, 200, 230),
            };

            if (renderWorld.Entities.TryGetValue(id, out RenderEntity? renderEntity) && renderEntity is RenderRectangle)
            {
                // Update the interpolation endpoints.
                renderEntity.PreviousPosition = previousPosition;
                renderEntity.CurrentPosition = currentPosition;
            }
            else
            {
                // Create a renderer for a new entity.
                renderWorld.Entities[id] = new RenderRectangle(previousPosition, currentPosition, size, color);
            }
        }
        // Remove entities that no longer exist in the new world.
        foreach (EntityId id in renderWorld.Entities.Keys.ToList())
        {
            if (!activeEntityIds.Contains(id))
                renderWorld.Entities.Remove(id);
        }

        return renderWorld;
    }
}
