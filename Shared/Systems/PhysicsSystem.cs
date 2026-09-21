using System;
using System.Collections.Generic;
using System.Numerics;

public static class PhysicsSystem
{
    public static Vector2 Move(Vector2 position, float width, float height, Vector2 movement, List<Block> blocks)
    {
        position = MoveHorizontal(position, width, height, movement.X, blocks);
        position = MoveVertical(position, width, height, movement.Y, blocks);
        return position;
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

    public static bool CollidesWithBlock(float x, float y, float width, float height, List<Block> blocks)
    {
        float playerLeft = x;
        float playerRight = x + width;
        float playerTop = y;
        float playerBottom = y + height;

        foreach (Block block in blocks)
        {
            float blockLeft = block.Position.X;
            float blockRight = block.Position.X + 1.0f;
            float blockTop = block.Position.Y;
            float blockBottom = block.Position.Y + 1.0f;

            if (playerRight > blockLeft && playerLeft < blockRight && playerBottom > blockTop && playerTop < blockBottom)
            {
                return true;
            }
        }

        return false;
    }

    private static Vector2 MoveHorizontal(Vector2 position, float width, float height, float amount, List<Block> blocks)
    {
        if (amount == 0)
            return position;

        float newX = position.X + amount;

        if (!CollidesWithBlock(newX, position.Y, width, height, blocks))
            return new Vector2(newX, position.Y);

        float correctedX = newX;

        if (amount > 0)
        {
            // Moving right: push left to the nearest blocking face.
            float right = newX + width;

            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(position.Y, position.Y + height, block.Position.Y, block.Position.Y + 1.0f))
                    continue;

                float blockLeft = block.Position.X;

                if (right > blockLeft && position.X + width <= blockLeft)
                    correctedX = Math.Min(correctedX, blockLeft - width);
            }
        }
        else
        {
            // Moving left: push right to the nearest blocking face.
            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(position.Y, position.Y + height, block.Position.Y, block.Position.Y + 1.0f))
                    continue;

                float blockRight = block.Position.X + 1.0f;

                if (newX < blockRight && position.X >= blockRight)
                    correctedX = Math.Max(correctedX, blockRight);
            }
        }

        return new Vector2(correctedX, position.Y);
    }

    private static Vector2 MoveVertical(Vector2 position, float width, float height, float amount, List<Block> blocks)
    {
        if (amount == 0)
            return position;

        float newY = position.Y + amount;

        if (!CollidesWithBlock(position.X, newY, width, height, blocks))
            return new Vector2(position.X, newY);

        float correctedY = newY;

        if (amount > 0)
        {
            // Moving down: land on top of the block.
            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(position.X, position.X + width, block.Position.X, block.Position.X + 1.0f))
                    continue;

                float blockTop = block.Position.Y;

                if (newY + height > blockTop && position.Y + height <= blockTop)
                    correctedY = Math.Min(correctedY, blockTop - height);
            }
        }
        else
        {
            // Moving up: hit the underside of the block.
            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(position.X, position.X + width, block.Position.X, block.Position.X + 1.0f))
                    continue;

                float blockBottom = block.Position.Y + 1.0f;

                if (newY < blockBottom && position.Y >= blockBottom)
                    correctedY = Math.Max(correctedY, blockBottom);
            }
        }

        return new Vector2(position.X, correctedY);
    }

    private static bool OverlapsHorizontally(float left1, float right1, float left2, float right2) => right1 > left2 && left1 < right2;

    private static bool OverlapsVertically(float top1, float bottom1, float top2, float bottom2) => bottom1 > top2 && top1 < bottom2;
}
