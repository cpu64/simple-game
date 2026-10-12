using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed class ConsoleUI
{
    private readonly List<string> _messages = new();
    private readonly StringBuilder _input = new();
    private readonly object _lock = new();

    public void WriteLine(string message)
    {
        lock (_lock)
        {
            _messages.Add(message);
            Redraw();
        }
    }

    public void Run()
    {
        lock (_lock)
        {
            Console.CursorVisible = false;
            Redraw();
        }

        try
        {
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);

                if (key.Key == ConsoleKey.Escape)
                    break;

                string? command = null;

                lock (_lock)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        command = _input.ToString();
                        _input.Clear();
                    }
                    else if (key.Key == ConsoleKey.Backspace)
                    {
                        if (_input.Length > 0)
                            _input.Length--;
                    }
                    else if (!char.IsControl(key.KeyChar))
                    {
                        _input.Append(key.KeyChar);
                    }

                    Redraw();
                }

                // Execute outside the lock because commands may call WriteLine().
                if (command != null)
                    ExecuteCommand(command);
            }
        }
        finally
        {
            lock (_lock)
            {
                Console.CursorVisible = true;
            }
        }
    }

    private void ExecuteCommand(string command)
    {
        switch (command.Trim().ToLowerInvariant())
        {
            case "hello":
                WriteLine("Hello world!");
                break;

            case "help":
                WriteLine("Available commands: hello, help, clear, exit");
                break;

            case "clear":
                lock (_lock)
                {
                    _messages.Clear();
                    Redraw();
                }
                break;

            case "exit":
                WriteLine("Press Escape to exit.");
                break;

            case "":
                break;

            default:
                WriteLine($"Unknown command: {command}");
                break;
        }
    }

    private void Redraw()
    {
        // Caller must hold _lock.
        int width = Console.WindowWidth;
        int height = Console.WindowHeight;

        if (width <= 0 || height <= 0)
            return;

        int outputRows = height - 1;

        var visibleMessages = _messages.TakeLast(outputRows).ToArray();

        for (int row = 0; row < outputRows; row++)
        {
            Console.SetCursorPosition(0, row);
            Console.Write(new string(' ', width));

            if (row < visibleMessages.Length)
            {
                Console.SetCursorPosition(0, row);

                string message = visibleMessages[row];
                Console.Write(message.Length > width ? message[..width] : message);
            }
        }

        // Keep the final row reserved for input.
        Console.SetCursorPosition(0, height - 1);
        Console.Write(new string(' ', width));
        Console.SetCursorPosition(0, height - 1);

        string prompt = "> ";
        Console.Write(prompt);

        string input = _input.ToString();
        int availableWidth = Math.Max(0, width - prompt.Length);

        if (input.Length > availableWidth)
            input = input[^availableWidth..];

        Console.Write(input);

        Console.SetCursorPosition(Math.Min(prompt.Length + input.Length, width - 1), height - 1);
    }
}
