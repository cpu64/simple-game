using System;
using System.Collections.Generic;
using System.Numerics;

public readonly record struct MovementResult(Vector2 Position, bool HitHorizontal, bool HitFloor, bool HitCeiling);

public static class PhysicsSystem
{
    private const float Epsilon = 0.01f;

    public static Vector2 Move(Vector2 position, float width, float height, Vector2 movement, List<Block> blocks)
    {
        return MoveDetailed(position, width, height, movement, blocks).Position;
    }

    public static MovementResult MoveDetailed(Vector2 position, float width, float height, Vector2 movement, List<Block> blocks)
    {
        (Vector2 horizontalPos, bool hitHorizontal) = MoveHorizontalDetailed(position, width, height, movement.X, blocks);
        (Vector2 finalPos, bool hitFloor, bool hitCeiling) = MoveVerticalDetailed(horizontalPos, width, height, movement.Y, blocks);

        return new MovementResult(finalPos, hitHorizontal, hitFloor, hitCeiling);
    }

    public static bool TryMove(Vector2 position, float width, float height, Vector2 movement, List<Block> blocks, out Vector2 newPosition)
    {
        newPosition = position + movement;
        if (CollidesWithBlock(newPosition.X, newPosition.Y, width, height, blocks))
        {
            newPosition = position;
            return false;
        }
        return true;
    }

    public static bool Overlaps(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2)
    {
        return pos1.X + size1.X > pos2.X && pos1.X < pos2.X + size2.X && pos1.Y + size1.Y > pos2.Y && pos1.Y < pos2.Y + size2.Y;
    }

    public static bool CollidesWithBlock(float x, float y, float width, float height, List<Block> blocks)
    {
        Vector2 position = new Vector2(x, y);
        Vector2 size = new Vector2(width, height);

        foreach (Block block in blocks)
        {
            if (Overlaps(position, size, block.Position, Vector2.One))
            {
                return true;
            }
        }

        return false;
    }

    private static (Vector2 Position, bool Hit) MoveHorizontalDetailed(Vector2 position, float width, float height, float amount, List<Block> blocks)
    {
        if (amount == 0)
            return (position, false);

        float newX = position.X + amount;

        if (!CollidesWithBlock(newX, position.Y, width, height, blocks))
            return (new Vector2(newX, position.Y), false);

        float correctedX = newX;
        bool hit = false;

        if (amount > 0)
        {
            float right = newX + width;

            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(position.Y, position.Y + height, block.Position.Y, block.Position.Y + 1.0f))
                    continue;

                float blockLeft = block.Position.X;

                if (right > blockLeft && position.X + width <= blockLeft + Epsilon)
                {
                    correctedX = Math.Min(correctedX, blockLeft - width);
                    hit = true;
                }
            }
        }
        else
        {
            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(position.Y, position.Y + height, block.Position.Y, block.Position.Y + 1.0f))
                    continue;

                float blockRight = block.Position.X + 1.0f;

                if (newX < blockRight && position.X >= blockRight - Epsilon)
                {
                    correctedX = Math.Max(correctedX, blockRight);
                    hit = true;
                }
            }
        }

        return (new Vector2(correctedX, position.Y), hit);
    }

    private static (Vector2 Position, bool HitFloor, bool HitCeiling) MoveVerticalDetailed(
        Vector2 position,
        float width,
        float height,
        float amount,
        List<Block> blocks
    )
    {
        if (amount == 0)
            return (position, false, false);

        float newY = position.Y + amount;

        if (!CollidesWithBlock(position.X, newY, width, height, blocks))
            return (new Vector2(position.X, newY), false, false);

        float correctedY = newY;
        bool hitFloor = false;
        bool hitCeiling = false;

        if (amount > 0)
        {
            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(position.X, position.X + width, block.Position.X, block.Position.X + 1.0f))
                    continue;

                float blockTop = block.Position.Y;

                if (newY + height > blockTop && position.Y + height <= blockTop + Epsilon)
                {
                    correctedY = Math.Min(correctedY, blockTop - height);
                    hitFloor = true;
                }
            }
        }
        else
        {
            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(position.X, position.X + width, block.Position.X, block.Position.X + 1.0f))
                    continue;

                float blockBottom = block.Position.Y + 1.0f;

                if (newY < blockBottom && position.Y >= blockBottom - Epsilon)
                {
                    correctedY = Math.Max(correctedY, blockBottom);
                    hitCeiling = true;
                }
            }
        }

        return (new Vector2(position.X, correctedY), hitFloor, hitCeiling);
    }

    private static bool OverlapsHorizontally(float left1, float right1, float left2, float right2) => right1 > left2 && left1 < right2;

    private static bool OverlapsVertically(float top1, float bottom1, float top2, float bottom2) => bottom1 > top2 && top1 < bottom2;
}
