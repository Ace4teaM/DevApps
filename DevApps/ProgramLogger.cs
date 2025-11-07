using ComponentAce.Compression.Libs.ZLib;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Serializer;
using System.IO;
using System.Text;

namespace DevApps
{
    /// <summary>
    /// Implémente la gestion du journal des événements de l'application.
    /// </summary>
    /// <remarks>
    /// Le journal des événements (log) permet de tracer les actions effectuées par l'application, facilitant ainsi le débogage et l'analyse des performances.
    /// Mais il permet aussi de conserver un historique des opérations réalisées, utile pour l'audit et la récupération de données.
    /// </remarks>
    internal class ProgramLogger : System.IO.TextWriter
    {
        public class RedirectStream : Stream
        {
            internal required ProgramLogger logger;

            public override bool CanRead => false;
            public override bool CanSeek => false;
            public override bool CanWrite => true;

            public override long Length => 0;

            public override long Position
            {
                get => 0;
                set{}
            }

            public override void Flush()
            {
                // Rien à faire ici (pas de buffer externe)
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return 0;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                try
                {
                    var text = Encoding.UTF8.GetString(buffer, offset, count);
                    logger.Write(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return 0;
            }

            public override void SetLength(long value)
            {
            }

            public byte[] ToArray() => Array.Empty<byte>();
        }

        internal static ProgramLogger Instance { get; } = new ProgramLogger();

        public static string LogFile => ".devapps.log";

        internal FileStream writer = new FileStream(LogFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        internal FileStream reader = new FileStream(LogFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite);
        internal RedirectStream redirect;

        public override Encoding Encoding => Encoding.UTF8;

        // Événement optionnel pour suivre l'écriture
        public event EventHandler<string>? TextWritten;

        public ProgramLogger()
        {
            redirect = new RedirectStream() { logger = this };
        }

        public bool ReadNext(out string line)
        {
            StringBuilder sb = new StringBuilder();
            int c = 0;
            while ((c= reader.ReadByte()) != -1)
            {
                if (c == '\n')
                {
                    line = sb.ToString();
                    return sb.Length > 0;
                }
                sb.Append((char)c);
            }
            line = sb.ToString();
            return sb.Length > 0;
        }

        public override string ToString()
        {
            reader.Seek(0, SeekOrigin.Begin);
            StringBuilder sb = new StringBuilder();
            while (ReadNext(out var line))
                sb.AppendLine(line);
            return sb.ToString();
        }

        public override void Write(char value)
        {
            try
            {
                writer.Write(Encoding.UTF8.GetBytes(value.ToString()));
                writer.Flush();
            }
            catch { }
            TextWritten?.Invoke(this, value.ToString());
        }

        public override void Write(string? value)
        {
            if (value != null)
            {
                try
                {
                    writer.Write(Encoding.UTF8.GetBytes(value.ToString()));
                    writer.Flush();
                }
                catch { }
                TextWritten?.Invoke(this, value);
            }
        }

        public override void WriteLine(string? value)
        {
            if (value != null)
            {
                try
                {
                    writer.Write(Encoding.UTF8.GetBytes(value.ToString() + '\n'));
                    writer.Flush();
                }
                catch { }
                TextWritten?.Invoke(this, value);
            }
        }
        public void Print(params object[] values)
        {
            foreach (var value in values)
            {
                Write(value.ToString());
            }
            Write(Environment.NewLine);
        }
        public void Backup(string key, Program.DevObject obj)
        {
            try
            {
                var currentFilename = Path.Combine(Program.DataDir, key);

                // Si aucune différence, ne rien faire
                if (obj.Content.IsDifferent(currentFilename))
                {
                    var index = 1;

                    var backupFilename = currentFilename + $".bak.{index}";

                    if (File.Exists(backupFilename))
                    {
                        do
                        {
                            index++;
                            backupFilename = currentFilename + $".bak.{index}";
                        } while (File.Exists(backupFilename));
                    }

                    var filename = Path.Combine(Program.DataDir, key + index);

                    obj.FlushContent();

                    File.Copy(currentFilename, backupFilename, true);

                    WriteLine($"Backup {key} to {backupFilename}");
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }
    }
}
