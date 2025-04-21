using Microsoft.Win32;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

internal partial class Program
{
    public class DevVariable
    {
        public class NoValue{
            public override string ToString()
            {
                return String.Empty;
            }
        }

        public static NoValue EmptyValue = new NoValue();

        public static DevVariable Create(string name, string desc)
        {
            var o = new DevVariable(desc, EmptyValue);
            References.Add(name, o);

            return o;
        }

        public static void Delete(string name)
        {
            mutexCheckVariableList.WaitOne();
            References.Remove(name);
            mutexCheckVariableList.ReleaseMutex();
        }

        internal static Mutex mutexCheckVariableList = new Mutex();

        public static Dictionary<string, DevVariable> References = new Dictionary<string, DevVariable>();


        public DevVariable()
        {
        }

        public DevVariable(string description, object value)
        {
            this.Description = description;
            this.Value = value;
        }

        /// <summary>
        /// trouve un nom unique
        /// </summary>
        /// <param name="name"></param>
        public static void MakeUniqueName(ref string name)
        {
            var newName = Program.RemoveDiacritics(name);
            int n = 2;

            var allowedChars = @"abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

            newName = newName.Replace(' ', '_');
            newName = newName.Replace('\t', '_');
            newName = newName.Replace('-', '_');

            newName = Regex.Replace(newName, "[^" + allowedChars + "]", "");

            while (References.ContainsKey(newName) || Program.Keywords.Contains(newName))
            {
                newName = name + n;
                n++;
            }

            name = newName;
        }

        public string Description{ get; set; } = String.Empty;
        public object Value { get; set; } = EmptyValue;
        public bool IsUsed { get; set; } = false;

        internal static bool DeletePrivate(string name)
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps\Variables";

                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(registryKey))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree(name, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }

            return false;
        }

        internal static bool SavePrivate(string name, DevVariable var, string? oldName = null)
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps\Variables";

                using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(registryKey))
                {
                    if (key != null)
                    {
                        key.DeleteSubKeyTree(oldName ?? name, false);

                        using (RegistryKey? subKey = key.CreateSubKey(name))
                        {
                            subKey.SetValue("description", var.Description);
                            subKey.SetValue(null, var.Value);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }

            return false;
        }

        internal static bool LoadPrivate(string name, out DevVariable var)
        {
            var = new DevVariable();
            try
            {
                var registryKey = @"SOFTWARE\DevApps\Variables";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(name))
                        {
                            if (subKey != null)
                            {
                                var.Description = subKey?.GetValue("description")?.ToString() ?? String.Empty;
                                var.Value = subKey?.GetValue(null) ?? EmptyValue;
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }

            return false;
        }

        internal static object GetPrivate(string name)
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps\Variables";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(name))
                        {
                            if (subKey != null)
                            {
                                return subKey?.GetValue(null) ?? EmptyValue;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }

            return EmptyValue;
        }

        internal static void SetPrivate(string name, object value)
        {
            try
            {
                var registryKey = @"SOFTWARE\DevApps\Variables";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        using (RegistryKey? subKey = key.OpenSubKey(name, true))
                        {
                            if (subKey != null)
                            {
                                subKey?.SetValue(null, value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }
        }

        internal static Dictionary<string, DevVariable> EnumPrivate()
        {
            Dictionary<string, DevVariable> list = new Dictionary<string, DevVariable>();

            try
            {
                var registryKey = @"SOFTWARE\DevApps\Variables";

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(registryKey))
                {
                    if (key != null)
                    {
                        foreach (string subKeyName in key.GetSubKeyNames())
                        {
                            var var = new DevVariable();

                            using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                            {
                                list.Add(subKeyName, new DevVariable
                                {
                                    Description = subKey?.GetValue("description")?.ToString() ?? String.Empty,
                                    Value = subKey?.GetValue(null) ?? EmptyValue
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erreur : " + ex.Message);
            }

            return list;
        }
    }
}
