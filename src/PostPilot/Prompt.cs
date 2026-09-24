namespace PostPilot;

/// <summary>
/// Terminal questions for settings that were not given as options. There are no defaults: an empty
/// answer asks again, and without a terminal a missing option is an error.
/// </summary>
static class Prompt
{
    public static bool Interactive => !Console.IsInputRedirected;

    public static string Choose(string option, string question, IReadOnlyList<(string Value, string Label)> choices)
    {
        RequireTerminal(option);
        Console.WriteLine(question);
        for (var i = 0; i < choices.Count; i++)
            Console.WriteLine($"  {i + 1}) {choices[i].Label}");
        while (true)
        {
            Console.Write($"Choose 1-{choices.Count}: ");
            var answer = ReadLine().Trim();
            if (int.TryParse(answer, out var n) && n >= 1 && n <= choices.Count)
                return choices[n - 1].Value;
            var match = choices.FirstOrDefault(c => string.Equals(c.Value, answer, StringComparison.OrdinalIgnoreCase));
            if (match.Value is not null)
                return match.Value;
        }
    }

    public static bool YesNo(string option, string question)
    {
        RequireTerminal(option);
        while (true)
        {
            Console.Write($"{question} [y/n]: ");
            switch (ReadLine().Trim().ToLowerInvariant())
            {
                case "y" or "yes" or "j" or "ja": return true;
                case "n" or "no" or "nein": return false;
            }
        }
    }

    public static string Text(string option, string question, bool allowEmpty = false, bool secret = false)
    {
        RequireTerminal(option);
        while (true)
        {
            Console.Write($"{question}: ");
            var answer = secret ? ReadSecret() : ReadLine();
            if (answer.Length > 0 || allowEmpty)
                return answer;
        }
    }

    static void RequireTerminal(string option)
    {
        if (!Interactive)
            throw new UserError($"Missing {option} (required when not running in a terminal).");
    }

    static string ReadLine() => Console.ReadLine() ?? throw new UserError("Input ended.");

    static string ReadSecret()
    {
        var chars = new List<char>();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace) { if (chars.Count > 0) chars.RemoveAt(chars.Count - 1); }
            else if (!char.IsControl(key.KeyChar)) chars.Add(key.KeyChar);
        }
        Console.WriteLine();
        return new string([.. chars]);
    }
}
