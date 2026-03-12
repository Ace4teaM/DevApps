using IronPython.Runtime;
using System.IO;
using System.Text;

namespace DevApps.Scripts
{
    /// <summary>
    /// Fournit une interface pour manipuler un flux de données d'un objet (DevObject)
    /// </summary>
    /// <remarks>
    /// Chaque objet possède un flux de données binaire permettant de stocker les informations généré ou importer.
    /// </remarks>
    public partial class Output
    {
        /// <summary>
        /// Méthode Python: Remplace le contenu par un texte encodé UTF8
        /// </summary>
        public void write_bytes(Bytes _bytes)
        {
            try
            {
                var bytes = _bytes.ToArray();
                cachedText = Encoding.UTF8.GetString(bytes);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(bytes);
                stream.SetLength(bytes.Length);

                AsChanged = true;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("write_bytes(): Failed to write in output");
                Program.Logger.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Méthode Python: Obtient les bytes du contenu
        /// </summary>
        public Bytes bytes()
        {
            return new Bytes(Bytes());
        }
    }
}
