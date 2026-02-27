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
        /// Ajoute un objet au projet
        /// </summary>
        /// <returns></returns>
        public static DevObjectInstance Create(out string name)
        {
            name = "NewObject";
            DevObject.MakeUniqueName(ref name);
            return DevObjectInstance.Create(name, "", []);
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
        public static string Duplicate(string name)
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                Program.DevObject.References.TryGetValue(name, out var reference);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                if (reference != null)
                {
                    var newReference = reference.Clone();
                    Program.DevObject.MakeUniqueName(ref name);
                    Program.DevObject.References.Add(name, newReference);
                    return name;
                }
            }

            return string.Empty;
        }
        /// <summary>
        /// Définit l'objet comme modèle en lui attribuant un GUID unique.
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static void SetAsModel(string name)
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                Program.DevObject.References.TryGetValue(name, out var reference);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                if (reference != null && reference is Program.DevObjectInstance inst)
                {
                    if (inst.guid == null)
                        inst.guid = Guid.NewGuid();
                }
            }
        }

        /// <summary>
        /// Met à jour le modèle depuis un objet du projet.
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static void UpdateModel(string name)
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                try
                {
                    if (DevObject.References.TryGetValue(name, out var reference))
                    {
                        // Si l'objet possède un modèle
                        if (reference.AsModel() && reference is DevObjectInstance instance)
                        {
                            // obtient le modèle de référence...
                            var handle2 = reference.mutexReadOutput.WaitOne();
                            if (handle2)
                            {
                                try
                                {
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
                                catch (Exception ex)
                                {
                                    Program.Logger.WriteLine(ex.Message);
                                }
                                finally
                                {
                                    reference.mutexReadOutput.ReleaseMutex();
                                }
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
                    Program.DevObject.mutexCheckObjectList.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// Met à jour un objet depuis son modèle de la bibliothèque partagée.
        /// </summary>
        /// <param name="name">Nom de l'objet</param>
        public static void UpdateFromModel(string name)
        {
            var handle = Program.DevObject.mutexCheckObjectList.WaitOne();
            if (handle)
            {
                Program.DevObject.References.TryGetValue(name, out var reference);
                Program.DevObject.mutexCheckObjectList.ReleaseMutex();

                if (reference != null && reference is Program.DevObjectInstance inst)
                {
                    if (inst.baseGuid != null)
                    {
                        var list = new List<Serializer.DevObjectInstance>();
                        SharedServices.EnumerateObjects(p => p.Guid == inst.baseGuid, Program.CommonSharedPath, ref list);
                        if (list.Count == 1)
                        {
                            if (MessageBox.Show($"Objet modèle trouvé: '{list[0].Description}'.\nVoulez-vous mettre à jour depuis la bibliothèque ?", "Avertissement", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                            {
                                // Actualise l'objet
                                var handle2 = Program.DevObject.mutexExecuteObjects.WaitOne();
                                if (handle2)
                                {
                                    inst.UpdateFrom(list[0].content);
                                    Program.DevObject.mutexExecuteObjects.ReleaseMutex();
                                }

                                Program.DevObject.CompilObjects([inst]);
                                Program.DevObject.Init();
                                Program.DevObject.Build(Program.DevObject.References.Where(p => p.Key == name));
                            }
                        }
                        else if (list.Count > 1)
                        {
                            MessageBox.Show("Il existe plusieurs objets partagés possédant cet identifiant, veuillez corriger la bibliothèque.", "Avertissement", MessageBoxButton.OK);

                            Program.Logger.WriteLine("Multiples objets partagés avec le GUID: " + inst.baseGuid);
                            foreach (var item in list)
                                Program.Logger.WriteLine("* " + item.Description);
                            Program.Logger.WriteLine();
                        }
                        else if (list.Count == 0)
                        {
                            MessageBox.Show("L'objet modèle est introuvable dans la bibliothèque.", "Avertissement", MessageBoxButton.OK);

                            Program.Logger.WriteLine("Objet partagé introuvable avec le GUID: " + inst.baseGuid);
                        }
                    }
                }
            }
        }
    }
}
