using System;
using System.Collections.Generic;

public class Simulation
{
    private const double PlayerSpeed = 8.0;
    private const double Gravity = 10.0;

    private const double PlayerWidth = 1.0;
    private const double PlayerHeight = 1.0;

    public static World Tick(World world, List<InputCommand> commands)
    {
        foreach (InputCommand command in commands)
        {
            Player player;

            if (!world.Players.TryGetValue(command.PlayerId, out player))
            {
                continue;
            }

            if (player.LastCommand >= command.Sequence)
                continue;

            player.LastCommand = command.Sequence;

            InputState input = command.Input;

            double deltaTime = GameConstants.SimulationTickDuration;

            double moveX = 0.0;
            double moveY = 0.0;

            if ((input & InputState.Left) != 0)
                moveX -= PlayerSpeed * deltaTime;

            if ((input & InputState.Right) != 0)
                moveX += PlayerSpeed * deltaTime;

            if ((input & InputState.Up) != 0)
                moveY -= PlayerSpeed * deltaTime;
            else
                moveY += Gravity * deltaTime;

            MovePlayer(player, world.Blocks, moveX, moveY);
        }

        world.Tick++;

        return world;
    }

    private static void MovePlayer(Player player, List<Block> blocks, double moveX, double moveY)
    {
        MoveHorizontal(player, blocks, moveX);
        MoveVertical(player, blocks, moveY);
    }

    private static void MoveHorizontal(Player player, List<Block> blocks, double amount)
    {
        if (amount == 0)
            return;

        double newX = player.X + amount;

        if (!CollidesWithBlock(newX, player.Y, PlayerWidth, PlayerHeight, blocks))
        {
            player.X = newX;
            return;
        }

        if (amount > 0)
        {
            double right = newX + PlayerWidth;
            double correctedX = newX;

            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(player.Y, player.Y + PlayerHeight, block.Y, block.Y + 1.0))
                {
                    continue;
                }

                double blockLeft = block.X;

                if (right > blockLeft && player.X + PlayerWidth <= blockLeft)
                {
                    correctedX = Math.Min(correctedX, blockLeft - PlayerWidth);
                }
            }

            player.X = correctedX;
        }
        else
        {
            double correctedX = newX;

            foreach (Block block in blocks)
            {
                if (!OverlapsVertically(player.Y, player.Y + PlayerHeight, block.Y, block.Y + 1.0))
                {
                    continue;
                }

                double blockRight = block.X + 1.0;

                if (newX < blockRight && player.X >= blockRight)
                {
                    correctedX = Math.Max(correctedX, blockRight);
                }
            }

            player.X = correctedX;
        }
    }

    private static void MoveVertical(Player player, List<Block> blocks, double amount)
    {
        if (amount == 0)
            return;

        double newY = player.Y + amount;

        if (!CollidesWithBlock(player.X, newY, PlayerWidth, PlayerHeight, blocks))
        {
            player.Y = newY;
            return;
        }

        if (amount > 0)
        {
            // Moving down: stand on top of the block.
            double correctedY = newY;

            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(player.X, player.X + PlayerWidth, block.X, block.X + 1.0))
                {
                    continue;
                }

                double blockTop = block.Y;

                if (newY + PlayerHeight > blockTop && player.Y + PlayerHeight <= blockTop)
                {
                    correctedY = Math.Min(correctedY, blockTop - PlayerHeight);
                }
            }

            player.Y = correctedY;
        }
        else
        {
            // Moving up: hit the underside of the block.
            double correctedY = newY;

            foreach (Block block in blocks)
            {
                if (!OverlapsHorizontally(player.X, player.X + PlayerWidth, block.X, block.X + 1.0))
                {
                    continue;
                }

                double blockBottom = block.Y + 1.0;

                if (newY < blockBottom && player.Y >= blockBottom)
                {
                    correctedY = Math.Max(correctedY, blockBottom);
                }
            }

            player.Y = correctedY;
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
            double blockLeft = block.X;
            double blockRight = block.X + 1.0;
            double blockTop = block.Y;
            double blockBottom = block.Y + 1.0;

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
