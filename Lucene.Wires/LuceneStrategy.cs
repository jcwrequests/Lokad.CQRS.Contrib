using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using ProtoBuf;
using System.Runtime.Serialization;

namespace Lucene.Wires
{

    static class NameCache<T>
    {
        // ReSharper disable StaticFieldInGenericType
        public static readonly string Name;
        public static readonly string Namespace;
        // ReSharper restore StaticFieldInGenericType
        static NameCache()
        {
            var type = typeof(T);

            Name = new string(Splice(type.Name).ToArray()).TrimStart('-');
            var dcs = type.GetCustomAttributes(false).OfType<DataContractAttribute>().ToArray();


            if (dcs.Length <= 0) return;
            var attribute = dcs.First();

            if (!string.IsNullOrEmpty(attribute.Name))
            {
                Name = attribute.Name;
            }

            if (!string.IsNullOrEmpty(attribute.Namespace))
            {
                Namespace = attribute.Namespace;
            }
        }

        static IEnumerable<char> Splice(string source)
        {
            foreach (var c in source)
            {
                if (char.IsUpper(c))
                {
                    yield return '-';
                }
                yield return char.ToLower(c);
            }
        }
    }
    public class LuceneStrategy : IDocumentStrategy
    {
        public TEntity Deserialize<TEntity>(System.IO.Stream stream)
        {
            var signature = stream.ReadByte();

            if (signature != 42)
                throw new InvalidOperationException("Unknown view format");

            return Serializer.Deserialize<TEntity>(stream);
        }

        public string GetEntityBucket<TEntity>()
        {
            return Conventions.ViewsFolder + "/" + NameCache<TEntity>.Name;
        }

        public string GetEntityLocation<TEntity>(object key)
        {
            return Guid.NewGuid().ToString().ToLowerInvariant() + ".pb";
        }

        public void Serialize<TEntity>(TEntity entity, System.IO.Stream stream)
        {
            stream.WriteByte(42);
            Serializer.Serialize(stream, entity);
        }
    }
}
