using System.Collections.Generic;
using System.Numerics;

public static class MovementSystem
{
    public static void Process(World world)
    {
        float deltaTime = (float)GameConstants.SimulationTickDuration;

        List<Entity> entitiesToRemove = new List<Entity>();

        foreach (Entity entity in world.Entities)
        {
            if (entity is not IMoving moving)
                continue;

            if (entity is IGravityAffected gravityAffected)
            {
                moving.Velocity += new Vector2(0.0f, gravityAffected.GravitationalAcceleration * deltaTime);
            }

            Vector2 movement = moving.Velocity * deltaTime;

            if (entity is ICollidable collidable)
            {
                bool collided = MoveCollidable(entity, moving, collidable, world.Blocks, movement);

                if (collided && entity is Bullet)
                {
                    entitiesToRemove.Add(entity);
                }
            }
            else
            {
                entity.Position += movement;
            }
        }

        foreach (Entity entity in entitiesToRemove)
        {
            world.Entities.Remove(entity);
        }
    }

    private static bool MoveCollidable(Entity entity, IMoving moving, ICollidable collidable, List<Block> blocks, Vector2 movement)
    {
        bool horizontalCollision = MoveHorizontal(entity, moving, collidable, blocks, movement.X);

        bool verticalCollision = MoveVertical(entity, moving, collidable, blocks, movement.Y);

        return horizontalCollision || verticalCollision;
    }

    private static bool MoveHorizontal(Entity entity, IMoving moving, ICollidable collidable, List<Block> blocks, float amount)
    {
        if (amount == 0)
            return false;

        Vector2 newPosition = entity.Position + new Vector2(amount, 0.0f);

        if (!CollisionService.CollidesWithBlock(newPosition, collidable.CollisionSize, blocks))
        {
            entity.Position = newPosition;
            return false;
        }

        Vector2 halfSize = collidable.CollisionSize * 0.5f;

        if (amount > 0)
        {
            // Moving right.
            float right = newPosition.X + halfSize.X;
            float correctedX = newPosition.X;

            foreach (Block block in blocks)
            {
                Vector2 blockMin = block.Position;
                Vector2 blockMax = block.Position + Vector2.One;

                Vector2 entityMin = new Vector2(entity.Position.X - halfSize.X, entity.Position.Y - halfSize.Y);

                Vector2 entityMax = new Vector2(right, entity.Position.Y + halfSize.Y);

                if (!CollisionService.OverlapsVertically(entityMin, entityMax, blockMin, blockMax))
                {
                    continue;
                }

                float blockLeft = block.Position.X;

                if (right > blockLeft && entity.Position.X + halfSize.X <= blockLeft)
                {
                    correctedX = System.Math.Min(correctedX, blockLeft - halfSize.X);
                }
            }

            entity.Position = new Vector2(correctedX, entity.Position.Y);
        }
        else
        {
            // Moving left.
            float left = newPosition.X - halfSize.X;
            float correctedX = newPosition.X;

            foreach (Block block in blocks)
            {
                Vector2 blockMin = block.Position;
                Vector2 blockMax = block.Position + Vector2.One;

                Vector2 entityMin = new Vector2(left, entity.Position.Y - halfSize.Y);

                Vector2 entityMax = new Vector2(entity.Position.X + halfSize.X, entity.Position.Y + halfSize.Y);

                if (!CollisionService.OverlapsVertically(entityMin, entityMax, blockMin, blockMax))
                {
                    continue;
                }

                float blockRight = block.Position.X + 1.0f;

                if (left < blockRight && entity.Position.X - halfSize.X >= blockRight)
                {
                    correctedX = System.Math.Max(correctedX, blockRight + halfSize.X);
                }
            }

            entity.Position = new Vector2(correctedX, entity.Position.Y);
        }

        // Horizontal movement was blocked.
        moving.Velocity = new Vector2(0.0f, moving.Velocity.Y);

        return true;
    }

    private static bool MoveVertical(Entity entity, IMoving moving, ICollidable collidable, List<Block> blocks, float amount)
    {
        if (amount == 0)
            return false;

        Vector2 newPosition = entity.Position + new Vector2(0.0f, amount);

        if (!CollisionService.CollidesWithBlock(newPosition, collidable.CollisionSize, blocks))
        {
            entity.Position = newPosition;
            return false;
        }

        Vector2 halfSize = collidable.CollisionSize * 0.5f;

        if (amount > 0)
        {
            // Moving down: stand on top of the block.
            float bottom = newPosition.Y + halfSize.Y;
            float correctedY = newPosition.Y;

            foreach (Block block in blocks)
            {
                Vector2 blockMin = block.Position;
                Vector2 blockMax = block.Position + Vector2.One;

                Vector2 entityMin = new Vector2(entity.Position.X - halfSize.X, entity.Position.Y - halfSize.Y);

                Vector2 entityMax = new Vector2(entity.Position.X + halfSize.X, bottom);

                if (!CollisionService.OverlapsHorizontally(entityMin, entityMax, blockMin, blockMax))
                {
                    continue;
                }

                float blockTop = block.Position.Y;

                if (bottom > blockTop && entity.Position.Y + halfSize.Y <= blockTop)
                {
                    correctedY = System.Math.Min(correctedY, blockTop - halfSize.Y);
                }
            }

            entity.Position = new Vector2(entity.Position.X, correctedY);
        }
        else
        {
            // Moving up: hit the underside of the block.
            float top = newPosition.Y - halfSize.Y;
            float correctedY = newPosition.Y;

            foreach (Block block in blocks)
            {
                Vector2 blockMin = block.Position;
                Vector2 blockMax = block.Position + Vector2.One;

                Vector2 entityMin = new Vector2(entity.Position.X - halfSize.X, top);

                Vector2 entityMax = new Vector2(entity.Position.X + halfSize.X, entity.Position.Y + halfSize.Y);

                if (!CollisionService.OverlapsHorizontally(entityMin, entityMax, blockMin, blockMax))
                {
                    continue;
                }

                float blockBottom = block.Position.Y + 1.0f;

                if (top < blockBottom && entity.Position.Y - halfSize.Y >= blockBottom)
                {
                    correctedY = System.Math.Max(correctedY, blockBottom + halfSize.Y);
                }
            }

            entity.Position = new Vector2(entity.Position.X, correctedY);
        }

        // Vertical movement was blocked.
        moving.Velocity = new Vector2(moving.Velocity.X, 0.0f);

        return true;
    }
}
