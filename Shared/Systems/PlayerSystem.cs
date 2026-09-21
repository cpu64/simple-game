using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PlayerSystem
{
    private const float Speed = 8.0f;
    private const float Gravity = 10.0f;
    private const float Width = 1.0f;
    private const float Height = 1.0f;

    public static void ProcessCommands(World world, List<InputCommand> commands, bool isReplay = false)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        foreach (InputCommand command in commands)
        {
            Player? player = world.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == command.PlayerId);

            if (player == null || player.LastCommand >= command.Sequence)
                continue;

            player.LastCommand = command.Sequence;

            player.Position = PhysicsSystem.Move(player.Position, Width, Height, ComputeMovement(command.Keys, dt), world.Blocks);

            if (!isReplay && (command.Keys & KeyState.MouseLeft) != 0)
                ProjectileSystem.SpawnSimpleBullet(world, player, command.Pointer);
        }
    }

    private static Vector2 ComputeMovement(KeyState keys, float dt)
    {
        float moveX = 0f;
        float moveY = (keys & KeyState.Up) != 0 ? -Speed * dt : Gravity * dt;

        if ((keys & KeyState.Left) != 0)
            moveX -= Speed * dt;
        if ((keys & KeyState.Right) != 0)
            moveX += Speed * dt;

        return new Vector2(moveX, moveY);
    }
}
