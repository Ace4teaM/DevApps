using Microsoft.Win32;
using System.Globalization;
using System.Text.RegularExpressions;

internal partial class Program
{
    public class DevVariable
    {
        public readonly struct Variant
        {
            private readonly VariantType type;

            private readonly long longValue;
            private readonly double doubleValue;
            private readonly string? stringValue;

            public static readonly Variant Empty = default;

            public static implicit operator Variant(int v) => new Variant(v);
            public static implicit operator Variant(double v) => new Variant(v);
            public static implicit operator Variant(string v) => new Variant(v);

            public Variant(int value)
            {
                type = VariantType.Int;
                longValue = value;
                doubleValue = 0;
                stringValue = null;
            }

            public Variant(long value)
            {
                type = VariantType.Long;
                longValue = value;
                doubleValue = 0;
                stringValue = null;
            }

            public Variant(double value)
            {
                type = VariantType.Double;
                doubleValue = value;
                longValue = 0;
                stringValue = null;
            }

            public Variant(string value)
            {
                type = VariantType.String;
                stringValue = value;
                longValue = 0;
                doubleValue = 0;
            }

            public static Variant Parse(string? text)
            {
                if (text == null)
                {
                    return Variant.Empty;
                }

                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                {
                    return new Variant(i);
                }

                if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    return new Variant(l);
                }

                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                {
                    return new Variant(d);
                }

                return new Variant(text);
            }

            public bool IsEmpty => type == VariantType.Empty;

            public int AsInt() => (int)longValue;

            public long AsLong() => longValue;

            public double AsDouble()
            {
                return type switch
                {
                    VariantType.Int => longValue,
                    VariantType.Long => longValue,
                    VariantType.Double => doubleValue,
                    _ => throw new InvalidOperationException()
                };
            }

            public string? AsString() => stringValue;

            public override string ToString()
            {
                return type switch
                {
                    VariantType.Int => longValue.ToString(),
                    VariantType.Long => longValue.ToString(),
                    VariantType.Double => doubleValue.ToString(),
                    VariantType.String => stringValue ?? "",
                    _ => ""
                };
            }
        }

        public enum VariantType
        {
            Empty,
            Int,
            Long,
            Double,
            String
        }

        public static readonly DevVariable NullVariable = new DevVariable();

        public static bool TryGet(string name, out DevVariable var)
        {
            var = References.GetValueOrDefault(name) ?? NullVariable;
            return var != NullVariable;
        }

        public static DevVariable Create(string name, string desc)
        {
            var o = new DevVariable(desc, Empty);
            References.Add(name, o);

            return o;
        }

        public static void Delete(string name)
        {
            References.Remove(name);
        }

        /// <summary>
        /// Synchronise l'accès à la liste (References)
        /// </summary>
        internal static readonly SemaphoreSlim _checkLock = new(1, 1);

        /// <summary>
        /// Enregistreur d'états des objets (pour l'historisation, l'annulation, la duplication, ...)
        /// </summary>
        public static DevApps.Record.Recorder<string, Serializer.DevVariable, Program.DevVariable> Recorder = new();

        public static Dictionary<string, DevVariable> References = new Dictionary<string, DevVariable>();

        public static readonly Variant Empty = default;

        public DevVariable()
        {
        }

        public DevVariable(string description, object? value)
        {
            this.Description = description;
            this.Value = Variant.Parse(value?.ToString());
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
        public Variant Value { get; set; } = Empty;
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
                Program.Logger.WriteLine("Erreur : " + ex.Message);
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
                Program.Logger.WriteLine("Erreur : " + ex.Message);
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
                                var.Value = Variant.Parse(subKey?.GetValue(null)?.ToString());
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("Erreur : " + ex.Message);
            }

            return false;
        }

        internal static Variant GetPrivate(string name)
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
                                return Variant.Parse(subKey?.GetValue(null)?.ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("Erreur : " + ex.Message);
            }

            return Empty;
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
                Program.Logger.WriteLine("Erreur : " + ex.Message);
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

                            using (RegistryKey? subKey = key.OpenSubKey(subKeyName))
                            {
                                list.Add(subKeyName, new DevVariable
                                {
                                    Description = subKey?.GetValue("description")?.ToString() ?? String.Empty,
                                    Value = Variant.Parse(subKey?.GetValue(null)?.ToString())
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Logger.WriteLine("Erreur : " + ex.Message);
            }

            return list;
        }

        /// <summary>
        /// True si le type de variable peut être utilisé dans les scripts natifs
        /// </summary>
        internal static bool IsCompatible(object variable)
        {
            return true;
        }
    }
}
