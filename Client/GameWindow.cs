using System;
using Raylib_cs;

public static class GameWindow
{
    public static void Run(
        LocalServer server,
        SharedInputState input)
    {
        Raylib.InitWindow(
            800,
            600,
            "Terraria Prototype");

        Raylib.SetTargetFPS(
            GameConstants.TargetFrameRate);

        try
        {
            RenderWorldSnapshot snapshot =
            server.GetRenderWorldSnapshot();

            while (!Raylib.WindowShouldClose())
            {
                UpdateInput(input);

                snapshot =
                server.GetRenderWorldSnapshot();

                Raylib.BeginDrawing();

                Raylib.ClearBackground(
                    Color.Black);

                foreach (Player player in
                    snapshot.Players.Values)
                {
                    Raylib.DrawCircle(
                        (int)player.X,
                                      (int)player.Y,
                                      10,
                                      Color.Red);
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

    private static void UpdateInput(
        SharedInputState input)
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
