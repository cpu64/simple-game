using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Raylib_cs;
using Client.UI.Rendering;
using Client.UI.Widgets;

public static class GameWindow
{
    private const float PixelsPerUnit = 20.0f;

    public static void Run(LocalServer server, SharedInputState input, RenderWorldUpdater renderWorldUpdater, Guid playerId)
    {
        Raylib.InitWindow(800, 600, "Terraria Prototype");

        Raylib.SetTargetFPS(GameConstants.TargetFrameRate);

        var spriteRenderer = new SpriteRenderer();
        var primitiveRenderer = new PrimitiveRenderer();
        IRenderer currentRenderer = primitiveRenderer;

        var healthBar = new PlayerHealthBar(
            new Rectangle(16, 16, 220, 26),
            currentRenderer,
            () => server.GetLocalPlayer(playerId)
        );

        var itemSlots = new ItemSlot[4]
        {
            new ItemSlot(new Rectangle(292, 536, 48, 48), currentRenderer, 1, "Pickaxe", "Pickaxe") { IsSelected = true },
            new ItemSlot(new Rectangle(348, 536, 48, 48), currentRenderer, 2, "Sword", "Sword"),
            new ItemSlot(new Rectangle(404, 536, 48, 48), currentRenderer, 3, "Platforms", "Platforms"),
            new ItemSlot(new Rectangle(460, 536, 48, 48), currentRenderer, 4, "Ropes", "Ropes")
        };

        void SelectSlot(int index)
        {
            for (int i = 0; i < itemSlots.Length; i++)
            {
                itemSlots[i].IsSelected = (i == index);
            }
        }

        try
        {
            RenderWorld world = new RenderWorld();

            while (!Raylib.WindowShouldClose())
            {
                using (Logger.Instance.Info("Drawing", LogCategory.Rendering).Every("render", GameConstants.TargetFrameRate).Time())
                {
                    UpdateInput(input);

                    if (Raylib.IsKeyPressed(KeyboardKey.F1))
                    {
                        currentRenderer = (currentRenderer == primitiveRenderer) ? spriteRenderer : primitiveRenderer;
                        healthBar.SetRenderer(currentRenderer);
                        for (int i = 0; i < itemSlots.Length; i++)
                        {
                            itemSlots[i].SetRenderer(currentRenderer);
                        }
                    }

                    if (Raylib.IsKeyPressed(KeyboardKey.One)) SelectSlot(0);
                    else if (Raylib.IsKeyPressed(KeyboardKey.Two)) SelectSlot(1);
                    else if (Raylib.IsKeyPressed(KeyboardKey.Three)) SelectSlot(2);
                    else if (Raylib.IsKeyPressed(KeyboardKey.Four)) SelectSlot(3);

                    world = renderWorldUpdater.UpdateRenderWorld(world);

                    Raylib.BeginDrawing();

                    Raylib.ClearBackground(Color.Black);

                    RenderWorld(world);

                    foreach (RenderEntity entity in world.Entities.Values)
                    {
                        entity.Render(world.Alpha);
                    }
                    float alphaStep = 1.0f / (GameConstants.TargetFrameRate / GameConstants.SimulationTickRate);
                    world.Alpha += alphaStep;

                    healthBar.Draw();
                    for (int i = 0; i < itemSlots.Length; i++)
                    {
                        itemSlots[i].Draw();
                    }
                }

                Raylib.EndDrawing();
            }
        }
        finally
        {
            spriteRenderer.Dispose();
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
