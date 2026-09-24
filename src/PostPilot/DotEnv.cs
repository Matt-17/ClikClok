namespace PostPilot;

/// <summary>
/// Loads KEY=VALUE lines from a .env file into the process environment. Variables that are already set
/// win, so a real environment variable overrides the file.
/// </summary>
static class DotEnv
{
    public static void Load(string path)
    {
        if (!File.Exists(path))
            return;
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;
            if (line.StartsWith("export "))
                line = line["export ".Length..].TrimStart();
            var equals = line.IndexOf('=');
            if (equals <= 0)
                continue;

            var key = line[..equals].Trim();
            var value = line[(equals + 1)..].Trim();
            if (value.Length >= 2 && value[0] is '"' or '\'' && value[^1] == value[0])
                value = value[1..^1];
            else if (value.IndexOf(" #", StringComparison.Ordinal) is var comment and >= 0)
                value = value[..comment].TrimEnd();

            if (Environment.GetEnvironmentVariable(key) is null)
                Environment.SetEnvironmentVariable(key, value);
        }
    }
}
