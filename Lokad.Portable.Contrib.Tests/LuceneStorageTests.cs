using Lokad.Portable.Contrib.AtomicStorage;
using Lucene.Wires;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Lucene.Tests
{
    [TestClass]
    public class LuceneStorageTests
    {
        private string index = @"..\..\Working\Lucene\index";
        private string backup = @"..\..\Working\Lucene\BackUp";

        [TestInitialize]
        public void Initialize()
        {

            foreach (string file in System.IO.Directory.EnumerateFiles(index))
            {
                System.IO.File.Delete(file);
            }
            foreach (string file in System.IO.Directory.EnumerateFiles(backup))
            {
                System.IO.File.Delete(file);
            }

        }
        [TestMethod]
        public void create_storage()
        {

            var strategy = new LuceneStrategy();

            var store = new LuceneDocumentReaderWriter<Key, Entity>(strategy, index);
            store.Dispose();
        }
        [TestMethod]
        public void store_an_entity()
        {

            var strategy = new LuceneStrategy();

            var store = new LuceneDocumentReaderWriter<Key, Entity>(strategy, index);
            store.AddOrUpdate(new Key("test", DateTime.Parse("01/01/2012")),
                               () => new Entity(10, 10),
                               (e) => e,
                               Lokad.Cqrs.AtomicStorage.AddOrUpdateHint.ProbablyExists);


            store.Dispose();
        }
        [TestMethod]
        public void store_an_entity_and_retrieve_it_from_store()
        {
            var strategy = new LuceneStrategy();

            var store = new LuceneDocumentReaderWriter<Key, Entity>(strategy, index);
            var key = new Key("test", DateTime.Parse("01/01/2012"));

            store.AddOrUpdate(key,
                               () => new Entity(10, 10),
                               (e) => e,
                               Lokad.Cqrs.AtomicStorage.AddOrUpdateHint.ProbablyExists);
            Entity entity;

            Assert.IsTrue(store.TryGet(key, out entity));

            Assert.IsTrue(entity.ItemCount == 10);
            Assert.IsTrue(entity.TotalAmount == 10);



            store.Dispose();
        }
        [TestMethod]
        public void store_an_entity_retrieve_it_from_store_perf_test()
        {
            var strategy = new LuceneStrategy();

            var store = new LuceneDocumentReaderWriter<Key, Entity>(strategy, index);
            var key = new Key("test", DateTime.Parse("01/01/2012"));

            store.AddOrUpdate(key,
                               () => new Entity(10, 10),
                               (e) => e,
                               Lokad.Cqrs.AtomicStorage.AddOrUpdateHint.ProbablyExists);
            Entity entity;

            Assert.IsTrue(store.TryGet(key, out entity));

            Assert.IsTrue(entity.ItemCount == 10);
            Assert.IsTrue(entity.TotalAmount == 10);

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            for (int _index = 1; _index <= 1000; _index++)
            {
                entity = null;
                stopWatch.Start();

                store.TryGet(key, out entity);
                stopWatch.Stop();
                Debug.WriteLine(stopWatch.Elapsed.ToString());
                Debug.WriteLine(entity.ToString());
                stopWatch.Reset();
            }

            store.Dispose();
        }
    }
}
