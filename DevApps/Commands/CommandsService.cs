using DevApps.Record;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;


namespace DevApps.Commands
{
    /// <summary>
    /// Ce service implémente un pattern de commandes utiliser à la fois pour historiser les actions et permettre l'annulation de celles-ci
    /// L'implémentation des actions est réalisé par Features, les commandes se place du point de vue de l'utilisateur final
    /// C'est a dire qu'une fonctionnalité va réaliser l'action métier et la commande s'assurer que l'action soit retranscrite à l'utilisateur dans l'UI et les journaux
    /// </summary>
    internal static class CommandsService
    {
        /// <summary>
        /// Déclare une méthode comme commande reversible
        /// </summary>
        internal class UserCommandAttribute : Attribute
        {
        }

        /// <summary>
        /// Termine le service distant
        /// </summary>
        internal static CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        /// <summary>
        /// Débute le service distant
        /// </summary>
        /// <returns></returns>
        internal static async Task Start()
        {
            while (Cancel.IsCancellationRequested == false)
            {
                using var server = new NamedPipeServerStream($"{Program.NamedPipePrefix}.commands");

                await server.WaitForConnectionAsync(Cancel.Token);

                using var reader = new StreamReader(server);
                using var writer = new StreamWriter(server);

                while (Cancel.IsCancellationRequested == false)
                {
                    var json = await reader.ReadLineAsync();

                    try
                    {
                        dynamic? obj = JsonConvert.DeserializeObject(json);

                        var methods = typeof(CommandsService).GetMethods(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                                .Where(m => m.GetCustomAttributes(typeof(UserCommandAttribute), false).Length > 0)
                                .ToArray();

                        if (obj != null)
                        {
                            obj.action = "test";

                            var m = methods.FirstOrDefault(m => m.Name.Equals((string)obj.action, StringComparison.OrdinalIgnoreCase));

                            var parameters = m.GetParameters();
                            var args = new List<object?>();
                            foreach (var param in parameters)
                            {
                                if (((IDictionary<string, object>)obj).TryGetValue(param.Name!, out var value))
                                {
                                    args.Add(Convert.ChangeType(value, param.ParameterType));
                                }
                                else
                                {
                                    args.Add(param.HasDefaultValue ? param.DefaultValue : null);
                                }
                            }

                            var result = m.Invoke(null, args.ToArray());
                            if (result is Task<string> taskResult)
                            {
                                writer.WriteLineAsync(taskResult.Result);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        writer.WriteLineAsync("Exception");
                    }
                }
            }
        }

        /// <summary>
        /// Termine le service distant
        /// </summary>
        internal static void Stop()
        {
            Cancel.Cancel();
        }

        /// <summary>
        ///  Enregistre les actions dans une transaction de l'historique annulable
        /// </summary>
        /// <example>
        ///    // Avec une série d'instruction dans le thread en cours
        ///    CommandsService.Record(
        ///         "move object",
        ///        () => {
        ///              using (DevFacet.Recorder.Rec(this.Name, this.facette))
        ///                    props.SetZone(new Rect(Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height));
        ///         }
        ///    );
        /// </example>
        internal static async Task<bool> Run(string title, Task task)
        {
            await HistoryServices.BeginTransaction();

            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }

            if (task.IsCompletedSuccessfully)
            {
                HistoryServices.Commit(title);
            }
            else
            {
                HistoryServices.Rollback();
            }

            return task.IsCompletedSuccessfully;
        }

        /// <summary>
        ///  Enregistre les actions dans le contexte de synchronisation
        /// </summary>
        /// <example>
        ///    // Avec un appel à une Feature (recommandé)
        ///    CommandsService.Record(
        ///         "move object",
        ///         DevApps.Features.Facets.MoveObject(this.Name, element.Name, Canvas.GetLeft(element), Canvas.GetTop(element), element.Width, element.Height)
        ///    ).Wait();
        /// </example>
        internal static async Task<bool> Run(string title, Action action)
        {
            await HistoryServices.BeginTransaction();

            bool success = false;
            try
            {
                action();
                success = true;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine(ex.Message);
            }

            if (success)
            {
                HistoryServices.Commit(title);
            }
            else
            {
                HistoryServices.Rollback();
            }

            return success;
        }

        //
        // Implémentation des commandes
        //

        [UserCommand, Description("Get object summary.")]
        internal static async Task<string> ObjectSummary(
            [Description("object name")] string name
            )
        {
            return await CommandResult.MakeJson(
                "Get object summary.",
                DevApps.Features.Objects.Summary(name),
                (ret) => new { Summary = ret, ObjectName = name }
            );
        }

        [UserCommand, Description("Get object data.")]
        internal static async Task<string> GetObjectData(
            [Description("object name")] string name
            )
        {
            return await CommandResult.MakeJson(
                $"Get object data.",
                DevApps.Features.Objects.GetData(name),
                (ret) => new { Data = ret, ObjectName = name }
            );
        }

        [UserCommand, Description("Get object names.")]
        internal static async Task<string> ListObjects()
        {
            return await CommandResult.MakeJson(
                $"Get object names.",
                DevApps.Features.Objects.GetNames(),
                (ret) => new { ObjectsNames = ret }
            );
        }

        [UserCommand, Description("Rename object.")]
        internal static async Task<string> RenameObject(
            [Description("object name")] string name,
            [Description("New object name")] string newName
            )
        {
            return await CommandResult.MakeJson(
                $"Rename object {name}.",
                DevApps.Features.Objects.Rename(name, newName),
                () => new { }
            );
        }

        [UserCommand, Description("Change object description.")]
        internal static async Task<string> ChangeObjectDescription(
            [Description("object name")] string name,
            [Description("New object description")] string description
            )
        {
            return await CommandResult.MakeJson(
                $"Change object {name} description.",
                DevApps.Features.Objects.SetDescription(name, description),
                (val) => new { }
            );
        }

        [UserCommand, Description("Change object tags.")]
        internal static async Task<string> ChangeObjectTags(
            [Description("object name")] string name,
            [Description("New object tags")] string tags
            )
        {
            return await CommandResult.MakeJson(
                $"Change object {name} tags.",
                DevApps.Features.Objects.SetTags(name, tags),
                (val) => new { }
            );
        }

        [UserCommand, Description("Create a new object.")]
        internal static async Task<string> CreateObject(
            [Description("Base name for object to create")] string name,
            [Description("object description")] string description,
            [Description("object tags")] string[] tags
            )
        {
            return await CommandResult.MakeJson(
                $"Create object {name}.",
                DevApps.Features.Objects.Create(name, description, tags),
                (ret) => new { CreatedObjectName = ret }
            );
        }

        [UserCommand, Description("Delete existing object.")]
        internal static async Task<string> DeleteObject(
            [Description("Name of object to delete")] string name
            )
        {
            return await CommandResult.MakeJson(
                $"Delete object {name}.",
                DevApps.Features.Objects.Delete(name),
                () => new { }
            );
        }

        [UserCommand, Description("Duplicate an existing object.")]
        internal static async Task<string> DuplicateObject(
            [Description("objet name to duplicate")] string name
            )
        {
            return await CommandResult.MakeJson(
                $"Duplicate object {name}.",
                DevApps.Features.Objects.Duplicate(name),
                (ret) => new { Name = ret }
            );
        }
    }
}
