
using DevApps.Features;
using DevApps.GUI;
using System.Diagnostics;
using System.Linq;

namespace DevApps.Record
{
    /// <summary>
    /// Gère l'historique des modifications pour le modèle métier, avec possibilité de validation (commit) ou d'annulation (rollback).
    /// </summary>
    internal static class HistoryServices
    {
        /// <summary>
        /// points de restaurations (marque les dates de fin de transaction) et leur description
        /// </summary>
        /// <remarks>
        /// les actions se trouvent toujours entre les points de restauration (Recorder)
        /// après une transaction réussie, un nouveau point de restauration est ajouté avec la date de fin de transaction
        /// il n'y a donc jamais d'action historisé après le dernier point de restauration dans la liste (sauf pendant une transaction en cours (actions en cours d'historisation))
        /// </remarks>
        internal static SortedList<DateTime, string> history = new();
        /// <summary>
        /// point de restauration actuel (0 par défaut)
        /// </summary>
        internal static int currentIndex = 0;
        /// <summary>
        /// date de début de la transaction en cours (null si aucune transaction en cours)
        /// </summary>
        internal static DateTime? transaction;
        /// <summary>
        /// verrou pour synchroniser les transactions et éviter les conflits d'historisation en cas de modifications simultanées (ex: plusieurs commandes exécutées en même temps)
        /// </summary>
        static SemaphoreSlim transactionLock = new(1, 1);

        /// <summary>
        /// premier point de restauration dans le passé
        /// </summary>
        static HistoryServices()
        {
            history.Add(Process.GetCurrentProcess().StartTime, "Initial state");
        }

        /// <summary>
        /// Indique le début de l'historisation
        /// </summary>
        internal static async Task BeginTransaction()
        {
            /// attend que la transaction en cours soit terminée
            await transactionLock.WaitAsync();

            // supprime les points de restauration suivants
            if (history.Count > currentIndex + 1)
            {
                var fromDate = history.ElementAt(currentIndex).Key;

                foreach (var obj in Program.DevObject.Recorder.records.Where(p => p.Key > fromDate).ToArray())
                    Program.DevObject.Recorder.records.Remove(obj.Key);

                foreach (var obj in Program.DevFacet.Recorder.records.Where(p => p.Key > fromDate).ToArray())
                    Program.DevFacet.Recorder.records.Remove(obj.Key);

                foreach (var obj in Program.DevVariable.Recorder.records.Where(p => p.Key > fromDate).ToArray())
                    Program.DevVariable.Recorder.records.Remove(obj.Key);

                for (int i = currentIndex + 1; i < history.Count; i++)
                {
                    history.RemoveAt(history.Count-1);
                }
            }

            // nouvelle date de début de transaction
            transaction = DateTime.Now;
        }
        /// <summary>
        /// Valide l'historique en cours et recommence une nouvelle transaction
        /// </summary>
        internal static void Commit(string message)
        {
            if (transaction != null)
            {
                history.Add(DateTime.Now, message);
                currentIndex = history.Count - 1;
                transaction = null;
                transactionLock.Release();
            }
        }

        internal static void Rollback()
        {
            if (transaction != null)
            {
                // annule toutes les modifications après la date de la dernière transaction réussite
                var last = history.Last().Key;

                try
                {
                    Program.Logger.WriteLine($"Rollback from >= {last}");

                    var objs = Program.DevObject.Recorder.Restore(Program.DevObject.References, last, transaction.Value);
                    var facets = Program.DevFacet.Recorder.Restore(Program.DevFacet.References, last, transaction.Value);
                    var vars = Program.DevVariable.Recorder.Restore(Program.DevVariable.References, last, transaction.Value);
                    //var files = Program.DevObjectFile.Recorder.Restore(Program.DevObjectFile.References, last, transaction.Value);

                    foreach (var obj in objs)
                        Program.DevObject.Recorder.records.Remove(obj.Key);

                    foreach (var facet in facets)
                        Program.DevFacet.Recorder.records.Remove(facet.Key);

                    foreach (var @var in vars)
                        Program.DevVariable.Recorder.records.Remove(@var.Key);
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                //fin de transaction
                transaction = null;
                transactionLock.Release();
            }
        }

        internal static void Undo()
        {
            if (currentIndex > 0)
            {
                var current = history.GetKeyAtIndex(currentIndex);
                var previousDate = history.GetKeyAtIndex(currentIndex-1);
                var label = history.GetValueAtIndex(currentIndex);

                try
                {
                    Program.Logger.WriteLine($"Undo to {previousDate} -> {label}");

                    var objCount = Program.DevObject.Recorder.Restore(Program.DevObject.References, previousDate, current).Count();
                    var facCount = Program.DevFacet.Recorder.Restore(Program.DevFacet.References, previousDate, current).Count();
                    var varCount = Program.DevVariable.Recorder.Restore(Program.DevVariable.References, previousDate, current).Count();
                    //Program.DevObjectFile.Recorder.Restore(Program.DevObjectFile.References, previousDate, current).Count();

                    // invalide la vue en cours
                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is IInvalidableView view)
                        {
                            view.InvalidateContent();
                        }
                    });

                    // invalide la liste des facettes
                    if (facCount > 0)
                        GUI.GuiService.InvalidateFacets();

                    currentIndex--;
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
            }
        }

        internal static void Redo()
        {
            if (currentIndex < history.Count - 1)
            {
                var current = history.GetKeyAtIndex(currentIndex);
                var nextDate = history.GetKeyAtIndex(currentIndex + 1);
                var label = history.GetValueAtIndex(currentIndex + 1);

                try
                {
                    Program.Logger.WriteLine($"Redo to {nextDate} -> {label}");

                    var objCount = Program.DevObject.Recorder.Apply(Program.DevObject.References, current, nextDate).Count();
                    var facCount = Program.DevFacet.Recorder.Apply(Program.DevFacet.References, current, nextDate).Count();
                    var varCount = Program.DevVariable.Recorder.Apply(Program.DevVariable.References, current, nextDate).Count();
                    //Program.DevObjectFile.Recorder.Apply(Program.DevObjectFile.References, current, nextDate).Count();

                    // invalide la vue en cours
                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is IInvalidableView view)
                        {
                            view.InvalidateContent();
                        }
                    });

                    // invalide la liste des facettes
                    if (facCount > 0)
                        GUI.GuiService.InvalidateFacets();

                    currentIndex++;
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
            }
        }
    }
}
