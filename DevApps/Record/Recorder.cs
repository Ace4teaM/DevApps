using Newtonsoft.Json;
using Serializer;

namespace DevApps.Record
{
    internal interface IRecord
    {

    }
    /// <summary>
    /// Classe générique pour sauvegarder / restaurer des objets sérialisables
    /// </summary>
    /// <typeparam name="K">Type de la clé</typeparam>
    /// <typeparam name="T">Type de l'objet sérialisable (Serializer.xxx)</typeparam>
    internal class Recorder<K, T, I> where T : ISerialisable, new()
    {
        //IDictionary<K, T> collection;
        internal SortedList<DateTime, IRecord> records = new();

        public class Insert : IDisposable, IRecord
        {
            internal static JsonSerializerSettings settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };

            /// <summary>
            /// Clone est conserve le type original Serializer.DevObjectInstance ou Serializer.DevObjectReference et non T (Serializer.DevObject)
            /// </summary>
            object Clone(object obj)
            {
                var json = JsonConvert.SerializeObject(obj, settings);
                return JsonConvert.DeserializeObject(json, obj.GetType(), settings);
            }

            internal required Recorder<K, T, I> _recorder;
            internal K _key;
            internal T _after; // après
            internal T _object; // objet en cours

            public Insert(K key, T obj)
            {
                _object = obj;
                _key = key;
            }

            public void Dispose()
            {
                _after = (T)Clone(_object);

                //if (!Equals(_before, after))//empêche la detection d'élément supprimé de la collection
                {
                    Program.Logger.WriteLine($"\nNew <{_key}>\nwith {_after.Content.GetType().Name}\n{_after}");
                    _recorder.records.Add(DateTime.Now, this);
                }
            }

        }

        public class Remove : IDisposable, IRecord
        {
            internal static JsonSerializerSettings settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };

            /// <summary>
            /// Clone est conserve le type original Serializer.DevObjectInstance ou Serializer.DevObjectReference et non T (Serializer.DevObject)
            /// </summary>
            object Clone(object obj)
            {
                var json = JsonConvert.SerializeObject(obj, settings);
                return JsonConvert.DeserializeObject(json, obj.GetType(), settings);
            }

            internal required Recorder<K, T, I> _recorder;
            internal K _key;
            internal T _before; // avant
            internal T _object; // objet en cours

            public Remove(K key, T obj)
            {
                _object = obj;
                _key = key;
                _before = (T)Clone(_object);
            }

            public void Dispose()
            {
                //if (!Equals(_before, after))//empêche la detection d'élément supprimé de la collection
                {
                    Program.Logger.WriteLine($"\nDelete <{_key}>\nwith {_before.Content.GetType().Name}\n{_before}");
                    _recorder.records.Add(DateTime.Now, this);
                }
            }

        }

        public class Record : IDisposable, IRecord
        {
            internal static JsonSerializerSettings settings = new JsonSerializerSettings { 
                MissingMemberHandling = MissingMemberHandling.Ignore, 
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Auto
            };

            /// <summary>
            /// Clone est conserve le type original Serializer.DevObjectInstance ou Serializer.DevObjectReference et non T (Serializer.DevObject)
            /// </summary>
            object Clone(object obj)
            {
                var json = JsonConvert.SerializeObject(obj, settings);
                return JsonConvert.DeserializeObject(json, obj.GetType(), settings);
            }

            internal required Recorder<K, T, I> _recorder;
            internal K _key;
            internal T _before; // avant
            internal T _after; // après
            internal T _object; // objet en cours

            public Record(K key, T obj)
            {
                _object = obj;
                _key = key;
                _before = (T)Clone(obj);
            }

            public void Dispose()
            {
                _after = (T)Clone(_object);

                //if (!Equals(_before, after))//empêche la detection d'élément supprimé de la collection
                {
                    Program.Logger.WriteLine($"\nChange <{_key}>\nfrom {_before.Content.GetType().Name}\n{_before}\nto {_before.Content.GetType().Name}\n{_after}");
                    _recorder.records.Add(DateTime.Now, this);
                }
            }

        }

        internal static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        public Recorder()
        {
        }

        /// <summary>
        /// Restore les objets modifiés dans la collection
        /// </summary>
        /// <param name="collection">Collection à modifier</param>
        public int Restore(IDictionary<K, I> collection, DateTime from, DateTime to)
        {
            int i = 0;
            var list = records.Where(p => p.Key.Ticks >= from.Ticks && p.Key.Ticks <= to.Ticks).Reverse().ToArray(); // déroule les éléments dans l'ordre inverse pour restaurer les états précédents
            foreach (var item in list)
            {
                i++;
                // l'objet existait avant la modification
                if (item.Value is Record record)
                {
                    // restore l'objet existant si possible
                    if (collection.TryGetValue(record._key, out var o))
                    {
                        var json = JsonConvert.SerializeObject(record._before);
                        //var serializer = (T)o; // conversion implicite object->Serializer ?
                        var serializer = new T(); // créer un serializer et assigne l'objet en cours
                        serializer.Content = o;
                        JsonConvert.PopulateObject(json, serializer);
                    }
                    else
                    {
                        // sinon replace l'objet existant
                        collection.Add(record._key, (I)record._before.Content);
                    }
                }
                // l'objet n'existait pas avant la modification (nouvel objet)
                if (item.Value is Insert insert)
                {
                    // supprime l'objet existant
                    collection.Remove(insert._key);
                }
                // l'objet existait avant la modification mais il a été supprimé (objet supprimé)
                if (item.Value is Remove remove)
                {
                    // sinon replace l'objet existant
                    collection.Add(remove._key, (I)remove._before.Content);
                }
            }
            return i;
        }

        /// <summary>
        /// Restore les objets modifiés dans la collection
        /// </summary>
        /// <param name="collection">Collection à modifier</param>
        public int Apply(IDictionary<K, I> collection, DateTime from, DateTime to)
        {
            int i = 0;
            var list = records.Where(p => p.Key.Ticks >= from.Ticks && p.Key.Ticks <= to.Ticks).ToArray();
            foreach (var item in list)
            {
                i++;
                // l'objet existait avant la modification
                if (item.Value is Record record)
                {
                    // restore l'objet existant si possible
                    if (collection.TryGetValue(record._key, out var o))
                    {
                        var json = JsonConvert.SerializeObject(record._after);
                        //var serializer = (T)o; // conversion implicite object->Serializer ?
                        var serializer = new T(); // créer un serializer et assigne l'objet en cours
                        serializer.Content = o;
                        JsonConvert.PopulateObject(json, serializer);
                    }
                    else
                    {
                        // sinon replace l'objet existant
                        collection.Add(record._key, (I)record._after.Content);
                    }
                }
                // l'objet n'existait pas avant la modification (nouvel objet)
                if (item.Value is Insert insert)
                {
                    // sinon replace l'objet existant
                    collection.Add(insert._key, (I)insert._after.Content);
                }
                // l'objet existait avant la modification mais il a été supprimé (objet supprimé)
                if (item.Value is Remove remove)
                {
                    // supprime l'objet existant
                    collection.Remove(remove._key);
                }
            }
            return i;
        }

        /// <summary>
        /// Efface l'historique
        /// </summary>
        public void Clear()
        {
            records.Clear();
        }

        public Record Rec(K key, T value)
        {
            return new Record(key, value) { _recorder = this };
        }
        public Insert New(K key, T value)
        {
            return new Insert(key, value) { _recorder = this };
        }
        public Remove Rem(K key, T value)
        {
            return new Remove(key, value) { _recorder = this };
        }
    }
}
