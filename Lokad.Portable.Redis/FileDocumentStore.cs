using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ServiceStack.Redis;

namespace Lokad.Portable.Contrib.AtomicStorage
{
    public sealed class FileDocumentStore : IDocumentStore
    {
        RedisClient redisClient;
        public FileDocumentStore(RedisClient redisClient)
        {
            if (redisClient == null) throw new ArgumentNullException("redisClient");
            this.redisClient = redisClient;
        }

        //public override string ToString()
        //{
        //    return new Uri(Path.GetFullPath(_folderPath)).AbsolutePath;
        //}


        readonly HashSet<Tuple<Type, Type>> _initialized = new HashSet<Tuple<Type, Type>>();


        public IDocumentWriter<TKey, TEntity> GetWriter<TKey, TEntity>()
        {
            var container = new FileDocumentReaderWriter<TKey, TEntity>(redisClient);
            if (_initialized.Add(Tuple.Create(typeof(TKey), typeof(TEntity))))
            {
                container.InitIfNeeded();
            }
            return container;
        }

        public IDocumentReader<TKey, TEntity> GetReader<TKey, TEntity>()
        {
            return new FileDocumentReaderWriter<TKey, TEntity>(redisClient);
        }

        public IDocumentStrategy Strategy
        {
            get { return null; }
        }

        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
        public IEnumerable<DocumentRecord> EnumerateContents(string hashId)
        {
            var entries = redisClient.GetAllEntriesFromHash(hashId);
            if (entries.Count.Equals(0)) yield break;
            foreach (var entry in entries)
            {
                yield return new DocumentRecord(entry.Key, () =>  GetBytes(entry.Value));

            }

           
        }

        public void WriteContents(string hashID, IEnumerable<DocumentRecord> records)
        {
            
            
            foreach (var pair in records)
            {
                redisClient.SetEntryInHashIfNotExists(hashID, pair.Key,GetString(pair.Read()));
            
            }
        }

        
        public void Reset(string hashId)
        {
            var entries = redisClient.GetAllEntriesFromHash(hashId);
            if (entries.Count.Equals(0)) return;
            foreach (var entry in entries)
            {
                redisClient.RemoveEntryFromHash(hashId, entry.Key);

            }
        }
    }
}
