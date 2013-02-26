using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Analysis.Standard;
using Version = Lucene.Net.Util.Version;
using Lucene.Net.Documents;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using System.IO;

namespace Lokad.Portable.Contrib.AtomicStorage
{
    public class LuceneDocumentStore : IDocumentStore
    {
        private string luceneDirectory;
        private IDocumentStrategy strategy;

        public LuceneDocumentStore(string luceneDirectory, IDocumentStrategy strategy)
        {
            this.luceneDirectory = luceneDirectory;
            this.strategy = strategy;
        }
       
        public IDocumentReader<TKey, TEntity> GetReader<TKey, TEntity>()
        {
            return new LuceneDocumentReaderWriter<TKey, TEntity>(strategy,luceneDirectory);
        }

        public IDocumentWriter<TKey, TEntity> GetWriter<TKey, TEntity>()
        {
            return new LuceneDocumentReaderWriter<TKey, TEntity>(strategy, luceneDirectory);
        }

        public IDocumentStrategy Strategy
        {
            get { return this.strategy; }
        }

        public void Reset(string bucket)
        {
            throw new NotImplementedException();
        }
        static string GetString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }
        static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }
        public IEnumerable<DocumentRecord> EnumerateContents(string bucket)
        {
            var _directory = FSDirectory.Open(new DirectoryInfo(bucket));
            var reader = IndexReader.Open( _directory, false);
            for (int index = 0; index < reader.MaxDoc; index++)
            {
                var document = reader.Document(index);
                var key = document.GetField("key").StringValue;
                var documentValue = document.GetField("document").StringValue;

                yield return new DocumentRecord(key, () => GetBytes(documentValue));
            }

        }
        public void WriteContents(string bucket, IEnumerable<DocumentRecord> records)
        {
            using (KeywordAnalyzer analyzer = new KeywordAnalyzer())
            {
                var policy = new SnapshotDeletionPolicy(new KeepOnlyLastCommitDeletionPolicy());
                using (var _directory = FSDirectory.Open(new DirectoryInfo(bucket)))
                {
                    using (var writer = new IndexWriter(_directory, analyzer, policy, IndexWriter.MaxFieldLength.UNLIMITED))
                    {
                        var doc = new Document();
                        foreach (DocumentRecord record in records)
                        {
                            doc.Add(new Field("key", record.Key, Field.Store.YES, Field.Index.ANALYZED));
                            doc.Add(new Field("document", GetString(record.Read()), Field.Store.YES, Field.Index.NOT_ANALYZED));
                            writer.AddDocument(doc);
                        }
                        writer.Commit();

                    }
                }
            }
            
            

     

            
        }
    }
}
