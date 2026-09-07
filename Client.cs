using System.Windows.Forms;

public static class Client
{
    public static void Main()
    {
        SharedInputState input = new SharedInputState();

        Server localServer = new Server(input);

        localServer.Start();

        Application.Run(
            new GameWindow(localServer, input)
        );
    }
}
