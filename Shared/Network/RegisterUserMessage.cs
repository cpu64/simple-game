using System;

public class RegisterUserMessage : IMessage
{
    public Guid ClientId { get; set; }

    public RegisterUserMessage()
    {
    }

    public RegisterUserMessage(Guid clientId)
    {
        ClientId = clientId;
    }
}
