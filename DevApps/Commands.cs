using DevApps;
using DevApps.Features;
using DevApps.GUI;
using System.Windows;
using System.Windows.Controls;
using static Program.DevFacet;

namespace Commands
{
    internal class Position
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    internal class Size
    {
        public int width { get; set; }
        public int height { get; set; }
    }

    internal class Command
    {
        public string? action { get; set; }
        public string? name { get; set; }
        public string? new_name { get; set; }
        public string? description { get; set; }
        public string? editor { get; set; }
        public string[]? tags { get; set; }
        public string? initMethod { get; set; }
        public string? buildMethod { get; set; }
        public string? facet { get; set; }
        public Position? position { get; set; }
        public Size? size { get; set; }
        public string? command { get; set; }
        public string? arguments { get; set; }
        public string? path { get; set; }
        public string? guid { get; set; }
        public string? index { get; set; }
    }
}

internal partial class Program
{
    internal static async Task ParseCommands(string commands)
    {
        var cmdList = System.Text.Json.JsonSerializer.Deserialize<List<Commands.Command>>(commands);
        if (cmdList == null)
            return;

        try
        {
            foreach (var cmd in cmdList)
            {
                switch (cmd.action)
                {
                    default:
                        continue;
                }

                Program.Logger.Print(cmd.action);
            }
        }
        catch (Exception ex)
        {
            Program.Logger.WriteLine(ex.Message);
        }
    }
}