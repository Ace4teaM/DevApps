using DevApps.GUI;
using System.Dynamic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static Program;
using static Program.DevFacet;

namespace DevApps.Features
{
    internal static class Facets
    {
        /// <summary>
        /// Déplace un objet dans la vue
        /// </summary>
        public static async Task MoveObject(string name, string objectName, double x, double y, double w, double h)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    facet.Objects[objectName].zone = new Rect(new Point(x, y), new Point(w, h));
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");

                DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                {
                    if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                    {
                        if (currentView.facette == facet)
                        {
                            var element = currentView.GetElement(objectName);
                            if (element != null)
                            {
                                Canvas.SetLeft(element, x);
                                Canvas.SetTop(element, y);
                                element.Width = w;
                                element.Height = h;
                            }
                        }
                    }
                });
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Retire un objet de la vue
        /// </summary>
        public static async Task RemoveObject(string name, string objectName)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    facet.Objects.Remove(objectName);
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");


                DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                {
                    if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                    {
                        if (currentView.facette == facet)
                        {
                            currentView.RemoveElement(objectName);
                        }
                    }
                });
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Ajoute un objet dans la vue
        /// </summary>
        public static async Task AddObject(string name, string objectName, double x, double y, double w, double h)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    var position = new Point(x, y);

                    var size = new Point(w, h);

                    var props = new DevFacet.ObjectProperties { title = TitlePlacement.TopLeft, background = "#FFFFFFFF", zone = new Rect(position, size) };

                    facet.Objects.Add(objectName, props);

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.facette == facet)
                            {
                                currentView.AddElement(objectName, props);
                            }
                        }
                    });
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Supprime une géométrie de la facette
        /// </summary>
        public static async Task RemoveGeomerty(string name, string guid)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    var index = facet.Geometries.FindIndex(p => p.guid.ToString() == guid);

                    if (index == -1)
                        throw new Exception($"La facette {facet} ne contient pas de géométrie à cet index");

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.facette == facet)
                            {
                                currentView.RemoveGeometry(index);
                            }
                        }
                    });

                    facet.Geometries.RemoveAt(index);

                    //History.Push(() => AddGeomerty(name, geometry.path, geometry.x, geometry.y, geometry.guid));
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Ajoute une géométrie à une facette
        /// </summary>
        public static async Task AddGeomerty(string name, string path, double x, double y, string? guid = null)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    var geometry = new DevFacet.Geometry(x, y, path);

                    // restaure le guid si il est fournit
                    if (guid != null)
                        geometry.guid = new Guid(guid);

                    facet.Geometries.Add(geometry);

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if(DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.facette == facet)
                            {
                                var geo = new DevFacet.Geometry(x, y, path);
                                currentView.AddGeometry(geo);
                            }
                        }
                    });

                    //History.Push(() => RemoveGeomerty(name, geometry.guid));
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Execute le script de construction de la sortie standard des objets
        /// </summary>
        public static async Task Build(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync(); // facet.Build();

                if (DevFacet.TryGet(name, out var facet))
                {
                    facet.Build();
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Crée une définition structuré de l'objet
        /// </summary>
        public static async Task<dynamic> GetData(string name)
        {
            dynamic data = new ExpandoObject();

            try
            {
                //await DevFacet._checkLock.WaitAsync();

                if (DevFacet.TryGet(name, out var obj))
                {
                    data.Name = name;
                    data.Texts = obj.Texts;
                    data.Geometries = obj.Geometries;
                    data.Objects = obj.Objects;
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");
            }
            finally
            {
                //DevFacet._checkLock.Release();
            }

            return data;
        }

        /// <summary>
        /// Crée une description textuelle de l'objet
        /// </summary>
        public static async Task<string> Summary(string name)
        {
            try
            {
                //await DevFacet._checkLock.WaitAsync();

                StringBuilder sb = new StringBuilder();

                if (DevFacet.TryGet(name, out var obj))
                {
                    sb.AppendLine($"The facet {name} is a visual layout contains objects, geometries and texts with absolute positioning.");

                    if (obj.Objects != null && obj.Objects.Count > 0)
                    {
                        sb.AppendLine($"It has {obj.Objects.Count} objects:");
                        foreach (var p in obj.Objects)
                        {
                            sb.AppendLine($"- {p.Key}: at '{p.Value.zone}'");
                        }
                    }
                    if (obj.Texts != null && obj.Texts.Count > 0)
                    {
                        sb.AppendLine($"It has {obj.Texts.Count} texts:");
                        foreach (var p in obj.Texts)
                        {
                            sb.AppendLine($"- '{p.text}': at 'X:{p.X}, Y:{p.Y}' with guid '{p.guid}'");
                        }
                    }
                    if (obj.Geometries != null && obj.Geometries.Count > 0)
                    {
                        sb.AppendLine($"It has {obj.Geometries.Count} geometries:");
                        foreach (var p in obj.Geometries)
                        {
                            sb.AppendLine($"- '{p.path}': at 'X:{p.X}, Y:{p.Y}' with guid '{p.guid}'");
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"The facet '{name}' can't be found.");
                }

                return sb.ToString();
            }
            finally
            {
               // DevFacet._checkLock.Release();
            }
        }

        /// <summary>
        /// Liste les noms des facettes
        /// </summary>
        public static async Task<string[]> GetNames()
        {
            try
            {
                //await DevFacet._checkLock.WaitAsync();

                return DevFacet.References.Keys.ToArray();
            }
            finally
            {
               //DevFacet._checkLock.Release();
            }
        }

        /// <summary>
        ///Supprime une facette du projet
        /// </summary>
        public static async Task Delete(string name)
        {
            if (DevFacet.TryGet(name, out var obj))
            {
                DevFacet.References.Remove(name);
            }
            else
                throw new Exception($"La facette {name} n'existe pas");

            // actualise la fenêtre

            DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
            {
                DevApps.GUI.GuiService.EditorWindow?.InvalidateFacets();
            });
        }

        /// <summary>
        /// Ajoute une facette au projet
        /// </summary>
        /// <returns></returns>
        public static async Task<string> Create(string baseName, string[] objectNames)
        {
            string name = baseName;

            DevFacet.MakeUniqueName(ref name);
            DevFacet.Create(name, objectNames);

            // actualise la fenêtre

            DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
            {
                DevApps.GUI.GuiService.EditorWindow?.InvalidateFacets();
            });

            return name;
        }
    }
}
