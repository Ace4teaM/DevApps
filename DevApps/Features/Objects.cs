using DevApps.GUI;
using DevApps.Print;
using System.Dynamic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using static Program;
using static Program.DevObject;

namespace DevApps.Features
{
    /// <summary>
    /// Fonctionnalités liés aux objets.
    /// </summary>
    internal static class Objects
    {
        /// <summary>
        /// Construit l'objet et son arbre de dépendances
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet mais uniquement sa sortie (Content), les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task BuildTree(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (Program.DevObject.TryGet(name, out var reference))
                    {
                        Program.DevObject.BuildTree(new KeyValuePair<string, DevObject>(name, reference));
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();

                // Actualise les compteurs
                GuiService.InvalidateObjectsStatus();
            }
        }

        /// <summary>
        /// Affiche le contenu d'un objet
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task ShowContent(string name)
        {
            DevObject? reference = null;

            try
            {
                await DevObject._checkLock.WaitAsync();

                if (Program.DevObject.TryGet(name, out reference))
                {
                    if (reference != null)
                    {
                        try
                        {
                            reference._readOutput.Wait();

                            GuiService.OpenEditorOrDefault(reference.Content, reference.Editor, false);
                        }
                        finally
                        {
                            reference._readOutput.Release();
                        }

                    }
                }
                else 
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._checkLock.Release();
            }
        }

        /// <summary>
        /// Edite le contenu d'un objet
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet mais uniquement sa sortie (Content), les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task EditContent(string name)
        {
            DevObject? reference = null;

            try
            {
                await DevObject._checkLock.WaitAsync();

                if (Program.DevObject.TryGet(name, out reference))
                {
                    try
                    {
                        reference._readOutput.Wait();

                        if (GuiService.OpenEditorOrDefault(reference.Content, reference.Editor, true)) // todo lock object execution
                        {
                            DevObject.IncrementBuilt(name);

                            // Actualise les compteurs
                            GuiService.InvalidateObjectsStatus();
                        }
                    }
                    finally
                    {
                        reference._readOutput.Release();
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._checkLock.Release();
            }
        }

        /// <summary>
        /// Crée une définition structuré de l'objet
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task<dynamic> GetData(string name)
        {
            dynamic data = new ExpandoObject();

            try
            {
                await DevObject._checkLock.WaitAsync();

                if (DevObject.TryGet(name, out var obj))
                {
                    data.Name = name;
                    data.Description = obj.Description;
                    data.DrawScript = obj.DrawCode.Item1;
                    data.BuildScript = obj.BuildMethod.Item1;
                    data.Properties = obj.Properties.Select(p=>new { p.Key, p.Value.Item1 });
                }
            }
            finally
            {
                DevObject._checkLock.Release();
            }

            return data;
        }

        /// <summary>
        /// Crée une description textuelle de l'objet
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task<string> Summary(string name)
        {
            try
            {
                await DevObject._checkLock.WaitAsync();

                StringBuilder sb = new StringBuilder();

                if (DevObject.TryGet(name, out var obj))
                {
                    sb.AppendLine($"The object {name} is described as follows: '{obj.Description}'.");

                    if (obj is DevObjectFile file)
                    {
                        sb.AppendLine($"It is an file object.");
                        sb.AppendLine($"This is an object whose content is directly written to and read from a file due to a large volume of data or a need for persistence.");
                    }

                    if (obj is DevObjectReference reference)
                    {
                        sb.AppendLine($"It is an reference to another object instance {reference.baseObjectName}.");
                        sb.AppendLine($"It shares the same properties except for its content.");
                    }

                    if (obj is DevObjectInstance instance)
                    {
                        if(instance.baseGuid != null)
                            sb.AppendLine($"It is an instance based to another object model with GUID: {instance.baseGuid}.");
                        else
                            sb.AppendLine($"It is an simple instance object");

                        if (instance.guid != null)
                            sb.AppendLine($"It can serve as a model for other objects possessing the GUID: {instance.guid}.");

                        if (obj.Pointers != null && obj.Pointers.Count > 0)
                        {
                            sb.AppendLine($"It has {obj.Pointers.Count} pointers:");
                            foreach (var p in obj.Pointers)
                            {
                                sb.AppendLine($"- {p.Key}: '{p.Value}'");
                            }
                        }
                        if(obj.Content != null)
                        {
                            sb.AppendLine($"It has content of length {obj.Content.Length} bytes.");
                            if (ToPDF.IsBMP(obj.Content))
                                sb.AppendLine($"It has image content formatted as binary BMP.");
                            else if (ToPDF.IsPNG(obj.Content))
                                sb.AppendLine($"It has image content formatted as binary PNG.");
                            else if (ToPDF.IsSVG(obj.Content))
                                sb.AppendLine($"It has graphical vectorial content formatted as text SVG.");
                            else if (ToPDF.IsJPEG(obj.Content))
                                sb.AppendLine($"It has image content formatted as binary JPEG.");
                            else if (ToPDF.IsUTF8(obj.Content))
                                sb.AppendLine($"It has text content encoded in UTF8.");
                        }
                        if(obj.Properties != null && obj.Properties.Count > 0)
                        {
                            sb.AppendLine($"It has {obj.Properties.Count} properties:");
                            foreach (var p in obj.Properties)
                            {
                                sb.AppendLine($"- {p.Key}");
                            }
                        }
                        if(String.IsNullOrEmpty(obj.DrawCode.Item1) == false)
                        {
                            sb.AppendLine($"It can be render with scripted code in Python langage.");
                            if(obj.DrawCode.Item2 == null)
                            {
                                sb.AppendLine($"But the script failed to compil");
                            }
                        }
                        if (String.IsNullOrEmpty(obj.BuildMethod.Item1) == false)
                        {
                            sb.AppendLine($"object content can be build with scripted code in Python langage.");
                            if (obj.BuildMethod.Item2 == null)
                            {
                                sb.AppendLine($"But the script failed to compil");
                            }
                        }
                        if (String.IsNullOrEmpty(obj.InitMethod.Item1) == false)
                        {
                            sb.AppendLine($"object content can be initialized with scripted code in Python langage.");
                            if (obj.InitMethod.Item2 == null)
                            {
                                sb.AppendLine($"But the script failed to compil");
                            }
                        }
                        if (String.IsNullOrEmpty(obj.LoopMethod.Item1) == false)
                        {
                            sb.AppendLine($"It can be executed cyclically using scripted code in the Python language.");
                            if (obj.LoopMethod.Item2 == null)
                            {
                                sb.AppendLine($"But the script failed to compil");
                            }
                        }
                        if (obj.MustBeBuild)
                        {
                            sb.AppendLine($"It needs to be rebuilt because some of the objects pointed to have been modified.");
                        }
                        if (obj.Tags != null && obj.Tags.Length > 0)
                        {
                            sb.AppendLine($"He possesses the following identification badges: {String.Join(",", obj.Tags)}");
                        }
                    }
                }
                else
                {
                    sb.AppendLine($"The object '{name}' can't be found.");
                }

                return sb.ToString();
            }
            finally
            {
                DevObject._checkLock.Release();
            }
        }

        /// <summary>
        /// Liste les noms des objets
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task<string[]> GetNames()
        {
            try
            {
                await DevObject._checkLock.WaitAsync();

                return DevObject.References.Keys.ToArray();
            }
            finally
            {
                DevObject._checkLock.Release();
            }
        }

        /// <summary>
        /// Renomme un objet 
        /// </summary>
        public static async Task Rename(string name, string newName)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var obj) == false)
                        throw new Exception($"L'objet {name} n'existe pas");

                    if (DevObject.References.ContainsKey(newName) == true)
                        throw new Exception($"Le nom d'objet {newName} est déjà utilisé");

                    // remplace l'entrée dans les références
                    using (DevObject.Recorder.Mov(name, newName))
                    {
                        DevObject.References[newName] = obj;
                        DevObject.References.Remove(name);
                    }

                    // renomme l'objet dans les objets de references
                    foreach (var o in DevObject.References)
                    {
                        if (o.Value is DevObjectReference objRef)
                        {
                            if (objRef.baseObjectName == name)
                            {
                                objRef.baseObjectName = newName;
                                Logger.WriteLine($"Renomme Reference {o.Key} : {name} => {newName}");
                            }
                        }
                    }

                    // renomme l'objet dans les pointeurs des autres objets
                    foreach (var o in DevObject.References)
                    {
                        var list = o.Value.Pointers.Where(p => p.Value.target == name).ToArray();
                        if (list.Length > 0)
                        {
                            using (DevObject.Recorder.Rec(o.Key, o.Value))
                            {
                                foreach (var pointer in list)
                                {
                                    o.Value.Pointers[pointer.Key].target = newName;
                                    Logger.WriteLine($"Renomme {pointer.Key} : {name} => {newName}");
                                }
                            }
                        }
                    }

                    // renomme l'objet dans les references des facettes
                    foreach (var o in DevFacet.References)
                    {
                        var list = o.Value.Objects.Where(p => p.Key == name).ToArray();
                        if (list.Length > 0)
                        {
                            using (DevFacet.Recorder.Rec(o.Key, o.Value))
                            {
                                foreach (var pointer in list)
                                {
                                    var tmp = pointer.Value;
                                    o.Value.Objects.Remove(pointer.Key);
                                    o.Value.Objects.Add(newName, tmp);
                                    Logger.WriteLine($"Renomme {o.Key} : {pointer.Key} => {newName}");
                                }
                            }
                        }
                    }
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }

            // actualise la vue de l'éditeur

            DevApps.GUI.GuiService.InvalidateObjects();
        }

        /// <summary>
        ///Supprime un objet du projet
        /// </summary>
        public static async Task Delete(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var obj))
                    {
                        // supprime l'objet de références
                        using (DevObject.Recorder.Rem(name, obj))
                            DevObject.References.Remove(name);

                        // supprime l'objet dans les objets de references
                        foreach (var o in DevObject.References)
                        {
                            if (o.Value is DevObjectReference objRef && objRef.baseObjectName == name)
                            {
                                using (DevObject.Recorder.Rec(o.Key, o.Value))
                                {
                                    // l'objet devient une instance
                                    DevObject.References.Remove(o.Key);
                                    var newObject = DevObject.Create(o.Key, objRef.Description, objRef.Tags); // todo manque les autres propriétés ...
                                    Logger.WriteLine($"Convert reference object to instance {o.Key}");
                                }
                            }
                        }

                        // déréférence l'objet dans les pointeurs des autres objets
                        foreach (var o in DevObject.References)
                        {
                            foreach (var pointer in o.Value.Pointers.Where(p => p.Value.target == name).ToArray())
                            {
                                using (DevObject.Recorder.Rec(o.Key, o.Value))
                                {
                                    o.Value.Pointers[pointer.Key].target = string.Empty;
                                    Logger.WriteLine($"Clear pointer {pointer.Key} => {name}");
                                }
                            }
                        }


                        // supprime l'objet dans les references des facettes
                        foreach (var o in DevFacet.References)
                        {
                            if (!o.Value.Objects.ContainsKey(name))
                                continue;

                            using (DevFacet.Recorder.Rec(o.Key, o.Value))
                            {
                                o.Value.Objects.Remove(name);
                            }

                            Logger.WriteLine($"Remove {name} from facet {o.Key}");
                        }

                        // événement de suppression de l'objet (dispose)
                        obj.OnDelete();
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }

            // actualise la vue de l'éditeur

            DevApps.GUI.GuiService.InvalidateObjects();
        }

        /// <summary>
        /// Ajoute un objet au projet
        /// </summary>
        /// <returns></returns>
        public static async Task<string> Create(string baseName, string description, string[] tags)
        {
            string name = baseName;

            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    DevObject.MakeUniqueName(ref name);
                    var o = DevObjectInstance.Create(name, description, tags);
                    using var rec = DevObject.Recorder.New(name, o);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }


            // actualise la vue de l'éditeur

            DevApps.GUI.GuiService.InvalidateObjects();

            return name;
        }

        /// <summary>
        /// Crée un objet de référence
        /// </summary>
        /// <returns></returns>
        public static async Task<string> CreateReference(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (Program.DevObject.TryGet(name, out var obj) == false)
                        throw new Exception($"L'objet {name} n'existe pas pour créer une référence");

                    var newName = name + "Ref";
                    Program.DevObject.MakeUniqueName(ref newName);

                    // si l'objet est déjà une référence on pointe vers le même objet de base pour éviter les références en cascade
                    if (obj is Program.DevObjectReference reference)
                    {
                        if (reference.BaseObjectName == null)
                            throw new Exception($"L'objet {name} est une référence mais ne possède pas de base pour créer une nouvelle référence");

                        var o = Program.DevObject.CreateReference(newName, reference.BaseObjectName);
                        using var rec = DevObject.Recorder.New(newName, o);
                    }
                    else
                    {
                        var o = Program.DevObject.CreateReference(newName, name);
                        using var rec = DevObject.Recorder.New(newName, o);
                    }

                    // actualise la vue de l'éditeur

                    DevApps.GUI.GuiService.InvalidateObjects();

                    return newName;
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }


        /// <summary>
        /// Crée un ou plusieurs objets à partir de fichiers
        /// </summary>
        public static async Task<string[]> CreateFromFiles(string[] files)
        {
            var objects = new List<Program.DevObject>();
            var names = new List<string>();

            try
            {
                await DevObject._checkLock.WaitAsync();

                foreach (string file in files)
                {
                    var o = DevObject.CreateFromFile(file, out string name);
                    using var rec = DevObject.Recorder.New(name, o);

                    objects.Add(o);
                    names.Add(name);
                }

                if (objects.Count > 0)
                {
                    try
                    {
                        await DevObject._executeLock.WaitAsync();

                        DevObject.CompilObjects(objects);
                        DevObject.Init();
                    }
                    finally
                    {
                        DevObject._executeLock.Release();
                    }
                }
            }
            finally
            {
                DevObject._checkLock.Release();
            }

            // actualise la vue de l'éditeur

            GuiService.InvalidateObjects();

            return names.ToArray();
        }


        /// <summary>
        /// Copie le contenu du stream dans le contenu de l'objet
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task CopyFromStream(string name, MemoryStream content)
        {
            // copie la sortie dans l'objet de destination
            if (String.IsNullOrEmpty(name) == false)
            {
                try
                {
                    await DevObject._executeLock.WaitAsync();

                    try
                    {
                        await DevObject._checkLock.WaitAsync();

                        if (DevObject.TryGet(name, out var obj))
                        {
                            if (obj.Content != null && obj.Content.CanWrite == true)
                            {
                                obj.Content.Position = 0;
                                content.Position = 0;
                                content.CopyTo(obj.Content);
                                obj.Content.SetLength(content.Length);
                                obj.Content.Position = 0;
                                content.Position = 0;
                            }
                        }
                        else
                            throw new Exception($"L'objet {name} n'existe pas");
                    }
                    finally
                    {
                        DevObject._checkLock.Release();
                    }
                }
                finally
                {
                    DevObject._executeLock.Release();
                }
            }
        }

        /// <summary>
        /// Copie le contenu du stream dans le contenu de l'objet
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task<bool> CopyFromFile(string name, string filename)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var obj))
                    {
                        using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            // copie le contenu
                            obj.Content.Position = 0;
                            fileStream.CopyTo(obj.Content);
                            obj.Content.SetLength(fileStream.Length);
                            obj.Content.Position = 0;
                            return true;
                        }
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Compare le contenu avec un fichier
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        /// <returns>true si les contenus sont différent, null si ils ne peuvent être comparé</returns>
        public static async Task<bool?> IsDifferentFromFile(string name, string filename)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.References.TryGetValue(name, out var obj))
                    {
                        if (obj.Content == null)
                            throw new Exception($"L'objet {name} ne possède pas de contenu");

                        var fi1 = new FileInfo(filename);

                        if (fi1.Length != obj.Content.Length)
                            return true;

                        obj.Content.Position = 0;

                        using var sha256 = SHA256.Create();
                        using var fs1 = File.OpenRead(filename);

                        var hash1 = sha256.ComputeHash(fs1);
                        var hash2 = sha256.ComputeHash(obj.Content);

                        obj.Content.Position = 0;

                        return hash1.SequenceEqual(hash2) == false;
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Charge le contenu en cache de tous les objets
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task LoadAllOutputs()
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    foreach (var o in DevObject.References)
                    {
                        o.Value.LoadContent();
                    }
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Sauvegarde le contenu tous les objets dans le cache
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task SaveAllOutputs()
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    foreach (var o in DevObject.References)
                    {
                        o.Value.FlushContent();
                    }
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// S'assure que tous les objets soit initialisé
        /// </summary>
        /// <remarks>
        /// Ne modifie pas l'objet, les modifications ne sont pas sérialisé dans l'historique avec Recorder
        /// </remarks>
        public static async Task MakeSureAllInitialized()
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    DevObject.Init();
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Ajoute l'objet à la bibliothèque
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        /*public static void AddToShared(string name)
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                Program.DevObject.References.TryGetValue(name, out var reference);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                if (reference != null)
                {
                    reference.mutexReadOutput.WaitOne();

                    using TextWriter writer = new StreamWriter(System.IO.Path.Combine(Program.CommonObjPath, name));

                    var settings = new JsonSerializerSettings
                    {
                        Formatting = Formatting.Indented
                    };
                    JsonSerializer serializer = JsonSerializer.CreateDefault(settings);

                    var instance = reference as Program.DevObjectInstance;
                    if (instance == null && reference is Program.DevObjectReference)
                        instance = ((Program.DevObjectReference)reference).GetBaseObject();

                    if (instance == null || selectedElement == null)
                        return;

                    serializer.Serialize(writer, new Serializer.DevObjectInstance(instance));

                    reference.SaveOutput(selectedElement?.Name!, Program.CommonSharedPath);

                    reference.mutexReadOutput.ReleaseMutex();
                }
            }
        }*/
        /// <summary>
        /// Duplique un objet
        /// </summary>
        /// <param name="name">Nom de l'objet à dupliquer</param>
        /// <returns>Nom de l'objet dupliqué</returns>
        public static async Task<string?> Duplicate(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var reference))
                {
                    var newReference = reference.Clone();
                    DevObject.MakeUniqueName(ref name);
                    DevObject.References.Add(name, newReference);
                    using var rec = DevObject.Recorder.New(name, newReference);

                    // actualise la vue de l'éditeur
                    DevApps.GUI.GuiService.InvalidateObjects();

                    return name;
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Duplique les objets
        /// </summary>
        /// <param name="name">Noms des objets à dupliquer</param>
        /// <returns>Nom des objets dupliqués</returns>
        public static async Task<string[]?> Duplicates(string[] names)
        {
            try
            {
                List<string> newNames = new List<string>();

                await DevObject._executeLock.WaitAsync();

                foreach (var name in names)
                {
                    if (DevObject.TryGet(name, out var reference))
                    {
                        string newName = name;
                        var newReference = reference.Clone();
                        DevObject.MakeUniqueName(ref newName);
                        DevObject.References.Add(newName, newReference);
                        using var rec = DevObject.Recorder.New(newName, newReference);

                        newNames.Add(name);
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }

                // actualise la vue de l'éditeur
                DevApps.GUI.GuiService.InvalidateObjects();

                return newNames.ToArray();
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Définit le script d'un objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        /// <param name="scriptName">Nom du script</param>
        /// <param name="scriptCode">Code du script</param>
        public static async Task SetScript(string name, ScriptType scriptType, string scriptCode)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var obj))
                {
                    using (DevObject.Recorder.Rec(name, obj))
                    {
                        try
                        {
                            switch (scriptType)
                            {
                                case ScriptType.Draw:
                                    obj.SetDrawCode(scriptCode);
                                    obj.CompilDraw();
                                    break;
                                case ScriptType.Build:
                                    obj.SetBuildMethod(scriptCode);
                                    obj.CompilBuild();
                                    break;
                                case ScriptType.Loop:
                                    obj.SetLoopMethod(scriptCode);
                                    obj.CompilLoop();
                                    break;
                                case ScriptType.Init:
                                    obj.SetInitMethod(scriptCode);
                                    obj.CompilInit();
                                    break;
                                case ScriptType.UserAction:
                                    obj.SetUserAction(scriptCode);
                                    obj.CompilUserAction();
                                    break;
                                default:
                                    throw new Exception($"Le type de script {scriptType} n'est pas reconnu");
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Erreur de compilation {scriptType}. " + ex.Message);
                        }
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Ajoute un pointeur à un objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        /// <param name="pointerName">Nom du pointeur</param>
        /// <param name="pointerTarget">Nom de l'objet ciblé</param>
        /// <param name="tags">Tags associés au pointeur</param>
        public static async Task AddPointer(string name, string pointerName, string pointerTarget, string[] tags)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var reference))
                {
                    using (DevObject.Recorder.Rec(name, reference))
                    {
                        reference.AddPointer(pointerName, pointerTarget, tags);

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateObjectsStatus();
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Copie les données du cache comme contenu initial de l'objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task MemorizeCachedContent(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var reference))
                {
                    using (DevObject.Recorder.Rec(name, reference))
                    {
                        byte[] bytes = new byte[reference.Content.Length];
                        reference.Content.Seek(0, SeekOrigin.Begin);
                        reference.Content.Read(bytes, 0, (int)reference.Content.Length);
                        reference.Content.Seek(0, SeekOrigin.Begin);

                        using (DevObject.Recorder.Rec(name, reference))
                            reference.InitialDataBase64 = Convert.ToBase64String(bytes);

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateObjectsStatus();
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Copie les données du contenu initial dans le cache de l'objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task RestoreCachedContent(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var reference))
                {
                    using (DevObject.Recorder.Rec(name, reference))
                    {
                        var bytes = Convert.FromBase64String(reference.InitialDataBase64);
                        reference.Content.Seek(0, SeekOrigin.Begin);
                        reference.Content.Write(bytes);
                        reference.Content.SetLength(bytes.Length);
                        reference.Content.Seek(0, SeekOrigin.Begin);

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateObjectsStatus();
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Définit l'editeur d'un objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task<string?> SetEditor(string name, string editor)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var reference))
                {
                    using (DevObject.Recorder.Rec(name, reference))
                    {
                        var old = reference.Editor;
                        reference.Editor = editor;

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateObjectsStatus();

                        return old;
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Définit la description d'un objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task<string> SetDescription(string name, string description)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                if (DevObject.TryGet(name, out var reference))
                {
                    using (DevObject.Recorder.Rec(name, reference))
                    {
                        var oldDescription = reference.Description;
                        reference.Description = description;

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateObjectsStatus();

                        return oldDescription;
                    }
                }
                else
                    throw new Exception($"L'objet {name} n'existe pas");
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Définit les tags d'un objet
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        /// <param name="tags">Tags sous la forme "#un #deux #trois ..."</param>
        public static async Task<string> SetTags(string name, string tags)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var reference))
                    {
                        var instance = reference.IsReference ? ((Program.DevObjectReference)reference).baseObject : (Program.DevObjectInstance)reference;
                        if (instance != null)
                        {
                            using (DevObject.Recorder.Rec(name, instance))
                            {
                                var old = String.Join(' ', reference.Tags);
                                instance!.tags = new HashSet<string>(tags.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

                                // actualise la vue de l'éditeur
                                DevApps.GUI.GuiService.InvalidateObjectsStatus();

                                return old;
                            }
                        }
                        else
                            throw new Exception($"L'objet de base de la référence {name} ne peut pas être identifié");
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Définit l'objet comme modèle en lui attribuant un GUID unique.
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task SetAsModel(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var reference))
                    {
                        if (reference is Program.DevObjectInstance instance)
                        {
                            using (DevObject.Recorder.Rec(name, reference))
                            {
                                if (instance.guid == null)
                                    instance.guid = Guid.NewGuid();

                                // actualise la vue de l'éditeur
                                DevApps.GUI.GuiService.InvalidateObjects();
                            }
                        }
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Met à jour le modèle depuis un objet du projet.
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task UpdateModel(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var reference))
                    {
                        // Si l'objet possède un modèle
                        if (reference is DevObjectInstance instance)
                        {
                            if (instance.AsModel())
                            {
                                // todo !! Attention pas de lock dans les appels des services SharedServices et LogServices !! 

                                if (SharedServices.ApplyAllObjects(p => p.Guid == instance.baseGuid, Program.CommonSharedPath, (dir, key, model) =>
                                {
                                    // log les modifications
                                    if (LogServices.LogDifference(model.content, instance, dir) == true)
                                    {
                                        using (DevObject.Recorder.Rec(key, model))
                                        {
                                            // actualise l'objet du modèle
                                            model.content.UpdateFrom(instance);
                                        }
                                        return true;
                                    }
                                    return false;
                                }) == 0)
                                {
                                    Program.Logger.WriteLine("Modèle introuvable pour l'objet " + name);
                                }
                                else
                                {
                                    // actualise la vue de l'éditeur
                                    DevApps.GUI.GuiService.InvalidateObjects();
                                }
                            }
                            else
                                throw new Exception($"L'objet {name} n'a pas de modèle assigné");
                        }
                        else
                            throw new Exception($"L'objet {name} n'est pas une instance");
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }

        /// <summary>
        /// Met à jour un objet depuis son modèle de la bibliothèque partagée.
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static async Task UpdateFromModel(string name)
        {
            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var reference))
                    {
                        if (reference is Program.DevObjectInstance instance)
                        {
                            if (instance.AsModel())
                            {
                                var list = new List<Serializer.DevObjectInstance>();
                                SharedServices.EnumerateObjects(p => p.Guid == instance.baseGuid, Program.CommonSharedPath, ref list);
                                if (list.Count == 1)
                                {
                                    if (MessageBox.Show(GuiService.EditorWindow, $"Objet modèle trouvé: '{list[0].Description}'.\nVoulez-vous mettre à jour depuis la bibliothèque ?", "Avertissement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                    {
                                        // Actualise l'objet
                                        using (DevObject.Recorder.Rec(name, instance))
                                        {
                                            instance.UpdateFrom(list[0].content);
                                        }

                                        Program.DevObject.CompilObjects([instance]);
                                        Program.DevObject.Init();
                                        Program.DevObject.Build(Program.DevObject.References.Where(p => p.Key == name));

                                        // actualise la vue de l'éditeur
                                        DevApps.GUI.GuiService.InvalidateObjects();
                                    }
                                }
                                else if (list.Count > 1)
                                {
                                    MessageBox.Show(GuiService.EditorWindow, "Il existe plusieurs objets partagés possédant cet identifiant, veuillez corriger la bibliothèque.", "Avertissement", MessageBoxButton.OK);

                                    Program.Logger.WriteLine("Multiples objets partagés avec le GUID: " + instance.baseGuid);
                                    foreach (var item in list)
                                        Program.Logger.WriteLine("* " + item.Description);
                                    Program.Logger.WriteLine();
                                }
                                else if (list.Count == 0)
                                {
                                    MessageBox.Show(GuiService.EditorWindow, "L'objet modèle est introuvable dans la bibliothèque.", "Avertissement", MessageBoxButton.OK);

                                    Program.Logger.WriteLine("Objet partagé introuvable avec le GUID: " + instance.baseGuid);
                                }
                            }
                            else
                                throw new Exception($"L'objet {name} n'a pas de modèle assigné");
                        }
                        else
                            throw new Exception($"L'objet {name} n'est pas une instance");
                    }
                    else
                        throw new Exception($"L'objet {name} n'existe pas");
                }
                finally
                {
                    DevObject._checkLock.Release();
                }
            }
            finally
            {
                DevObject._executeLock.Release();
            }
        }
    }
}
