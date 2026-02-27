using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;

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
        /// Flux mémoire en cours
        /// </summary>
        internal Stream stream;
        /// <summary>
        /// utilisé pour optimiser les accès au contenu UTF8 dans une string .NET
        /// </summary>
        internal string? cachedText;
        /// <summary>
        /// Nom du fichier associé aux données persistantes
        /// </summary>
        internal string Filename; // todo changer en nom d'objet et nullable ?
        internal string? objectName;
        /// <summary>
        /// True si le contenu du stream a changé
        /// </summary>
        public bool AsChanged = false;

        /// <summary>
        /// Méthode Python: True si le contenu est vide
        /// </summary>
        public Stream MemoryStream()
        {
            return stream;
        }

        /// <summary>
        /// Stock les données mémoire en fichier
        /// </summary>
        internal void Flush()//todo a supprimer ?
        {
            using var file = File.Open(Filename, FileMode.OpenOrCreate);
            stream.Seek(0, SeekOrigin.Begin);
            stream.CopyTo(file);
        }

        /// <summary>
        /// Recharge les données du fichier en mémoire
        /// </summary>
        internal void Reload()//todo a supprimer ?
        {
            using var file = File.Open(Filename, FileMode.Open);
            stream.Seek(0, SeekOrigin.Begin);
            file.CopyTo(stream);
            stream.SetLength(file.Length);

            AsChanged = true;
        }

        internal static List<Output>? Collector = null;

        /// <summary>
        /// Débute la collecte des instances Output créées
        /// </summary>
        internal static void BeginCollect()
        {
            Collector = new List<Output>();
        }

        /// <summary>
        /// Termine la collecte des instances Output créées
        /// </summary>
        /// <returns>Tableau des instances ayant retourné true à l'appel de action</returns>
        /// <remarks>
        /// action permet d'appliquer une action sur chaque instance créée
        /// </remarks>
        internal static Output[] EndCollect(Func<Output,bool>? action)
        {
            List<Output> retval = new List<Output>();

            if (Collector != null)
            {
                if (action != null)
                {
                    foreach (var instance in Collector)
                    {
                        if(action.Invoke(instance))
                            retval.Add(instance);
                    }
                }
                Collector.Clear();
            }

            Collector = null;

            return retval.ToArray();
        }

        public Output(Stream stream, string filename)
        {
            this.stream = stream;
            Filename = filename;
            objectName = Path.GetFileNameWithoutExtension(filename);
            Collector?.Add(this);
        }

        public void clear()
        {
            stream.Seek(0, SeekOrigin.Begin);
            stream.SetLength(0);
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

                AsChanged = true;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("write(): Failed to write in output");
                Program.Logger.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Méthode Python: Remplace le contenu
        /// </summary>
        public void write(Output content)
        {
            try
            {
                stream.Seek(0, SeekOrigin.Begin);
                content.stream.CopyTo(stream);
                stream.SetLength(content.stream.Length);
                stream.Seek(0, SeekOrigin.Begin);

                AsChanged = true;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("write(): Failed to write in output");
                Program.Logger.WriteLine(ex.Message);
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

                AsChanged = true;
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("append(): Failed to write in output");
                Program.Logger.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Méthode .NET: Obtient les bytes du contenu
        /// </summary>
        public byte[] Bytes()
        {
            byte[] bytes = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(bytes, 0, (int)stream.Length);
            stream.Seek(0, SeekOrigin.Begin); 
            return bytes;
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
                cachedText = Encoding.UTF8.GetString(Bytes());
            if(double.TryParse(cachedText, out val) == false)
                Program.Logger.WriteLine("number(): Failed to parse output to number");
            return val;
        }

        /// <summary>
        /// Méthode Python: True si le contenu est vide
        /// </summary>
        public bool isEmpty()
        {
            return !(Stream != null && Stream.Length > 0);
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

                using (var reader = new StreamReader(Stream, Encoding.UTF8, true, 1024, true))//encoding a détecter
                {
                    Stream.Position = 0;
                    cachedText = reader.ReadToEnd();
                    Stream.Position = 0;
                }

                return cachedText;
            }
            catch (Exception)
            {
                Program.Logger.WriteLine("text(): Failed to parse output to string");
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
                cachedText = Encoding.UTF8.GetString(Bytes());
                //new IronPython.Runtime.PythonEnumerable.Create(stream.GetBuffer());
                return Regex.Split(cachedText, "\r\n|\r|\n");
            }
            catch (Exception)
            {
                Program.Logger.WriteLine("lines(): Failed to parse output to string array");
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
        internal Stream Stream
        {
            get
            {
                return stream;
            }
        }
    }
}
