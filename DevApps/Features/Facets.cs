using DevApps.Commands;
using DevApps.GUI;
using System.ComponentModel;
using System.Dynamic;
using System.Text;
using System.Windows;
using static Program;
using static Program.DevFacet;

namespace DevApps.Features
{
    internal static class Facets
    {
        /// <summary>
        /// Déplace un objet dans la vue
        /// </summary>
        [Description("Déplace un objet dans la vue")]
        [RemoteCall]
        public static async Task MoveObject(string name, string objectName, double x, double y, double w, double h)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    using (DevFacet.Recorder.Rec(name, facet))
                        facet.Objects[objectName].zone = new Rect(x, y, w, h);
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");

                DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                {
                    if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                    {
                        if (currentView.Name == name)
                        {
                            currentView.InvalidateElement(objectName);
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
        /// Retire une commande de la vue
        /// </summary>
        [Description("Retire une commande de la vue")]
        [RemoteCall]
        public static async Task RemoveCommand(string name, string CommandName)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    using (DevFacet.Recorder.Rec(name, facet))
                        facet.Commands.Remove(CommandName);
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");


                DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                {
                    if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                    {
                        if (currentView.Facet == facet)
                        {
                            currentView.RemoveElement(CommandName);
                        }
                    }
                }));
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Ajoute un commande dans la vue
        /// </summary>
        [Description("Ajoute un commande dans la vue")]
        [RemoteCall]
        public static async Task AddCommand(string name, string CommandName, double x, double y)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    var props = new DevFacet.CommandProperties { pos = new System.Windows.Point(x,y) };

                    using (DevFacet.Recorder.Rec(name, facet))
                        facet.Commands.Add(CommandName, props);

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.Facet == facet)
                            {
                                currentView.AddCommand(CommandName, props);
                            }
                        }
                    }));
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
        /// Retire un objet de la vue
        /// </summary>
        [Description("Retire un objet de la vue")]
        [RemoteCall]
        public static async Task RemoveObject(string name, string objectName)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    using (DevFacet.Recorder.Rec(name, facet))
                        facet.Objects.Remove(objectName);
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");


                DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                {
                    if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                    {
                        if (currentView.Facet == facet)
                        {
                            currentView.RemoveElement(objectName);
                        }
                    }
                }));
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Ajoute un objet dans la vue
        /// </summary>
        [Description("Ajoute un objet dans la vue")]
        [RemoteCall]
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

                    using (DevFacet.Recorder.Rec(name, facet))
                        facet.Objects.Add(objectName, props);

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.Facet == facet)
                            {
                                currentView.AddElement(objectName, props);
                            }
                        }
                    }));
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
        /// Supprime une texte de la facette
        /// </summary>
        [Description("Supprime une texte de la facette")]
        [RemoteCall]
        public static async Task RemoveText(string name, string guid)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    var index = facet.Texts.FindIndex(p => p.guid.ToString() == guid);

                    if (index == -1)
                        throw new Exception($"La facette {facet} ne contient pas de texte à cet index");

                    using (DevFacet.Recorder.Rec(name, facet))
                    {
                        facet.Texts.RemoveAt(index);
                    }


                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.Facet == facet)
                            {
                                currentView.RemoveText(new Guid(guid));
                            }
                        }
                    }));
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
        /// Ajoute une texte à une facette
        /// </summary>
        [Description("Ajoute une texte à une facette")]
        [RemoteCall]
        public static async Task AddText(string name, string text, double x, double y, string? guid = null)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    using (DevFacet.Recorder.Rec(name, facet))
                    {
                        var Text = new DevFacet.Text(x, y, text);

                        // restaure le guid si il est fournit
                        if (guid != null)
                            Text.guid = new Guid(guid);

                        facet.Texts.Add(Text);
                    }

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.Facet == facet)
                            {
                                var geo = new DevFacet.Text(x, y, text);
                                currentView.AddText(geo);
                            }
                        }
                    }));
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
        [Description("Supprime une géométrie de la facette")]
        [RemoteCall]
        public static async Task RemoveGeometry(string name, string guid)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    var index = facet.Geometries.FindIndex(p => p.guid.ToString() == guid);
                    
                    if (index == -1)
                        throw new Exception($"La facette {facet} ne contient pas de géométrie à cet index");

                    using (DevFacet.Recorder.Rec(name, facet))
                    {
                        facet.Geometries.RemoveAt(index);
                    }


                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.Facet == facet)
                            {
                                currentView.RemoveGeometry(new Guid(guid));
                            }
                        }
                    }));
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
        [Description("Ajoute une géométrie à une facette")]
        [RemoteCall]
        public static async Task<string> AddGeometry(string name, string path, double x, double y, string? guid = null)
        {
            try
            {
                //await DevFacet._executeLock.WaitAsync();

                if (DevFacet.TryGet(name, out var facet))
                {
                    using (DevFacet.Recorder.Rec(name, facet))
                    {
                        var geometry = new DevFacet.Geometry(x, y, path);

                        // restaure le guid si il est fournit
                        if (guid != null)
                            geometry.guid = new Guid(guid);
                        
                        guid = geometry.guid.ToString(); // pour retour

                        facet.Geometries.Add(geometry);
                    }

                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke((Action)(() =>
                    {
                        if(DevApps.GUI.GuiService.EditorWindow?.Content is DesignerView currentView)
                        {
                            if (currentView.Facet == facet)
                            {
                                var geo = new DevFacet.Geometry(x, y, path);
                                currentView.AddGeometry(geo);
                            }
                        }
                    }));
                }
                else
                    throw new Exception($"La facette {name} n'existe pas");

                return guid;
            }
            finally
            {
                //DevFacet._executeLock.Release();
            }
        }

        /// <summary>
        /// Execute le script de construction de la sortie standard des objets
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        [Description("Execute le script de construction de la sortie standard des objets")]
        [RemoteCall]
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
        /// Retourne une définition structuré de la facette
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        [Description("Retourne une définition structuré de la facette")]
        [RemoteCall]
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
        /// Retourne une description textuelle de la facette
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        [Description("Retourne une description textuelle de la facette")]
        [RemoteCall]
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
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        [Description("Liste les noms des facettes")]
        [RemoteCall]
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
        /// Supprime une facette du projet
        /// </summary>
        [Description("Supprime une facette du projet")]
        [RemoteCall]
        public static async Task Delete(string name)
        {
            if (DevFacet.TryGet(name, out var obj))
            {
                using (DevFacet.Recorder.Rem(name, obj))
                    DevFacet.References.Remove(name);
            }
            else
                throw new Exception($"La facette {name} n'existe pas");

            // actualise la fenêtre

            DevApps.GUI.GuiService.InvalidateFacets();
        }

        /// <summary>
        /// Ajoute une facette au projet
        /// </summary>
        [Description("Ajoute une facette au projet")]
        [RemoteCall]
        public static async Task<string> Create(string baseName, string[] objectNames)
        {
            string name = baseName;

            DevFacet.MakeUniqueName(ref name);
            var obj = DevFacet.Create(name, objectNames);
            using var rec = DevFacet.Recorder.New(name, obj);

            // actualise la fenêtre

            DevApps.GUI.GuiService.InvalidateFacets();

            return name;
        }
    }
}
