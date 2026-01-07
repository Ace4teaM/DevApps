namespace DevApps.Scripts
{
    /// <summary>
    /// Fournit les méthodes d'accès à la console pour le langage de scripts
    /// </summary>
    public class Terminal
    {
        /// <summary>
        /// Méthode Python : Ecrit dans la sortie standard
        /// </summary>
        public void write(string text)
        {
            System.Console.WriteLine(text);
//            Program.Dispatcher.Invoke(new Action(() => { System.Console.WriteLine(text); }));
        }
    }
}
