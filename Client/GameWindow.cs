using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

public static class GameWindow
{
    public static void Run(LocalServer server, SharedInputState input, Guid playerId)
    {
        Raylib.InitWindow(800, 600, "Terraria Prototype");

        Raylib.SetTargetFPS(GameConstants.TargetFrameRate);

        try
        {
            while (!Raylib.WindowShouldClose())
            {
                UpdateInput(input);

                RenderInput renderInput = server.GetRenderInput();

                Raylib.BeginDrawing();

                Raylib.ClearBackground(Color.Black);

                RenderWorld(renderInput.World);

                RenderBullets(renderInput.World);

                RenderEnemies(renderInput.World);

                RenderLocalPlayer(renderInput.World, playerId);

                RenderRemotePlayers(renderInput.AuthoritativeSnapshots, renderInput.World.Tick, playerId);

                Raylib.EndDrawing();
            }
        }
        finally
        {
            server.Stop();
            Raylib.CloseWindow();
        }
    }

    private static void RenderBullets(World world)
    {
        foreach (Bullet bullet in world.Entities.OfType<Bullet>())
        {
            Color color = bullet is HeavyBullet ? Color.DarkGreen : Color.Green;
            int width = (int)Math.Max(3, Math.Round(bullet.Size.X * 20.0));
            int height = (int)Math.Max(3, Math.Round(bullet.Size.Y * 20.0));
            Raylib.DrawRectangle((int)Math.Round(bullet.Position.X * 20.0), (int)Math.Round(bullet.Position.Y * 20.0), width, height, color);
        }
    }

    private static void RenderEnemies(World world)
    {
        foreach (Enemy enemy in world.Entities.OfType<Enemy>())
        {
            if (enemy.IsDead)
                continue;

            int screenX = (int)Math.Round(enemy.Position.X * 20.0);
            int screenY = (int)Math.Round(enemy.Position.Y * 20.0);
            int width = (int)Math.Round(enemy.Size.X * 20.0);
            int height = (int)Math.Round(enemy.Size.Y * 20.0);

            Color bodyColor = new Color(50, 150, 255, 230);
            Raylib.DrawRectangle(screenX, screenY, width, height, bodyColor);
            Raylib.DrawRectangleLines(screenX, screenY, width, height, Color.DarkBlue);

            if (enemy.Health < enemy.MaxHealth && enemy.Health > 0)
            {
                int barWidth = width;
                int barHeight = 4;
                int barY = screenY - 6;

                Raylib.DrawRectangle(screenX, barY, barWidth, barHeight, Color.Red);
                int healthWidth = (int)Math.Round(barWidth * ((double)enemy.Health / enemy.MaxHealth));
                Raylib.DrawRectangle(screenX, barY, healthWidth, barHeight, Color.Green);
            }
        }
    }

    private static void RenderWorld(World world)
    {
        foreach (Block block in world.Blocks)
        {
            Raylib.DrawRectangle((int)Math.Round(block.Position.X * 20.0), (int)Math.Round(block.Position.Y * 20.0), 20, 20, Color.Green);
        }
    }

    private static void RenderLocalPlayer(World world, Guid playerId)
    {
        Player? player = world.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == playerId);

        if (player == null || player.IsDead)
            return;

        int screenX = (int)Math.Round(player.Position.X * 20.0);
        int screenY = (int)Math.Round(player.Position.Y * 20.0);

        Color playerColor = player.IsInvulnerable && ((int)(Raylib.GetTime() * 12) % 2 == 0) ? new Color(255, 120, 120, 150) : Color.Red;

        Raylib.DrawRectangle(screenX, screenY, 20, 20, playerColor);

        if (player.Health < player.MaxHealth)
        {
            int barWidth = 20;
            int barHeight = 4;
            int barY = screenY - 6;

            Raylib.DrawRectangle(screenX, barY, barWidth, barHeight, Color.Maroon);
            int healthWidth = (int)Math.Round(barWidth * ((double)player.Health / player.MaxHealth));
            Raylib.DrawRectangle(screenX, barY, healthWidth, barHeight, Color.Green);
        }
    }

    private static void RenderRemotePlayers(IReadOnlyList<World> snapshots, long worldTick, Guid localPlayerId)
    {
        if (snapshots.Count == 0)
            return;

        // The local simulated world is the render clock.
        //
        // With:
        //
        //     SnapshotIntervalTicks = 10
        //     InterpolationBufferTicks = 5
        //
        // InterpolationDelayTicks is 15.
        //
        // If the local world is tick 1010:
        //
        //     renderTick = 1010 - 15 = 995
        //
        // The render tick therefore advances naturally with the local simulation.
        long renderTick = worldTick - GameConstants.InterpolationDelayTicks;

        FindSnapshots(snapshots, renderTick, out World before, out World after);

        if (before.Tick == after.Tick)
        {
            RenderRemoteWorld(before, localPlayerId);
            return;
        }

        double alpha = (renderTick - before.Tick) / (double)(after.Tick - before.Tick);

        alpha = Math.Clamp(alpha, 0.0, 1.0);

        RenderInterpolatedRemotePlayers(before, after, alpha, localPlayerId);
    }

    private static void FindSnapshots(IReadOnlyList<World> snapshots, long renderTick, out World before, out World after)
    {
        // RenderInput guarantees that snapshots are sorted by Tick.

        if (renderTick <= snapshots[0].Tick)
        {
            before = snapshots[0];
            after = snapshots[0];
            return;
        }

        for (int i = 0; i < snapshots.Count - 1; i++)
        {
            World current = snapshots[i];
            World next = snapshots[i + 1];

            if (current.Tick <= renderTick && renderTick <= next.Tick)
            {
                before = current;
                after = next;
                return;
            }
        }

        // There is no later authoritative snapshot yet.
        //
        // Do not extrapolate. Render the newest authoritative state
        // until another snapshot arrives.
        World latest = snapshots[snapshots.Count - 1];

        before = latest;
        after = latest;
    }

    private static void RenderInterpolatedRemotePlayers(World before, World after, double alpha, Guid localPlayerId)
    {
        foreach (Player beforePlayer in before.Entities.OfType<Player>())
        {
            // The local player is rendered from the predicted local world.
            if (beforePlayer.UserId == localPlayerId)
                continue;

            Player? afterPlayer = after.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == beforePlayer.UserId);

            if (afterPlayer == null)
                continue;

            Vector2 position = Vector2.Lerp(beforePlayer.Position, afterPlayer.Position, (float)alpha);

            Raylib.DrawRectangle((int)Math.Round(position.X * 20.0), (int)Math.Round(position.Y * 20.0), 20, 20, Color.Red);
        }
    }

    private static void RenderRemoteWorld(World world, Guid localPlayerId)
    {
        foreach (Player player in world.Entities.OfType<Player>())
        {
            // The local player is rendered from the predicted local world.
            if (player.UserId == localPlayerId)
                continue;

            Raylib.DrawRectangle((int)Math.Round(player.Position.X * 20.0), (int)Math.Round(player.Position.Y * 20.0), 20, 20, Color.Red);
        }
    }

    private static void UpdateInput(SharedInputState input)
    {
        KeyState keys = KeyState.None;

        if (Raylib.IsKeyDown(KeyboardKey.Left))
            keys |= KeyState.Left;

        if (Raylib.IsKeyDown(KeyboardKey.Right))
            keys |= KeyState.Right;

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            keys |= KeyState.Up;

        if (Raylib.IsMouseButtonDown(MouseButton.Left))
            keys |= KeyState.MouseLeft;

        Vector2 mouseScreen = Raylib.GetMousePosition();
        Vector2 pointer = mouseScreen / 20f;

        input.Write(keys, pointer);
    }
}
