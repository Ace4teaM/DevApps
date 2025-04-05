using Microsoft.Win32;
using System.Diagnostics;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Bienvenue dans l'installation DevApps");

        try
        {
            // définit l'emplacement de ce programme
            try
            {
                var registryKey = @"Software\DevAppsSetup";

                using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(registryKey))
                {
                    if (key != null)
                    {
                        key.SetValue(null, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevAppsSetup.exe"));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Impossible d'enregistrer l'emplacement du fichier dans le registre");
                Console.WriteLine("Erreur : " + ex.Message);
                throw;
            }

            // obtient le chemin d'accès à l'installation
            string? pathToDevApps = null;
            try
            {
                var registryKey = @"Software\DevApps";

                using (RegistryKey? key = Registry.LocalMachine.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        pathToDevApps = key.GetValue(null, null)?.ToString();
                    }
                }

                if (pathToDevApps == null)
                {
                    Console.WriteLine("DevApps n'est pas installé ou n'est pas enregistré au registre de Windows");
                    ConsoleKey readKey = ConsoleKey.NoName;
                    do
                    {
                        Console.WriteLine("Voulez vous spécifier le chemin d'accès (Y/N) ?");
                        readKey = Console.ReadKey(true).Key;
                        if (readKey == ConsoleKey.Y)
                        {
                            Console.WriteLine("Entrez le chemin d'accès à l'installation de DevApps : ");
                            var path = Console.ReadLine();
                            if(Directory.Exists(path) && File.Exists(Path.Combine(path, "DevApps.exe")))
                            {
                                pathToDevApps = Path.Combine(path, "DevApps.exe");
                            }
                            else if (File.Exists(path) && Path.GetFileName(path) == "DevApps.exe")
                            {
                                pathToDevApps = path;
                            }
                            else
                            {
                                Console.WriteLine("Le chemin d'accès spécifié n'est pas valide");
                                continue;
                            }

                            using (RegistryKey? key = Registry.LocalMachine.CreateSubKey(registryKey))
                            {
                                if (key != null)
                                {
                                    key.SetValue(null, pathToDevApps);
                                }
                            }
                        }
                    } while (pathToDevApps == null && readKey != ConsoleKey.N);


                    if(pathToDevApps == null)
                    {
                        Console.WriteLine("Installation annulée");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            // ajoute le raccourci au menu contextuel de l'explorateur windows
            if (args.Contains("--add-shell"))
            {
                Console.Write("Ajout raccourci à l'explorateur windows... ");
                try
                {
                    var registryKey = @"Directory\Background\shell\DevApps";

                    using (RegistryKey? key = Registry.ClassesRoot.CreateSubKey(registryKey))
                    {
                        if (key != null)
                        {
                            key.SetValue(null, "Ouvrir avec DevApps");

                            using (RegistryKey? subKey = key.CreateSubKey(@"command"))
                            {
                                subKey.SetValue(null, String.Format("\"{0}\" \"%v\" -w", pathToDevApps));
                            }
                        }
                    }
                    Console.WriteLine("OK");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            Console.WriteLine("Fin de l'installation");
        }
        catch (Exception)
        {
            Console.WriteLine("Installation terminée avec des erreurs");
        }
        finally
        {
            Console.ReadKey();
        }
    }
}