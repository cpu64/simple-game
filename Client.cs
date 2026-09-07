using System;
using System.Windows.Forms;

public static class Client
{
    public static void Main()
    {
        Server localServer = new Server();

        Application.Run(
            new GameWindow(localServer)
        );
    }
}
