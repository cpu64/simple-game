using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public sealed class ConsoleUI
{
    private readonly List<string> _messages = new();
    private readonly StringBuilder _input = new();
    private readonly object _lock = new();

    private readonly CommandFactory _commandFactory;
    private readonly Stack<Command> _commandHistory = new();

    public ConsoleUI(CommandFactory commandFactory)
    {
        _commandFactory = commandFactory;
    }

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

    private void ExecuteCommand(string input)
    {
        string command = input.Trim();

        switch (command.ToLowerInvariant())
        {
            case "help":
                WriteLine("Available commands:");
                WriteLine("  help             - Show available commands");
                WriteLine("  loglevel <level> - Change the minimum log level");
                WriteLine("  undo             - Undo the last command");
                WriteLine("  clear            - Clear the console");
                return;

            case "clear":
                lock (_lock)
                {
                    _messages.Clear();
                    Redraw();
                }
                return;

            case "undo":
                if (_commandHistory.TryPop(out Command? previousCommand))
                {
                    previousCommand.Undo();
                    WriteLine("Command undone.");
                }
                else
                {
                    WriteLine("Nothing to undo.");
                }
                return;
        }

        Command? newCommand = _commandFactory.Create(command);

        if (newCommand == null)
        {
            WriteLine($"Unknown command: {command}");
            return;
        }

        newCommand.Execute();
        _commandHistory.Push(newCommand);
        WriteLine($"Executed: {command}");
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
