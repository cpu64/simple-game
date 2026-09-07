using System;
using System.Drawing;
using System.Windows.Forms;

public class GameWindow : Form
{
    private readonly Server server;

    private readonly Timer timer;

    private DateTime lastFrameTime;
    private double accumulatedTime;

    private bool left;
    private bool right;

    public GameWindow(Server server)
    {
        this.server = server;

        lastFrameTime = DateTime.UtcNow;
        accumulatedTime = 0.0;

        Text = "Terraria Prototype";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;
        KeyPreview = true;

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;

        timer = new Timer();
        timer.Interval = (int)(1000.0 / GameConstants.TargetFrameRate);
        timer.Tick += OnFrame;
        timer.Start();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left)
            left = true;

        if (e.KeyCode == Keys.Right)
            right = true;
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left)
            left = false;

        if (e.KeyCode == Keys.Right)
            right = false;
    }

    private void OnFrame(object sender, EventArgs e)
    {
        DateTime now = DateTime.UtcNow;

        double frameTime = (now - lastFrameTime).TotalSeconds;

        lastFrameTime = now;

        // Prevent a huge catch-up if the window
        // was suspended or the debugger stopped.
        if (frameTime > 0.25)
            frameTime = 0.25;

        accumulatedTime += frameTime;

        while (accumulatedTime >= GameConstants.SimulationTickDuration)
        {
            InputState input = new InputState
            {
                Left = left,
                Right = right
            };

            server.Tick(input);

            accumulatedTime -= GameConstants.SimulationTickDuration;
        }

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        g.Clear(Color.Black);

        Player player = server.World.Player;

        g.FillEllipse(
            Brushes.Red,
            (float)player.X - 10,
            (float)player.Y - 10,
            20,
            20
        );
    }
}
