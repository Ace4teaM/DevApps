using DevApps.Record;
using Newtonsoft.Json;


namespace DevApps.Commands
{
    /// <summary>
    /// Format de résultat d'une commande utilisateur
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Execute une commande sans valeur de retour et retourne le résultat
        /// </summary>
        /// <param name="task">action à executer</param>
        /// <param name="val">résultat de la commande</param>
        /// <param name="undo">commande inverse</param>
        /// <returns></returns>
        public static async Task<string> MakeJson(string title, Task task, Func<object?> val)
        {
            await HistoryServices.BeginTransaction();

            var result = new CommandResult();
            try
            {
                await task;
                result.Success = task.IsCompletedSuccessfully;
                result.Data = val();
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new CommandResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            // si possibilité d'annuler ajout à l'historique
            if (result.Success)
            {
                HistoryServices.Commit(title);
            }
            else
            {
                HistoryServices.Rollback();
            }

            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
        /// <summary>
        /// Execute une commande avec valeur de retour et retourne le résultat
        /// </summary>
        /// <param name="task">action à executer</param>
        /// <param name="val">résultat de la commande</param>
        /// <param name="undo">commande inverse</param>
        /// <returns></returns>
        public static async Task<string> MakeJson<T>(string title, Task<T> task, Func<T, object?> val)
        {
            await HistoryServices.BeginTransaction();

            var result = new CommandResult();
            try
            {
                var data = await task;
                result.Success = task.IsCompletedSuccessfully;
                result.Data = val(data);
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new CommandResult
                {
                    Success = false,
                    Message = ex.Message
                });
            }

            // si possibilité d'annuler ajout à l'historique
            if (result.Success)
            {
                HistoryServices.Commit(title);
            }
            else
            {
                HistoryServices.Rollback();
            }

            return JsonConvert.SerializeObject(result);
        }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; } // json
    }

}
