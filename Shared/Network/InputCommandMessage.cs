using System;

public class InputCommandMessage : IMessage
{
    public InputCommand Command { get; set; }

    public InputCommandMessage() { }

    public InputCommandMessage(InputCommand command)
    {
        Command = command;
    }
}
