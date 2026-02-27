using System.IO;
using System.Security.Cryptography;
using System.Windows;
using static Program;

namespace DevApps.Features
{
    /// <summary>
    /// Fonctionnalités liés aux objets.
    /// </summary>
    internal static class Objects
    {
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

                    var obj = DevObject.References.GetValueOrDefault(name);
                    if (obj != null)
                    {
                        DevObject.References.Remove(name);

                        foreach (var o in DevFacet.References)
                        {
                            if (!o.Value.Objects.ContainsKey(name))
                                continue;

                            o.Value.Objects.Remove(name);
                        }

                        obj.OnDelete();
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
        /// Ajoute un objet au projet
        /// </summary>
        /// <returns></returns>
        public static async Task<string> Create(string baseName)
        {
            string name = baseName;

            try
            {
                await DevObject._executeLock.WaitAsync();

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    DevObject.MakeUniqueName(ref name);
                    DevObjectInstance.Create(name, "", []);
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
            return name;
        }

        /// <summary>
        /// Copie le contenu du stream dans le contenu de l'objet
        /// </summary>
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
        public static async Task<bool> CopyFromFile(string name, string filename)
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
            return false;
        }

        /// <summary>
        /// Compare le contenu avec un fichier
        /// </summary>
        /// <returns>true si les contenus sont différent, null si ils ne peuvent être comparé</returns>
        public static async Task<bool?> IsDifferentFromFile(string name, string filename)
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

                        if (DevObject.References.TryGetValue(name, out var obj) && obj.Content != null)
                        {
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
                        {
                            Program.Logger.WriteLine($"L'objet \"{name}\" n'existe pas ou n'a pas de contenu");
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
            return null;
        }

        /// <summary>
        /// Charge le contenu en cache de tous les objets
        /// </summary>
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

                try
                {
                    await DevObject._checkLock.WaitAsync();

                    if (DevObject.TryGet(name, out var reference))
                    {
                        var newReference = reference.Clone();
                        DevObject.MakeUniqueName(ref name);
                        DevObject.References.Add(name, newReference);
                        return name;
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

            return null;
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
                        if (reference != null && reference is Program.DevObjectInstance instance)
                        {
                            if (instance.guid == null)
                                instance.guid = Guid.NewGuid();
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
                        if (reference.AsModel() && reference is DevObjectInstance instance)
                        {
                            // todo !! Attention pas de lock dans les appels des services SharedServices et LogServices !! 

                            if (SharedServices.ApplyAllObjects(p => p.Guid == instance.baseGuid, Program.CommonSharedPath, (dir, model) =>
                            {
                                // log les modifications
                                if (LogServices.LogDifference(model.content, instance, dir) == true)
                                {
                                    // actualise l'objet du modèle
                                    model.content.UpdateFrom(instance);
                                    return true;
                                }
                                return false;
                            }) == 0)
                            {
                                Program.Logger.WriteLine("Modèle introuvable pour l'objet " + name);
                            }
                        }
                    }
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
                            if (instance.baseGuid != null)
                            {
                                var list = new List<Serializer.DevObjectInstance>();
                                SharedServices.EnumerateObjects(p => p.Guid == instance.baseGuid, Program.CommonSharedPath, ref list);
                                if (list.Count == 1)
                                {
                                    if (MessageBox.Show($"Objet modèle trouvé: '{list[0].Description}'.\nVoulez-vous mettre à jour depuis la bibliothèque ?", "Avertissement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                                    {
                                        // Actualise l'objet
                                        instance.UpdateFrom(list[0].content);

                                        Program.DevObject.CompilObjects([instance]);
                                        Program.DevObject.Init();
                                        Program.DevObject.Build(Program.DevObject.References.Where(p => p.Key == name));
                                    }
                                }
                                else if (list.Count > 1)
                                {
                                    MessageBox.Show("Il existe plusieurs objets partagés possédant cet identifiant, veuillez corriger la bibliothèque.", "Avertissement", MessageBoxButton.OK);

                                    Program.Logger.WriteLine("Multiples objets partagés avec le GUID: " + instance.baseGuid);
                                    foreach (var item in list)
                                        Program.Logger.WriteLine("* " + item.Description);
                                    Program.Logger.WriteLine();
                                }
                                else if (list.Count == 0)
                                {
                                    MessageBox.Show("L'objet modèle est introuvable dans la bibliothèque.", "Avertissement", MessageBoxButton.OK);

                                    Program.Logger.WriteLine("Objet partagé introuvable avec le GUID: " + instance.baseGuid);
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
        }
    }
}
