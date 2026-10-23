using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;

public static class GameWindow
{
    private const float PixelsPerUnit = 20.0f;

    public static void Run(LocalServer server, SharedInputState input, RenderWorldUpdater renderWorldUpdater, Guid playerId)
    {
        Raylib.InitWindow(800, 600, "Terraria Prototype");

        Raylib.SetTargetFPS(GameConstants.TargetFrameRate);

        try
        {
            RenderWorld world = new RenderWorld();

            while (!Raylib.WindowShouldClose())
            {
                using (Logger.Instance.Info("Drawing", LogCategory.Rendering).Every("render", GameConstants.TargetFrameRate).Time())
                {
                    UpdateInput(input);

                    world = renderWorldUpdater.UpdateRenderWorld(world);

                    Raylib.BeginDrawing();

                    bool isDay = (world.Tick % GameConstants.FullDayNightCycleTicks) < GameConstants.DayNightDurationTicks;
                    Raylib.ClearBackground(isDay ? new Color(45, 55, 70, 255) : new Color(15, 15, 25, 255));

                    RenderWorld(world);

                    foreach (RenderEntity entity in world.Entities.Values)
                    {
                        entity.Render(world.Alpha);
                    }
                    float alphaStep = 1.0f / (GameConstants.TargetFrameRate / GameConstants.SimulationTickRate);
                    world.Alpha += alphaStep;
                }

                Raylib.EndDrawing();
            }
        }
        finally
        {
            server.Stop();
            Raylib.CloseWindow();
        }
    }

    private static void RenderWorld(RenderWorld world)
    {
        foreach (Block block in world.Blocks)
        {
            Raylib.DrawRectangle(
                (int)Math.Round(block.Position.X * PixelsPerUnit),
                (int)Math.Round(block.Position.Y * PixelsPerUnit),
                (int)Math.Round(PixelsPerUnit),
                (int)Math.Round(PixelsPerUnit),
                Color.Green
            );
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
        Vector2 pointer = mouseScreen / PixelsPerUnit;

        input.Write(keys, pointer);
    }
}
