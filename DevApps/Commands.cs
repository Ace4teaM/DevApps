using static IronPython.Modules._ast;
using System.Net;
using System.Windows;
using DevApps.GUI;
using static Program.DevFacet;
using System.Windows.Controls;

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
    public string? position { get; set; }
    public string? size { get; set; }
    public string? command { get; set; }
    public string? arguments { get; set; }
    public string? path { get; set; }
    public string? guid { get; set; }
    public string? index { get; set; }
}


internal partial class Program
{
    internal static void ParseCommands(string commands)
    {
        var cmdList = System.Text.Json.JsonSerializer.Deserialize<List<Command>>(commands);
        if (cmdList == null)
            return;

        try
        {
            foreach (var cmd in cmdList)
            {
                switch (cmd.action)
                {
                    case "INSERT_COMMAND":
                        {
                            if(cmd.facet == null || cmd.command == null || cmd.arguments == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            facet.BuildCommands.Add(cmd.command, cmd.arguments);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if(currentView != null && currentView.facette == facet)
                            {
                                currentView.InvalidateCommands();
                            }
                        }
                        break;
                    case "ADD_GEOMETRY":
                        {
                            if (cmd.facet == null || cmd.position == null || cmd.path == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            var position = Point.Parse(cmd.position);

                            var geometry = new DevFacet.Geometry(position.X, position.Y, cmd.path);

                            facet.Geometries.Add(geometry);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if (currentView != null && currentView.facette == facet)
                            {
                                var geo = new DevFacet.Geometry(position.X, position.Y, cmd.path);
                                currentView.AddGeometry(geo);
                            }
                        }
                        break;
                    case "REMOVE_GEOMETRY":
                        {
                            if (cmd.facet == null || cmd.index == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            var index = int.Parse(cmd.index);

                            if (index >= facet.Geometries.Count)
                                throw new Exception(@"La facette {cmd.facet} ne contient pas de géométrie à cet index");

                            var geometry = facet.Geometries[index];

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if (currentView != null && currentView.facette == facet)
                            {
                                currentView.RemoveGeometry(index);
                            }
                        }
                        break;
                    case "ADD_OBJECT":
                        {
                            if (cmd.facet == null || cmd.position == null || cmd.size == null || cmd.name == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            var position = Point.Parse(cmd.position);

                            var size = Point.Parse(cmd.size);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if (currentView != null && currentView.facette == facet)
                            {
                                currentView.AddElement(cmd.name, new DevFacet.ObjectProperties { title = TitlePlacement.TopLeft, background = "#FFFFFFFF", zone = new Rect(position, size) });
                            }
                        }
                        break;
                    case "REMOVE_OBJECT":
                        {
                            if (cmd.facet == null || cmd.name == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            var obj = DevObject.Get(cmd.name);

                            facet.Objects.Remove(cmd.name);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if (currentView != null && currentView.facette == facet)
                            {
                                currentView.RemoveElement(cmd.name);
                            }
                        }
                        break;
                    case "MOVE_OBJECT":
                        {
                            if (cmd.facet == null || cmd.position == null || cmd.size == null || cmd.name == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            var obj = DevObject.Get(cmd.name);

                            var position = Point.Parse(cmd.position);

                            var size = Point.Parse(cmd.size);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if (currentView != null && currentView.facette == facet)
                            {
                                var element = currentView.GetElement(cmd.name);
                                if (element != null)
                                {
                                    Canvas.SetLeft(element, position.X);
                                    Canvas.SetTop(element, position.Y);
                                    element.Width = size.X;
                                    element.Height = size.Y;
                                }
                            }
                        }
                        break;
                    case "DELETE_OBJECT":
                        {
                            if (cmd.name == null)
                                throw new ArgumentException();

                            var facet = DevFacet.Get(cmd.facet);

                            if (facet == null)
                                throw new Exception(@"La facette {cmd.facet} n'existe pas");

                            var obj = DevObject.Get(cmd.name);

                            DevObject.DeleteObject(cmd.name);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerView;

                            if (currentView != null && currentView.facette == facet)
                            {
                                currentView.RemoveElement(cmd.name);
                            }
                        }
                        break;
                    case "CREATE_OBJECT":
                        {
                            if (cmd.name == null || cmd.description == null)
                                throw new ArgumentException();

                            var obj = DevObject.Create(cmd.name, cmd.description, cmd.tags ?? new string[0]);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerDataView;

                            if (currentView != null)
                            {
                                currentView.InvalidateObjects();
                            }
                        }
                        break;
                    case "RENAME_OBJECT":
                        {
                            if (cmd.name == null || cmd.new_name == null)
                                throw new ArgumentException();

                            var obj = DevObject.Get(cmd.name);

                            if (obj == null)
                                throw new Exception(@"L'objet {cmd.name} n'existe pas");

                            if (DevObject.References.ContainsKey(cmd.new_name) == true)
                                throw new Exception(@"Le nom d'objet {obj.new_name} est déjà utilisé");

                            DevObject.References[cmd.name] = obj;
                            DevObject.References.Remove(cmd.new_name);

                            var currentView = DevApps.GUI.Service.EditorWindow?.content as DesignerDataView;

                            if (currentView != null)
                            {
                                currentView.InvalidateObjects();
                            }
                        }
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}