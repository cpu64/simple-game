using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public static class PlayerSystem
{
    public const float Speed = 8.0f;
    private const float JumpSpeed = 7.5f;
    private const float GroundDecel = 50.0f;
    private const float AirAccel = 35.0f;
    private const float AirCounterAccel = 50.0f;
    private const float AirDecel = 6.0f;
    private const float SimpleBulletCooldown = 0.2f;
    private const float HeavyBulletCooldown = 0.35f;

    public static void ProcessCommands(World world, List<InputCommand> commands)
    {
        float dt = (float)GameConstants.SimulationTickDuration;

        // Decrement timers for all players each tick independently of input commands
        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Player p)
                continue;

            if (p.InvulnerabilityTimer > 0)
            {
                p.InvulnerabilityTimer = Math.Max(0, p.InvulnerabilityTimer - dt);
            }

            if (p.AttackCooldownTimer > 0)
            {
                p.AttackCooldownTimer = Math.Max(0, p.AttackCooldownTimer - dt);
            }
        }

        for (int i = 0; i < world.Entities.Count; i++)
        {
            if (world.Entities[i] is not Player player)
                continue;

            int nextIndex = -1;
            long nextSequence = long.MaxValue;

            for (int c = 0; c < commands.Count; c++)
            {
                InputCommand cmd = commands[c];
                if (cmd.PlayerId == player.UserId && cmd.Sequence > player.LastCommand && cmd.Sequence < nextSequence)
                {
                    nextSequence = cmd.Sequence;
                    nextIndex = c;
                }
            }

            if (nextIndex != -1)
            {
                InputCommand command = commands[nextIndex];
                player.LastCommand = command.Sequence;

                if (player.IsDead)
                {
                    PhysicsSystem.StepBody(player, world.Blocks, dt);
                    continue;
                }

                bool isGrounded = player.Velocity.Y >= 0 && PhysicsSystem.IsGrounded(player.Position, player.Size, world.Blocks);

                float inputX = 0f;
                if ((command.Keys & KeyState.Left) != 0)
                    inputX -= 1f;
                if ((command.Keys & KeyState.Right) != 0)
                    inputX += 1f;

                if ((command.Keys & KeyState.Up) != 0 && isGrounded)
                {
                    player.Velocity = new Vector2(player.Velocity.X, -JumpSpeed);
                    isGrounded = false;
                }

                float velX = player.Velocity.X;

                if (inputX != 0)
                {
                    float targetVelX = inputX * Speed;

                    if (isGrounded)
                    {
                        if (Math.Sign(velX) == Math.Sign(inputX) || Math.Abs(velX) < 0.1f)
                        {
                            if (Math.Abs(velX) > Speed)
                            {
                                velX = Math.Sign(velX) * Math.Max(Speed, Math.Abs(velX) - GroundDecel * dt);
                            }
                            else
                            {
                                velX = targetVelX;
                            }
                        }
                        else
                        {
                            velX += inputX * GroundDecel * dt;
                        }
                    }
                    else
                    {
                        if (Math.Sign(velX) == Math.Sign(inputX))
                        {
                            if (Math.Abs(velX) > Speed)
                            {
                                velX = Math.Sign(velX) * Math.Max(Speed, Math.Abs(velX) - AirDecel * dt);
                            }
                            else
                            {
                                velX = Math.Clamp(velX + inputX * AirAccel * dt, -Speed, Speed);
                            }
                        }
                        else
                        {
                            velX += inputX * AirCounterAccel * dt;
                        }
                    }
                }
                else
                {
                    if (isGrounded)
                    {
                        if (Math.Abs(velX) <= GroundDecel * dt)
                            velX = 0f;
                        else
                            velX -= Math.Sign(velX) * GroundDecel * dt;
                    }
                    else if (Math.Abs(velX) > Speed)
                    {
                        velX = Math.Sign(velX) * Math.Max(Speed, Math.Abs(velX) - AirDecel * dt);
                    }
                }

                player.Velocity = new Vector2(velX, player.Velocity.Y);

                PhysicsSystem.StepBody(player, world.Blocks, dt);

                ProcessWeaponFiring(world, player, command);
            }
            else
            {
                bool isGrounded = player.Velocity.Y >= 0 && PhysicsSystem.IsGrounded(player.Position, player.Size, world.Blocks);
                if (isGrounded)
                {
                    float velX = player.Velocity.X;
                    if (Math.Abs(velX) <= GroundDecel * dt)
                        velX = 0f;
                    else
                        velX -= Math.Sign(velX) * GroundDecel * dt;
                    player.Velocity = new Vector2(velX, player.Velocity.Y);
                }

                PhysicsSystem.StepBody(player, world.Blocks, dt);
            }
        }
    }

    public static void ProcessWeaponFiring(World world, Player player, InputCommand command)
    {
        if (player.AttackCooldownTimer <= 0)
        {
            if ((command.Keys & KeyState.MouseLeft) != 0)
            {
                ProjectileSystem.SpawnSimpleBullet(world, player, command.Pointer);
                player.AttackCooldownTimer = SimpleBulletCooldown;
            }
            else if ((command.Keys & KeyState.MouseRight) != 0)
            {
                ProjectileSystem.SpawnHeavyBullet(world, player, command.Pointer);
                player.AttackCooldownTimer = HeavyBulletCooldown;
            }
        }
    }
}
