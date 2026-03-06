
using DevApps.GUI;
using System.Diagnostics;

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
                //todo : restaurer l'état du modèle métier à la date de début de transaction
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

                    int count = 0;

                    count += Program.DevObject.Recorder.Restore(Program.DevObject.References, previousDate, current);
                    count += Program.DevFacet.Recorder.Restore(Program.DevFacet.References, previousDate, current);
                    count += Program.DevVariable.Recorder.Restore(Program.DevVariable.References, previousDate, current);
                    //Program.DevObjectFile.Recorder.Restore(Program.DevObjectFile.References, previousDate);

                    // invalide la vue en cours
                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is IInvalidableView view)
                        {
                            view.InvalidateContent();
                        }
                    });

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

                    int count = 0;

                    count += Program.DevObject.Recorder.Apply(Program.DevObject.References, current, nextDate);
                    count += Program.DevFacet.Recorder.Apply(Program.DevFacet.References, current, nextDate);
                    count += Program.DevVariable.Recorder.Apply(Program.DevVariable.References, current, nextDate);
                    //Program.DevObjectFile.Recorder.Apply(Program.DevObjectFile.References, previousDate);

                    // invalide la vue en cours
                    DevApps.GUI.GuiService.EditorWindow?.Dispatcher.Invoke(() =>
                    {
                        if (DevApps.GUI.GuiService.EditorWindow?.Content is IInvalidableView view)
                        {
                            view.InvalidateContent();
                        }
                    });

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
