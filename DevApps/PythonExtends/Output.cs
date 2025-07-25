using IronPython.Runtime;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace DevApps.PythonExtends
{
    /// <summary>
    /// Fournit une classe de base pour gérer les données d'un objet
    /// </summary>
    /// <remarks>
    /// Chaque objet possède un flux de données binaire permettant de stocker les informations généré ou importer.
    /// </remarks>
    public class Output
    {
        /// <summary>
        /// Flux mémoire en cours
        /// </summary>
        MemoryStream stream;
        /// <summary>
        /// utilisé pour optimiser les accès au contenu UTF8 dans une string .NET
        /// </summary>
        string? cachedText;
        /// <summary>
        /// Nom du fichier associé aux données persistantes
        /// </summary>
        internal string Filename;

        /// <summary>
        /// Stock les données mémoire en fichier
        /// </summary>
        internal void Flush()
        {
            using var file = File.Open(Filename, FileMode.OpenOrCreate);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(file);
        }

        /// <summary>
        /// Recharge les données du fichier en mémoire
        /// </summary>
        internal void Reload()
        {
            using var file = File.Open(Filename, FileMode.Open);
            stream.Seek(0, SeekOrigin.Begin);
            file.CopyTo(stream);
            stream.SetLength(file.Length);
        }

        public Output(MemoryStream stream, string filename)
        {
            this.stream = stream;
            Filename = filename;
        }

        /// <summary>
        /// Méthode Python: Remplace le contenu par un texte encodé UTF8
        /// </summary>
        public void write(string text)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                cachedText = text;
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(bytes);
                stream.SetLength(bytes.Length);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("write(): Failed to write in output");
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Méthode Python: Remplace le contenu par un texte encodé UTF8
        /// </summary>
        public void append(string text)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(text);
                var length = stream.Length;
                stream.Seek(0, SeekOrigin.End);
                stream.Write(bytes);
                cachedText = null;
                stream.SetLength(length + bytes.Length);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("append(): Failed to write in output");
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Méthode Python: Remplace le contenu par un texte encodé UTF8
        /// </summary>
        public void write_bytes(IronPython.Runtime.Bytes _bytes)
        {
            try
            {
                var bytes = _bytes.ToArray();
                cachedText = Encoding.UTF8.GetString(bytes);
                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(bytes);
                stream.SetLength(bytes.Length);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("write_bytes(): Failed to write in output");
                System.Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Méthode Python: Obtient les bytes du contenu
        /// </summary>
        public IronPython.Runtime.Bytes bytes()
        {
            return new IronPython.Runtime.Bytes(stream.ToArray());
        }

        /// <summary>
        /// Méthode .NET: Obtient les bytes du contenu
        /// </summary>
        public byte[] Bytes()
        {
            return stream.ToArray();
        }

        /// <summary>
        /// Méthode Python: Obtient la taille du contenu
        /// </summary>
        public long size()
        {
            return stream.Length;
        }

        /// <summary>
        /// Méthode Python: Parse le contenu en nombre (0 si échoue)
        /// </summary>
        public double number()
        {
            double val = 0;
            if (cachedText == null)
                cachedText = Encoding.UTF8.GetString(stream.ToArray());
            if(double.TryParse(cachedText, out val) == false)
                System.Console.WriteLine("number(): Failed to parse output to number");
            return val;
        }


        /// <summary>
        /// Méthode Python: Parse le contenu en texte (vide si échoue)
        /// </summary>
        public string text()
        {
            try
            {
                if (cachedText != null)
                    return cachedText;
                cachedText = Encoding.UTF8.GetString(stream.ToArray());
                //new IronPython.Runtime.PythonEnumerable.Create(stream.GetBuffer());
                return cachedText;
            }
            catch (Exception)
            {
                System.Console.WriteLine("text(): Failed to parse output to string");
                return string.Empty;
            }
        }

        /// <summary>
        /// Méthode Python: Parse le contenu en lignes de textes (tableau vide si échoue)
        /// </summary>
        /// <remarks>
        /// Supprime les caractères \n et \r
        /// </remarks>
        public string[] lines()
        {
            try
            {
                if (cachedText != null)
                    return Regex.Split(cachedText, "\r\n|\r|\n");
                cachedText = Encoding.UTF8.GetString(stream.ToArray());
                //new IronPython.Runtime.PythonEnumerable.Create(stream.GetBuffer());
                return Regex.Split(cachedText, "\r\n|\r|\n");
            }
            catch (Exception)
            {
                System.Console.WriteLine("lines(): Failed to parse output to string array");
                return [];
            }
        }

        /// <summary>
        /// Méthode Python: Parse le contenu en tableau de mots (tableau vide si échoue)
        /// </summary>
        /// <param name="columnsExp">RegEx représentant la séparation entre les mots</param>
        public string[] words(string columnsExp)
        {
            List<string> retval = new List<string>();

            var lines = this.lines();

            if (lines.Length == 0)
                return Array.Empty<string>();

            var reg = new Regex(columnsExp, RegexOptions.IgnoreCase);

            var results = lines.Select(p => reg.Match(p)).ToArray();

            var columns = results.Select(p => p.Groups.Values.Count() - 1).Max();

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < results.Length; j++)
                {
                    var text = results[j].Groups[1 + i].Value;
                    retval.Add(text);
                }
            }

            return retval.ToArray();
        }

        /// <summary>
        /// Flux de données interne
        /// </summary>
        internal MemoryStream Stream
        {
            get
            {
                return this.stream;
            }
        }
    }
}
