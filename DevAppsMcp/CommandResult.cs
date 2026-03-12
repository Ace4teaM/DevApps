namespace DevAppsMcp
{
    /// <summary>
    /// Format de résultat d'une commande utilisateur
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; } // json
    }

}
