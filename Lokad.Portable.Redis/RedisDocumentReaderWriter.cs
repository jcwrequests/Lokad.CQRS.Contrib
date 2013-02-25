using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using System.IO;
using System.ComponentModel;
using ServiceStack.Redis;
using System.Security.Cryptography;
using ProtoBuf;

namespace Lokad.Portable.Contrib.AtomicStorage
{
    public class RedisDocumentReaderWriter<TKey, TEntity> : IDocumentReader<TKey, TEntity>,
                                               IDocumentWriter<TKey, TEntity>, 
                                               IDisposable
     {  
        RedisClient redisClient;

        public RedisDocumentReaderWriter(RedisClient redisClient)
        {
            if (redisClient == null) throw new ArgumentNullException("redisClient");
            
            this.redisClient = redisClient;

        }

        public void InitIfNeeded()
        {
            //
        }

 

        public bool TryGet(TKey key, out TEntity view)
        {
            view = default(TEntity);
            try
            {
                var hashedKey = GetKeyHash(key);
                view = redisClient.Get<TEntity>(hashedKey);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
           
        }


        string GetKeyHash(TKey key)
        {
            SHA1 sha1 = SHA1.Create();
            byte[] shaHash = null;
            string hashedKey = null;
            using( MemoryStream keyStream = new MemoryStream())
            {
                Serializer.Serialize(keyStream, key);
                shaHash = sha1.ComputeHash(keyStream.ToArray());
                hashedKey = System.Text.UTF8Encoding.UTF8.GetString(shaHash);
            }
            return hashedKey;
            
        }

        
        public TEntity AddOrUpdate(TKey key, Func<TEntity> addFactory, Func<TEntity, TEntity> update,
            AddOrUpdateHint hint)
        {
            var hashedKey = GetKeyHash(key);
            var entity = redisClient.Get<TEntity>(hashedKey);
            bool isNew = (entity == null);
            TEntity result = (isNew ? addFactory() : update(entity));

            return result;

            
        }

       

        public bool TryDelete(TKey key)
        {
            var hashedKey = GetKeyHash(key);
            return redisClient.Remove(hashedKey);
        }
  

        public void Dispose()
        {
            redisClient.Dispose();
        }
     }
}
