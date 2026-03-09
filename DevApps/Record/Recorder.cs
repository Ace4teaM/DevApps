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

        public class Move : IDisposable, IRecord
        {
            internal required Recorder<K, T, I> _recorder;
            internal K _key_before;
            internal K _key_after;

            public Move(K key_before, K key_after)
            {
                _key_before = key_before;
                _key_after = key_after;
            }

            public void Dispose()
            {
                //if (!Equals(_before, after))//empêche la detection d'élément supprimé de la collection
                {
                    Program.Logger.WriteLine($"\nRename <{_key_before}> to {_key_after}");
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
            internal static object Clone(object obj)
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
            NullValueHandling = NullValueHandling.Include
        };

        public Recorder()
        {
        }

        /// <summary>
        /// Restore les objets modifiés dans la collection (Undo)
        /// </summary>
        /// <param name="collection">Collection à modifier</param>
        /// <returns>Liste des éléments restaurés</returns>
        public IEnumerable<KeyValuePair<DateTime, IRecord>> Restore(IDictionary<K, I> collection, DateTime from, DateTime to)
        {
            var list = records.Where(p => p.Key > from && p.Key < to).Reverse().ToArray(); // déroule les éléments dans l'ordre inverse pour restaurer les états précédents
            foreach (var item in list)
            {
                // l'objet existait avant la modification
                if (item.Value is Record record)
                {
                    // restore l'objet existant si possible
                    if (collection.TryGetValue(record._key, out var o))
                    {
                        var json = JsonConvert.SerializeObject(record._before);
                        var serializer = (T)Record.Clone(record._before);
                        serializer.Content = o;
                        JsonConvert.PopulateObject(json, serializer, settings);
                    }
                    else
                    {
                        // sinon replace l'objet existant
                        collection.Add(record._key, (I)record._before.Content);
                    }
                }
                // l'objet n'existait pas avant la modification (nouvel objet)
                else if (item.Value is Insert insert)
                {
                    // supprime l'objet existant
                    collection.Remove(insert._key);
                }
                // l'objet existait avant la modification mais il a été supprimé (objet supprimé)
                else if (item.Value is Remove remove)
                {
                    // sinon replace l'objet existant
                    collection.Add(remove._key, (I)remove._before.Content);
                }
                // l'objet à été renommé
                else if (item.Value is Move move)
                {
                    // sinon replace l'objet existant
                    if (collection.TryGetValue(move._key_after, out var o))
                    {
                        collection.Remove(move._key_after);
                        collection.Add(move._key_before, o);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Restore les objets modifiés dans la collection (Redo)
        /// </summary>
        /// <param name="collection">Collection à modifier</param>
        public IEnumerable<KeyValuePair<DateTime, IRecord>> Apply(IDictionary<K, I> collection, DateTime from, DateTime to)
        {
            var list = records.Where(p => p.Key >= from && p.Key <= to).ToArray();
            foreach (var item in list)
            {
                // l'objet existait avant la modification
                if (item.Value is Record record)
                {
                    // restore l'objet existant si possible
                    if (collection.TryGetValue(record._key, out var o))
                    {
                        var json = JsonConvert.SerializeObject(record._after);
                        var serializer = (T)Record.Clone(record._after);
                        serializer.Content = o;
                        JsonConvert.PopulateObject(json, serializer, settings);
                    }
                    else
                    {
                        // sinon replace l'objet existant
                        collection.Add(record._key, (I)record._after.Content);
                    }
                }
                // l'objet n'existait pas avant la modification (nouvel objet)
                else if (item.Value is Insert insert)
                {
                    // sinon replace l'objet existant
                    collection.Add(insert._key, (I)insert._after.Content);
                }
                // l'objet existait avant la modification mais il a été supprimé (objet supprimé)
                else if (item.Value is Remove remove)
                {
                    // supprime l'objet existant
                    collection.Remove(remove._key);
                }
                // l'objet à été renommé
                else if (item.Value is Move move)
                {
                    // sinon replace l'objet existant
                    if (collection.TryGetValue(move._key_before, out var o))
                    {
                        collection.Remove(move._key_before);
                        collection.Add(move._key_after, o);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Efface l'historique
        /// </summary>
        public void Clear()
        {
            records.Clear();
        }


        /// <summary>
        /// Un objet est modifié
        /// </summary>
        public Record Rec(K key, T value)
        {
            return new Record(key, value) { _recorder = this };
        }

        /// <summary>
        /// Un objet est créé
        /// </summary>
        public Insert New(K key, T value)
        {
            return new Insert(key, value) { _recorder = this };
        }

        /// <summary>
        /// Un objet est supprimé
        /// </summary>
        public Remove Rem(K key, T value)
        {
            return new Remove(key, value) { _recorder = this };
        }

        /// <summary>
        /// Un objet est renommé
        /// </summary>
        public Move Mov(K key_before, K key_after)
        {
            return new Move(key_before, key_after) { _recorder = this };
        }
    }
}
