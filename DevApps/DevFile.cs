using DevApps.GUI;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows;

internal partial class Program
{
    /// <summary>
    /// Moniteur de changement de fichier sous Windows
    /// </summary>
    internal class FileChangeNotification
    {
        private const uint FILE_NOTIFY_CHANGE_FILE_NAME = 0x00000001;
        private const uint FILE_NOTIFY_CHANGE_LAST_WRITE = 0x00000010;

        private const uint INFINITE = 0xFFFFFFFF;
        private const uint WAIT_OBJECT_0 = 0;
        private const uint WAIT_TIMEOUT = 0x00000102;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr FindFirstChangeNotification(
            string lpPathName,
            bool bWatchSubtree,
            uint dwNotifyFilter);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FindNextChangeNotification(IntPtr hChangeHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool FindCloseChangeNotification(IntPtr hChangeHandle);

        [DllImport("kernel32.dll")]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        internal string path;
        internal CancellationToken cancel;

        public FileChangeNotification(string path, CancellationToken cancel)
        {
            this.path = Path.GetFullPath(path);
            this.cancel = cancel;
        }

        public void Wait(Func<int> callback)
        {
            var dirname = Path.GetDirectoryName(path);
            var filename = Path.GetFileName(path);

            if (dirname == null || filename == null)
                return;

            var lastChangeDate = File.GetLastWriteTime(path);

            IntPtr handle = FindFirstChangeNotification(dirname, false, FILE_NOTIFY_CHANGE_FILE_NAME | FILE_NOTIFY_CHANGE_LAST_WRITE);

            if (handle == IntPtr.Zero || handle == new IntPtr(-1))
            {
                Program.Logger.WriteLine("Erreur : " + Marshal.GetLastWin32Error());
                return;
            }

            Program.Logger.WriteLine($"Surveillance de : {path}");

            while (cancel.IsCancellationRequested == false)
            {
                uint result = WaitForSingleObject(handle, 5000);
                if (result == WAIT_OBJECT_0 || result == WAIT_TIMEOUT)
                {
                    var newDate = File.GetLastWriteTime(path);

                    if (newDate > lastChangeDate)
                    {
                        Application.Current.Dispatcher.Invoke(() => {
                            callback.Invoke();
                        });
                        lastChangeDate = newDate;
                    }

                    // Reset pour continuer à écouter
                    if (!FindNextChangeNotification(handle))
                    {
                        Program.Logger.WriteLine("Erreur lors de FindNextChangeNotification");
                        break;
                    }
                }
            }

            FindCloseChangeNotification(handle);

            Program.Logger.WriteLine($"Fin de la surveillance de {path}");
        }
    }

    /// <summary>
    /// Représente un lien avec un fichier du projet final
    /// </summary>
    public class DevFile : IDisposable
    {

        public static Dictionary<string, DevFile> References = new Dictionary<string, DevFile>();

        /// <summary>
        /// Bloque l'accès à la liste References
        /// </summary>
        internal static Mutex mutexCheckList = new Mutex();

        internal string filename;
        /// <summary>
        /// Nom relatif du fichier final
        /// </summary>
        public string Filename { get { return filename; } }

        /// <summary>
        /// Nom de l'objet recevant les modifications
        /// </summary>
        internal string objectname;
        public string ObjectName { get { return objectname; } }

        /// <summary>
        /// Thread d'execution du ChangeNotification
        /// </summary>
        internal Thread? fileChangeNotificationThread;
        internal FileChangeNotification? notifyChange;
        internal CancellationTokenSource? cancelNotifyChange;

        public DevFile(string filename, string objectname)
        {
            this.filename = filename;
            this.objectname = objectname;
            this.cancelNotifyChange = new CancellationTokenSource();
            this.notifyChange = new FileChangeNotification(filename, cancelNotifyChange.Token);

            if (Path.GetFullPath(filename).StartsWith(Environment.CurrentDirectory) == false)
                throw new Exception("Accès aux fichiers en dehors du répertoire de travail interdit");

            fileChangeNotificationThread = new Thread(() =>
            {
                this.notifyChange.Wait(() => {
                    // lit les modifications
                    Program.Logger.WriteLine($"Read file modification... {this.filename}");
                    if (Read())
                    {
                        // reconstruit l'objet
                        if(DevObject.TryGet(this.objectname, out var obj) == false)
                        {
                            Program.Logger.WriteLine($"Object {this.objectname} not found !");
                            return 0;
                        }

                        Program.Logger.WriteLine($"Rebuild {this.objectname}");
                        DevObject.BuildTree(new KeyValuePair<string, DevObject>(this.objectname, obj));

                        if (GuiService.IsInitialized && GuiService.IsObjectsView)
                        {
                            GuiService.InvalidateObjectsStatus();
                        }
                        if (GuiService.IsInitialized && GuiService.IsFacetsView)
                        {
                            GuiService.Invalidate(this.objectname);
                        }
                    }
                    return 0;
                });
            })
            { IsBackground = true }; // 💡 Permet de ne pas bloquer l'arrêt du programme

            fileChangeNotificationThread.Start();
        }

        /// <summary>
        /// Lit le contenu du fichier et met à jour l'objet lié
        /// </summary>
        public bool Read()
        {
            try
            {
                using (FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var handle = DevObject.mutexExecuteObjects.WaitOne();
                    if (handle && DevObject.References.TryGetValue(objectname, out var obj))
                    {
                        // copie le contenu
                        obj.Content.Position = 0;
                        fileStream.CopyTo(obj.Content);
                        obj.Content.SetLength(fileStream.Length);
                        obj.Content.Position = 0;

                        DevObject.mutexExecuteObjects.ReleaseMutex();

                        return true;
                    }
                    else
                    {
                        if(DevObject.References.ContainsKey(objectname) == false)
                            Program.Logger.WriteLine($"L'objet \"{objectname}\" n'existe pas");
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine($"Read file error to {filename}");
                Program.Logger.WriteLine(ex.Message);
            }

            return false;
        }

        /// <summary>
        /// Compare les différences entre le fichier et l'objet
        /// </summary>
        /// <returns></returns>
        public bool Diff()
        {
            var handle = DevObject.mutexExecuteObjects.WaitOne();
            if (handle)
            {
                try
                {
                    if (DevObject.References.TryGetValue(objectname, out var obj) && obj.Content != null)
                    {
                        var fi1 = new FileInfo(filename);

                        if (fi1.Length != obj.Content.Length)
                            return true;

                        obj.Content.Position = 0;

                        using var sha256 = SHA256.Create();
                        using var fs1 = File.OpenRead(filename);

                        var hash1 = sha256.ComputeHash(fs1);
                        var hash2 = sha256.ComputeHash(obj.Content);

                        obj.Content.Position = 0;

                        return hash1.SequenceEqual(hash2) == false;
                    }
                    else
                    {
                        if (DevObject.References.ContainsKey(objectname) == false)
                            Program.Logger.WriteLine($"L'objet \"{objectname}\" n'existe pas ou n'a pas de contenu");
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.WriteLine(ex.Message);
                }
                finally
                {
                    DevObject.mutexExecuteObjects.ReleaseMutex();
                }
            }
            return false;
        }

        /// <summary>
        /// Ecrit le contenu de l'objet mis à jour dans le fichier lié
        /// </summary>
        public void Write()
        {
            try
            {
                using (FileStream fileStream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite))
                {
                    var handle = DevObject.mutexExecuteObjects.WaitOne();
                    if (handle && DevObject.References.TryGetValue(objectname, out var obj))
                    {
                        // copie le contenu
                        obj.Content.Position = 0;
                        obj.Content.CopyTo(fileStream);
                        fileStream.SetLength(obj.Content.Length);

                        DevObject.mutexExecuteObjects.ReleaseMutex();
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine($"Write file error to {filename}");
                Program.Logger.WriteLine(ex.Message);
            }
        }

        public void Dispose()
        {
            if(cancelNotifyChange != null)
                cancelNotifyChange.Cancel();

            //todo attendre la fin du thread ?
        }
    }
}
