using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Simulation
{
    private const double PlayerSpeed = 8.0;
    private const double Gravity = 10.0;

    private const double PlayerWidth = 1.0;
    private const double PlayerHeight = 1.0;

    public static World Tick(World world, List<InputCommand> commands)
    {
        using var timer = Logger.Instance.Time("Simulation", LogCategory.Simulation).Every(GameConstants.SimulationTickRate);

        foreach (InputCommand command in commands)
        {
            Player? player = world.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == command.PlayerId);

            if (player == null)
                continue;

            if (player.LastCommand >= command.Sequence)
                continue;

            player.LastCommand = command.Sequence;

            KeyState keys = command.Keys;

            double deltaTime = GameConstants.SimulationTickDuration;

            double moveX = 0.0;
            double moveY = 0.0;

            if ((keys & KeyState.Left) != 0)
                moveX -= PlayerSpeed * deltaTime;

            if ((keys & KeyState.Right) != 0)
                moveX += PlayerSpeed * deltaTime;

            if ((keys & KeyState.Up) != 0)
                moveY -= PlayerSpeed * deltaTime;
            else
                moveY += Gravity * deltaTime;

            MovePlayer(player, world.Blocks, new Vector2((float)moveX, (float)moveY));

            if ((keys & KeyState.MouseLeft) != 0 && command.Pointer is Vector2 pointer)
            {
                CreateBullet(world, player, pointer);
            }
        }

        UpdateBullets(world);

        world.Tick++;

        return world;
    }

    private static void CreateBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 direction = clickPosition - player.Position;

        // Ignore clicks directly on the player.
        if (direction.LengthSquared() <= 0.0001f)
            return;

        direction = Vector2.Normalize(direction);

        Entity bullet = new SimpleBullet(world.NextEntityId++, player.Position, player.Id, direction, 300);

        world.Entities.Add(bullet);
    }

    private static void UpdateBullets(World world)
    {
        if (world.Entities == null)
            return;

        float deltaTime = (float)GameConstants.SimulationTickDuration;

        for (int i = world.Entities.Count - 1; i >= 0; i--)
        {
            if (world.Entities[i] is not SimpleBullet bullet)
                continue;

            bullet.Position += bullet.Velocity * deltaTime;

            // Consume one tick of lifespan.
            bullet.TicksLeft--;

            // Remove when the remaining lifespan is exhausted.
            if (bullet.TicksLeft <= 0)
            {
                world.Entities.RemoveAt(i);
            }
        }
    }

    private static void MovePlayer(Player player, List<Block> blocks, Vector2 movement)
    {
        MoveHorizontal(player, blocks, movement.X);
        MoveVertical(player, blocks, movement.Y);
    }

    private static void MoveHorizontal(Player player, List<Block> blocks, float amount)
    {
        if (amount == 0)
            return;

        float newX = player.Position.X + amount;

        if (!CollidesWithBlock(newX, player.Position.Y, PlayerWidth, PlayerHeight, blocks))
        {
            player.Position = new Vector2(newX, player.Position.Y);
            return;
        }

        if (amount > 0)
        {
            // Moving right.
            float right = newX + (float)PlayerWidth;
            float correctedX = newX;

            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(player.Position.Y, player.Position.Y + (float)PlayerHeight, block.Position.Y, block.Position.Y + 1.0f))
                {
                    continue;
                }

                float blockLeft = block.Position.X;

                if (right > blockLeft && player.Position.X + (float)PlayerWidth <= blockLeft)
                {
                    correctedX = Math.Min(correctedX, blockLeft - (float)PlayerWidth);
                }
            }

            player.Position = new Vector2(correctedX, player.Position.Y);
        }
        else
        {
            // Moving left.
            float correctedX = newX;

            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(player.Position.Y, player.Position.Y + (float)PlayerHeight, block.Position.Y, block.Position.Y + 1.0f))
                {
                    continue;
                }

                float blockRight = block.Position.X + 1.0f;

                if (newX < blockRight && player.Position.X >= blockRight)
                {
                    correctedX = Math.Max(correctedX, blockRight);
                }
            }

            player.Position = new Vector2(correctedX, player.Position.Y);
        }
    }

    private static void MoveVertical(Player player, List<Block> blocks, float amount)
    {
        if (amount == 0)
            return;

        float newY = player.Position.Y + amount;

        if (!CollidesWithBlock(player.Position.X, newY, PlayerWidth, PlayerHeight, blocks))
        {
            player.Position = new Vector2(player.Position.X, newY);
            return;
        }

        if (amount > 0)
        {
            // Moving down: stand on top of the block.
            float correctedY = newY;

            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(player.Position.X, player.Position.X + (float)PlayerWidth, block.Position.X, block.Position.X + 1.0f))
                {
                    continue;
                }

                float blockTop = block.Position.Y;

                if (newY + (float)PlayerHeight > blockTop && player.Position.Y + (float)PlayerHeight <= blockTop)
                {
                    correctedY = Math.Min(correctedY, blockTop - (float)PlayerHeight);
                }
            }

            player.Position = new Vector2(player.Position.X, correctedY);
        }
        else
        {
            // Moving up: hit the underside of the block.
            float correctedY = newY;

            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(player.Position.X, player.Position.X + (float)PlayerWidth, block.Position.X, block.Position.X + 1.0f))
                {
                    continue;
                }

                float blockBottom = block.Position.Y + 1.0f;

                if (newY < blockBottom && player.Position.Y >= blockBottom)
                {
                    correctedY = Math.Max(correctedY, blockBottom);
                }
            }

            player.Position = new Vector2(player.Position.X, correctedY);
        }
    }

    private static bool CollidesWithBlock(double x, double y, double width, double height, List<Block> blocks)
    {
        double playerLeft = x;
        double playerRight = x + width;
        double playerTop = y;
        double playerBottom = y + height;

        foreach (Block block in blocks)
        {
            double blockLeft = block.Position.X;
            double blockRight = block.Position.X + 1.0;
            double blockTop = block.Position.Y;
            double blockBottom = block.Position.Y + 1.0;

            if (playerRight > blockLeft && playerLeft < blockRight && playerBottom > blockTop && playerTop < blockBottom)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OverlapsHorizontally(double left1, double right1, double left2, double right2)
    {
        return right1 > left2 && left1 < right2;
    }

    private static bool OverlapsVertically(double top1, double bottom1, double top2, double bottom2)
    {
        return bottom1 > top2 && top1 < bottom2;
    }
}
