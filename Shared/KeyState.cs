using System;

[Flags]
public enum KeyState
{
    None = 0,
    Left = 1 << 0,
    Right = 1 << 1,
    Up = 1 << 2,
    MouseLeft = 1 << 3,
    MouseRight = 1 << 4,
}
