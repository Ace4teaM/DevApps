using IronPython.Modules;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serializer;
using System.IO;

namespace DevApps
{
    internal static class SharedServices
    {
        /// <summary>
        /// Applique une action à tous les objets validant le prédicat
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="path"></param>
        /// <param name="action"></param>
        internal static int ApplyAllObjects(Func<DevObjectInstance, bool> predicate, string path, Func<string,string,DevObjectInstance,bool> action)// = Program.CommonDataPath
        {
            int count = 0;
            try
            {
                // liste les objets partagés
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    var filename = System.IO.Path.Combine(dir, Program.Filename);
                    if (File.Exists(filename) == true)
                    {
                        using StreamReader reader = new StreamReader(filename);

                        var settings = new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented
                        };

                        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
                        serializer.Error += (sender, e) =>
                        {
                            Program.Logger.WriteLine(e.ErrorContext.Error.ToString());
                        };

                        var proj = new Serializer.DevExternalProject();

                        serializer.Populate(reader, proj);

                        // Ajoute les objets au projet
                        bool save = false;

                        foreach (var o in proj.Objects)
                        {
                            if (predicate.Invoke(o.Value))
                            {
                                if (action.Invoke(dir, o.Key, o.Value))
                                {
                                    count++;
                                    save = true;
                                }
                            }
                        }

                        reader.Close();

                        // Sauvegarde les modifications
                        if (save)
                        {
                            try
                            {
                                var tmpFilename = Path.GetTempFileName();

                                using (TextWriter writer = new StreamWriter(tmpFilename))
                                {
                                    serializer.Serialize(writer, proj);
                                }

                                File.Move(tmpFilename, filename, true);
                            }
                            catch (Exception ex)
                            {
                                Program.Logger.WriteLine($"Erreur lors de la sauvegarde du projet {filename}.");
                                Program.Logger.WriteLine(ex.Message);
                            }
                        }
                    }
                    else
                    {
                        count += ApplyAllObjects(predicate, dir, action);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }

            return count;
        }

        /// <summary>
        /// Enumère tous les objets validant le prédicat
        /// </summary>
        internal static int EnumerateObjects(Func<DevObjectInstance, bool> predicate, string path, ref List<DevObjectInstance> list)// = Program.CommonDataPath
        {
            try
            {
                // liste les objets partagés
                foreach (var dir in Directory.EnumerateDirectories(path))
                {
                    var filename = System.IO.Path.Combine(dir, Program.Filename);
                    if (File.Exists(filename) == true)
                    {
                        var header = System.IO.Path.GetFileName(dir);

                        using StreamReader reader = new StreamReader(filename);

                        JsonSerializer serializer = JsonSerializer.CreateDefault();
                        serializer.Error += (sender, e) =>
                        {
                            Program.Logger.WriteLine(e.ErrorContext.Error.ToString());
                        };

                        var proj = new Serializer.DevExternalProject();

                        serializer.Populate(reader, proj);

                        // Ajoute les objets au projet

                        foreach (var o in proj.Objects)
                        {
                            foreach (var ptr in o.Value.Pointers)
                                ptr.Value.target = String.Empty;

                            if (predicate.Invoke(o.Value))
                                list.Add(o.Value);
                        }
                    }
                    else
                    {
                        EnumerateObjects(predicate, dir, ref list);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }

            return list.Count;
        }

        /// <summary>
        /// Obtient le chemin d'accès à la bibliothèque d'objets
        /// </summary>
        internal static string? GetRegisterSharedPath()
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        return key.GetValue("SharedPath")?.ToString();
                    }
                }
            }
            catch
            {
            }

            return null;
        }

    }
}
