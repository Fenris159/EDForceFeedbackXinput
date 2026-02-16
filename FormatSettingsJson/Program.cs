using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

static class Program
{
    static int Main(string[] args)
    {
        string path = args.Length > 0 ? args[0] : "settings.json";
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return 1;
        }
        try
        {
            string content = File.ReadAllText(path);
            var obj = JObject.Parse(content);
            string formatted = obj.ToString(Formatting.Indented);
            File.WriteAllText(path, formatted);
            Console.WriteLine($"Formatted: {Path.GetFullPath(path)}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
}
