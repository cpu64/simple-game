using System;
using System.Drawing;
using System.Windows.Forms;

public class GameWindow : Form
{
    private readonly Server server;
    private readonly SharedInputState input;

    private readonly Timer timer;

    private WorldSnapshot snapshot;

    public GameWindow(Server server, SharedInputState input)
    {
        this.server = server;
        this.input = input;

        Text = "Terraria Prototype";
        ClientSize = new Size(800, 600);
        DoubleBuffered = true;
        KeyPreview = true;

        KeyDown += OnKeyDown;
        KeyUp += OnKeyUp;
        FormClosed += OnFormClosed;

        snapshot = server.GetSnapshot();

        timer = new Timer();
        timer.Interval =
        (int)(1000.0 / GameConstants.TargetFrameRate);

        timer.Tick += OnFrame;
        timer.Start();
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left)
            input.Press(InputState.Left);

        if (e.KeyCode == Keys.Right)
            input.Press(InputState.Right);

        if (e.KeyCode == Keys.Up)
            input.Press(InputState.Up);
    }

    private void OnKeyUp(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Left)
            input.Release(InputState.Left);

        if (e.KeyCode == Keys.Right)
            input.Release(InputState.Right);

        if (e.KeyCode == Keys.Up)
            input.Release(InputState.Up);
    }

    private void OnFrame(object sender, EventArgs e)
    {
        snapshot = server.GetSnapshot();

        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        g.Clear(Color.Black);

        Player player = snapshot.Player;

        g.FillEllipse(
            Brushes.Red,
            (float)player.X - 10,
            (float)player.Y - 10,
            20,
            20
        );
    }

    private void OnFormClosed(object sender, FormClosedEventArgs e)
    {
        timer.Stop();
        server.Stop();
    }
}
