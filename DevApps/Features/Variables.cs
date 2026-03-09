using DevApps.GUI;
using static Program;

namespace DevApps.Features
{
    /// <summary>
    /// Fonctionnalités liés aux variables.
    /// </summary>
    internal static class Variables
    {
        /// <summary>
        /// Crée une variable
        /// </summary>
        /// <returns></returns>
        public static async Task<string> Create(string baseName, string description) // todo ajouter la valeur par défaut
        {
            string name = baseName;

            try
            {
                await DevVariable._checkLock.WaitAsync();

                DevVariable.MakeUniqueName(ref name);
                var o = DevVariable.Create(name, description);
                using var rec = DevVariable.Recorder.New(name, o);
            }
            finally
            {
                DevVariable._checkLock.Release();
            }


            // actualise la vue de l'éditeur

            DevApps.GUI.GuiService.InvalidateVariables();

            return name;
        }

        /// <summary>
        /// Supprime une variable
        /// </summary>
        public static async Task Delete(string name)
        {
            try
            {
                await DevVariable._checkLock.WaitAsync();

                    if (DevVariable.TryGet(name, out var obj))
                    {
                        // supprime l'objet de références
                        using (DevVariable.Recorder.Rem(name, obj))
                            DevVariable.References.Remove(name);
                    }
                    else
                        throw new Exception($"La variable {name} n'existe pas");
            }
            finally
            {
                DevVariable._checkLock.Release();
            }

            // actualise la vue de l'éditeur

            DevApps.GUI.GuiService.InvalidateVariables();
        }

        /// <summary>
        /// Renomme une variable 
        /// </summary>
        public static async Task Rename(string name, string newName)
        {
            try
            {
                await DevVariable._checkLock.WaitAsync();

                if (DevVariable.TryGet(name, out var variable) == false)
                    throw new Exception($"La variable {name} n'existe pas");

                if (DevVariable.References.ContainsKey(newName) == true)
                    throw new Exception($"Le nom de variable {newName} est déjà utilisé");

                // remplace l'entrée dans les références
                using (DevVariable.Recorder.Mov(name, newName))
                {
                    DevVariable.References[newName] = variable;
                    DevVariable.References.Remove(name);
                }

                // renomme l'objet dans les references des autres objets
                try
                {
                    DevObject._checkLock.Wait();

                    foreach (var obj in Program.DevObject.References)
                    {
                        using (DevObject.Recorder.Mov(obj.Key, obj.Key))
                        {
                            foreach (var property in obj.Value.Properties.Where(p => p.Value.Item1.Contains(name)).ToArray())
                            {
                                property.Value.Item1.Replace(name, newName); // todo rechercher dans la syntaxe et non seulement le texte !
                                Program.Logger.WriteLine($"Renomme dans la propriété {obj.Key}.{property.Key} => {property.Value.Item1}");
                                //todo recompiler l'expression...
                            }

                            // todo idem pour les scripts ...
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
                DevVariable._checkLock.Release();
            }

            // actualise la vue de l'éditeur

            DevApps.GUI.GuiService.InvalidateVariables();
        }

        /// <summary>
        /// Définit la description d'une variable
        /// </summary>
        /// <param name="name">Nom de la variable</param>
        public static async Task<string> SetDescription(string name, string description)
        {
            try
            {
                await DevVariable._checkLock.WaitAsync();

                if (DevVariable.TryGet(name, out var reference))
                {
                    using (DevVariable.Recorder.Rec(name, reference))
                    {
                        var oldDescription = reference.Description;
                        reference.Description = description;

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateVariablesStatus();

                        return oldDescription;
                    }
                }
                else
                    throw new Exception($"La variable {name} n'existe pas");
            }
            finally
            {
                DevVariable._checkLock.Release();
            }
        }

        /// <summary>
        /// Définit la valeur d'une variable
        /// </summary>
        /// <param name="name">Nom de la variable</param>
        public static async Task<object> SetValue(string name, object? value)
        {
            try
            {
                await DevVariable._checkLock.WaitAsync();

                if (DevVariable.TryGet(name, out var reference))
                {
                    using (DevVariable.Recorder.Rec(name, reference))
                    {
                        var old = reference.Value.ToString();
                        reference.Value = DevVariable.Variant.Parse(value?.ToString());

                        // actualise la vue de l'éditeur
                        DevApps.GUI.GuiService.InvalidateVariablesStatus();

                        return old;
                    }
                }
                else
                    throw new Exception($"La variable {name} n'existe pas");
            }
            finally
            {
                DevVariable._checkLock.Release();
            }
        }
    }
}