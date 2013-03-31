using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;
using Lucene.Tests;

namespace Lokad.Portable.Contrib.Tests
{
    [TestClass]
    public class RedisStorageTests
    {
        [TestMethod]
        public void open_redis_coonection()
        {
            RedisClient redisClient = new RedisClient("localhost", 6379);
            redisClient.Ping();
        }
        [TestMethod]
        public void create_store()
        {
            RedisClient redisClient = new RedisClient("localhost", 6379);
            var store = new Lokad.Portable.Contrib.AtomicStorage.RedisDocumentReaderWriter<Key, Entity>(redisClient);
            store.Dispose();

        }
        [TestMethod]
        public void store_an_entity()
        {
            RedisClient redisClient = new RedisClient("localhost", 6379);
            var store = new Lokad.Portable.Contrib.AtomicStorage.RedisDocumentReaderWriter<Key, Entity>(redisClient);
            store.AddOrUpdate(new Key("test", DateTime.Parse("01/01/2012")),
                               () => new Entity(10, 10),
                               (e) => e,
                               Lokad.Cqrs.AtomicStorage.AddOrUpdateHint.ProbablyExists);
            store.Dispose();
        }
        [TestMethod]
        public void store_an_entity_and_retrieve_it_from_store()
        {
            RedisClient redisClient = new RedisClient("localhost", 6379);
            var store = new Lokad.Portable.Contrib.AtomicStorage.RedisDocumentReaderWriter<Key, Entity>(redisClient);
            var key = new Key("test", DateTime.Parse("01/01/2012"));

            store.AddOrUpdate(key,
                               () => new Entity(10, 10),
                               (e) =>
                               {
                                   e.ItemCount = 20;
                                   return e;
                               },
                               Lokad.Cqrs.AtomicStorage.AddOrUpdateHint.ProbablyExists);
           
            Entity entity;

            Assert.IsTrue(store.TryGet(key, out entity));

            Assert.IsTrue(entity.ItemCount == 20);
            Assert.IsTrue(entity.TotalAmount == 10);
            
            store.Dispose();
        }
    }
}
