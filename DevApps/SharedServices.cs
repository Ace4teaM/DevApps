using Newtonsoft.Json;
using Serializer;
using System.IO;

namespace DevApps
{
    internal static class SharedServices
    {
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
                            System.Console.WriteLine(e.ErrorContext.Error.ToString());
                        };

                        var proj = new Serializer.DevExternalProject();

                        serializer.Populate(reader, proj);

                        // Ajoute les objets au projet

                        foreach (var o in proj.Objects)
                        {
                            o.Value.dataPath = Path.Combine(dir, Program.DataDir, o.Key);

                            foreach (var ptr in o.Value.Pointers)
                                ptr.Value.target = String.Empty;

                            if (predicate.Invoke(o.Value))
                                list.Add(o.Value);
                        }
                    }
                    else
                    {
                        var header = System.IO.Path.GetFileName(dir);
                        EnumerateObjects(predicate, dir, ref list);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return list.Count;
        }
    }
}
