using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PlayerCommandSystem
{
    private const float PlayerSpeed = 8.0f;

    public static void Process(World world, List<InputCommand> commands)
    {
        foreach (InputCommand command in commands)
        {
            Player? player = world.Entities.OfType<Player>().FirstOrDefault(p => p.UserId == command.PlayerId);

            if (player == null)
                continue;

            if (player.LastCommand >= command.Sequence)
                continue;

            player.LastCommand = command.Sequence;

            KeyState keys = command.Keys;

            // Horizontal movement.
            float intendedVelocityX = 0.0f;

            if ((keys & KeyState.Left) != 0)
                intendedVelocityX -= PlayerSpeed;

            if ((keys & KeyState.Right) != 0)
                intendedVelocityX += PlayerSpeed;

            if (intendedVelocityX != 0.0f)
            {
                // Only take control of horizontal velocity if it isn't
                // already faster than the player's intended movement speed.
                if (MathF.Abs(player.Velocity.X) < PlayerSpeed)
                {
                    player.Velocity = new Vector2(intendedVelocityX, player.Velocity.Y);
                }
            }
            else
            {
                // No horizontal input.
                // For now, stop horizontal movement.
                player.Velocity = new Vector2(0.0f, player.Velocity.Y);
            }

            // Vertical movement.
            if ((keys & KeyState.Up) != 0)
            {
                if (CollisionService.IsGrounded(player.Position, player.CollisionSize, world.Blocks))
                {
                    player.Velocity = new Vector2(player.Velocity.X, -PlayerSpeed);
                }
            }

            if ((keys & KeyState.MouseLeft) != 0 && command.Pointer is Vector2 pointer)
            {
                CreateBullet(world, player, pointer);
            }
        }
    }

    public static void CreateBullet(World world, Player player, Vector2 clickPosition)
    {
        Vector2 velocity = clickPosition - player.Position;

        // Ignore clicks directly on the player.
        if (velocity.LengthSquared() <= 0.0001f)
            return;

        velocity = Vector2.Normalize(velocity) * 10;

        // Entity bullet = new SimpleBullet(
        //     world.NextEntityId++,
        //     player.Position,
        //     player.Id,
        //     10,
        //     new Vector2(0.1f, 0.1f),
        //     10,
        //     player.Velocity + velocity,
        //     new Vector2(0.1f, 0.1f),
        //     300
        // );

        Entity bullet = new HeavyBullet(
            world.NextEntityId++,
            player.Position,
            player.Id,
            50,
            new Vector2(0.1f, 0.1f),
            10,
            player.Velocity + velocity,
            new Vector2(0.1f, 0.1f),
            8.0f,
            120
        );

        world.Entities.Add(bullet);
    }
}
