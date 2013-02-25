using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lokad.Cqrs.AtomicStorage;
using System.IO;
using Hyper.ComponentModel;
using System.ComponentModel;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Analysis.Standard;
using Version = Lucene.Net.Util.Version;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Analysis;

namespace Lokad.Portable.Contrib.AtomicStorage
{
    public class LuceneDocumentReaderWriter<TKey, TEntity> : IDocumentReader<TKey, TEntity>,
                                               IDocumentWriter<TKey, TEntity>,
                                               IDisposable

    {
        readonly IDocumentStrategy _strategy;
        readonly string _indexPath;
        IndexWriter writer;
        KeywordAnalyzer analyzer = new KeywordAnalyzer();
        SnapshotDeletionPolicy policy;
        private FSDirectory _directory;

        public LuceneDocumentReaderWriter(IDocumentStrategy strategy,
                                           string luceneDir)
        {
            _strategy = strategy;
            _indexPath = luceneDir;
            HyperTypeDescriptionProvider.Add(typeof(TKey));
            _directory = FSDirectory.Open(new DirectoryInfo(luceneDir));
            if (IndexWriter.IsLocked(_directory)) IndexWriter.Unlock(_directory);
            var lockFilePath = Path.Combine(luceneDir, "write.lock");
            if (File.Exists(lockFilePath)) File.Delete(lockFilePath);
            policy = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
            writer = new IndexWriter(_directory, analyzer, policy, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        public bool TryGet(TKey key, out TEntity view)
        {
            view = default(TEntity);
            try
            {
               
            }
            catch (Exception ex)
            {

            }
            throw new NotImplementedException();
        }

        public TEntity AddOrUpdate(TKey key, Func<TEntity> addFactory, Func<TEntity, TEntity> update, AddOrUpdateHint hint = AddOrUpdateHint.ProbablyExists)
        {
            throw new NotImplementedException();
        }

        private void StoreResultInIndex(TKey key, TEntity entity)
        {
           
            var doc = new Document();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(key);

            foreach (PropertyDescriptor property in properties)
            {

                var value = property.GetValue(key).ToString();
                var name = property.Name;
                doc.Add(new Field(name, value, Field.Store.YES, Field.Index.ANALYZED));
            }
            string serializeEntity = string.Empty;

            using (System.IO.MemoryStream entityStream = new System.IO.MemoryStream())
            {
                _strategy.Serialize(entity,entityStream);
                serializeEntity = GetString(entityStream.ToArray());
            }
            
            doc.Add(new Field("document",serializeEntity, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
            writer.Commit();
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
        private static BooleanQuery CreateQuery(TKey key)
        {
            var query = new BooleanQuery();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(key);

            foreach (PropertyDescriptor property in properties)
            {

                var value = property.GetValue(key).ToString();
                var name = property.Name;
                query.Add(new TermQuery(new Term(property.Name, property.GetValue(key).ToString())), Occur.MUST);
            }

            return query;
        }
        private TEntity GetEntity(TKey key)
        {
            var reader = IndexReader.Open(_directory, false);
            using (var searcher = new IndexSearcher(reader))
            {
                var hits_limit = 1000;
             
                {
                    var query = CreateQuery(key);

                    var hits = searcher.Search(query, hits_limit).ScoreDocs;
                    var results = mapLuceneToDataList(hits, searcher);

                    searcher.Dispose();

                    return results.FirstOrDefault();
                }

            }
        }
            private static IEnumerable<TEntity> mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => mapLuceneDocumentToData(searcher.Doc(hit.Doc)));
        }
            private static TEntity mapLuceneDocumentToData(Document doc)
            {
                //return doc.Get("documentPath");
                //deserialize document to type
                throw new NotImplementedException();
            }
        public bool TryDelete(TKey key)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
