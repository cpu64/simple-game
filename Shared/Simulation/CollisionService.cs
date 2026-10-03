using System.Collections.Generic;
using System.Numerics;

public static class CollisionService
{
    public static bool CollidesWithBlock(Vector2 position, Vector2 size, List<Block> blocks)
    {
        Vector2 halfSize = size * 0.5f;

        Vector2 entityMin = position - halfSize;
        Vector2 entityMax = position + halfSize;

        foreach (Block block in blocks)
        {
            Vector2 blockMin = block.Position;
            Vector2 blockMax = block.Position + Vector2.One;

            if (entityMax.X > blockMin.X && entityMin.X < blockMax.X && entityMax.Y > blockMin.Y && entityMin.Y < blockMax.Y)
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsGrounded(Vector2 position, Vector2 size, List<Block> blocks)
    {
        Vector2 groundCheckPosition = position + new Vector2(0.0f, 0.001f);
        return CollidesWithBlock(groundCheckPosition, size, blocks);
    }

    public static bool OverlapsHorizontally(Vector2 bounds1Min, Vector2 bounds1Max, Vector2 bounds2Min, Vector2 bounds2Max)
    {
        return bounds1Max.X > bounds2Min.X && bounds1Min.X < bounds2Max.X;
    }

    public static bool OverlapsVertically(Vector2 bounds1Min, Vector2 bounds1Max, Vector2 bounds2Min, Vector2 bounds2Max)
    {
        return bounds1Max.Y > bounds2Min.Y && bounds1Min.Y < bounds2Max.Y;
    }
}
