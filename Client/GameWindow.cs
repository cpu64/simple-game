using System;
using System.Collections.Generic;
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

    private static void RenderLocalPlayer(World world, Guid playerId)
    {
        if (!world.Players.TryGetValue(playerId, out Player player))
        {
            return;
        }

        Raylib.DrawCircle((int)Math.Round(player.X), (int)Math.Round(player.Y), 10, Color.Red);
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
        // The render tick therefore advances naturally with the
        // local simulation. No separate render cursor is required.
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
        foreach (KeyValuePair<Guid, Player> entry in before.Players)
        {
            Guid playerId = entry.Key;

            // The local player is rendered separately from the
            // predicted local world. Do not render its authoritative
            // snapshot here.
            if (playerId == localPlayerId)
            {
                continue;
            }

            if (!after.Players.TryGetValue(playerId, out Player afterPlayer))
            {
                continue;
            }

            Player beforePlayer = entry.Value;

            double x = Lerp(beforePlayer.X, afterPlayer.X, alpha);
            double y = Lerp(beforePlayer.Y, afterPlayer.Y, alpha);

            Raylib.DrawCircle((int)Math.Round(x), (int)Math.Round(y), 10, Color.Red);
        }
    }

    private static void RenderRemoteWorld(World world, Guid localPlayerId)
    {
        foreach (KeyValuePair<Guid, Player> entry in world.Players)
        {
            Guid playerId = entry.Key;

            // The local player is rendered from the predicted local world.
            if (playerId == localPlayerId)
            {
                continue;
            }

            Player player = entry.Value;

            Raylib.DrawCircle((int)Math.Round(player.X), (int)Math.Round(player.Y), 10, Color.Red);
        }
    }

    private static double Lerp(double from, double to, double amount)
    {
        return from + (to - from) * amount;
    }

    private static void UpdateInput(SharedInputState input)
    {
        if (Raylib.IsKeyDown(KeyboardKey.Left))
            input.Press(InputState.Left);
        else
            input.Release(InputState.Left);

        if (Raylib.IsKeyDown(KeyboardKey.Right))
            input.Press(InputState.Right);
        else
            input.Release(InputState.Right);

        if (Raylib.IsKeyDown(KeyboardKey.Up))
            input.Press(InputState.Up);
        else
            input.Release(InputState.Up);
    }
}
