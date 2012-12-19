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
    public class FileDocumentReaderWriter<TKey, TEntity> : IDocumentReader<TKey, TEntity>,
                                               IDocumentWriter<TKey, TEntity>, 
                                               IDisposable
     {  
        readonly string _folder;
        readonly IDocumentStrategy _strategy;
        readonly string _indexPath;
        private FSDirectory _directory;
        IndexWriter writer;
        KeywordAnalyzer analyzer = new KeywordAnalyzer();
        SnapshotDeletionPolicy policy;

        public FileDocumentReaderWriter(string directoryPath,
                                        IDocumentStrategy strategy,
                                        string luceneDir)
        {
            _folder = Path.Combine(directoryPath, strategy.GetEntityBucket<TEntity>()); 
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

        public void InitIfNeeded()
        {
            System.IO.Directory.CreateDirectory(_folder);
        }

        public void BackUp(string backUpDirectory)
        {
            IndexCommit cp = policy.Snapshot();
            try
            {
                //copy snapshot files
                var files = cp.FileNames;
                foreach (var file in files)
                {
                    FileInfo fileInfo = new FileInfo(System.IO.Path.Combine(_indexPath,file));
                    fileInfo.CopyTo(System.IO.Path.Combine(backUpDirectory, 
                                                           System.IO.Path.GetFileName(file)),
                                                           true
                                    );
               
                }

                FileInfo segmentsGen = new FileInfo(System.IO.Path.Combine(_indexPath,"segments.gen"));
                segmentsGen.CopyTo(System.IO.Path.Combine(backUpDirectory,"segments.gen"),true);

                var source = System.IO.Directory.EnumerateFiles(_indexPath).
                             Where(f => !System.IO.Directory.EnumerateFiles(backUpDirectory).
                                         Select(i => Path.GetFileName(i)).
                                         Contains(Path.GetFileName(f))).
                             Where(f => !f.EndsWith("write.lock",StringComparison.InvariantCultureIgnoreCase));

                foreach (var file in source)
                {
                    FileInfo item = new FileInfo(file);
                    item.CopyTo(System.IO.Path.Combine(backUpDirectory, item.Name));
                }



                

            }
            finally
            {
                policy.Release();
            }


        }

        public bool TryGet(TKey key, out TEntity view)
        {
            view = default(TEntity);
            try
            {
                var name = GetFileName(key);
               
                if (!File.Exists(name))
                    return false;

                using (var stream = File.Open(name, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (stream.Length == 0)
                        return false;
                    view = _strategy.Deserialize<TEntity>(stream);
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
                // if file happened to be deleted between the moment of check and actual read.
                return false;
            }
            catch (DirectoryNotFoundException)
            {
                return false;
            }
        }

        string GetName()
        {
            return Path.Combine(_folder, _strategy.GetEntityLocation<TEntity>(null));
        }

        string GetFileName(TKey key)
        {
            //check cache first then goto the lucene index
            //need to create cache which will be a dictionary
            //using (var searcher = new IndexSearcher(_indexPath, false))
            var reader = IndexReader.Open(_directory, false);
            using (var searcher = new IndexSearcher(reader))
            {
                var hits_limit = 1000;
                var analyzer = new StandardAnalyzer(Version.LUCENE_29);

                {
                    var query = CreateQuery(key);

                    var hits = searcher.Search(query, hits_limit).ScoreDocs;
                    var results = _mapLuceneToDataList(hits, searcher);

                    analyzer.Close();
                    searcher.Dispose();

                    return results.FirstOrDefault();
                }

            }

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

        private static IEnumerable<string> _mapLuceneToDataList(IEnumerable<ScoreDoc> hits, IndexSearcher searcher)
        {
            return hits.Select(hit => _mapLuceneDocumentToData(searcher.Doc(hit.Doc)));
        }

        private static string _mapLuceneDocumentToData(Document doc)
        {
            return doc.Get("documentPath");

        }


        public TEntity AddOrUpdate(TKey key, Func<TEntity> addFactory, Func<TEntity, TEntity> update,
            AddOrUpdateHint hint)
        {
            var name = GetFileName(key);
            bool isNew = (name == null);
            if (name == null) name = GetName();

            try
            {
                // This is fast and allows to have git-style subfolders in atomic strategy
                // to avoid NTFS performance degradation (when there are more than 
                // 10000 files per folder). Kudos to Gabriel Schenker for pointing this out
                var subfolder = Path.GetDirectoryName(name);
                if (subfolder != null && !System.IO.Directory.Exists(subfolder))
                    System.IO.Directory.CreateDirectory(subfolder);
 

                // we are locking this file.
                using (var file = File.Open(name, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    byte[] initial = new byte[0];
                    TEntity result;
                    if (file.Length == 0)
                    {
                        result = addFactory();
                    }
                    else
                    {
                        using (var mem = new MemoryStream())
                        {
                            file.CopyTo(mem);
                            mem.Seek(0, SeekOrigin.Begin);
                            var entity = _strategy.Deserialize<TEntity>(mem);
                            initial = mem.ToArray();
                            result = update(entity);
                        }
                    }

                    // some serializers have nasty habbit of closing the
                    // underling stream
                    using (var mem = new MemoryStream())
                    {
                        _strategy.Serialize(result, mem);
                        var data = mem.ToArray();

                        if (!data.SequenceEqual(initial))
                        {
                            // upload only if we changed
                            file.Seek(0, SeekOrigin.Begin);
                            file.Write(data, 0, data.Length);
                            // truncate this file
                            file.SetLength(data.Length);
                        }
                    }
                    if (isNew) StoreResultInIndex(key, name);
                    return result;
                }
            }
            catch (DirectoryNotFoundException)
            {
                var s = string.Format(
                    "Container '{0}' does not exist.",
                    _folder);
                throw new InvalidOperationException(s);
            }
        }

       

        public bool TryDelete(TKey key)
        {
            var name = GetFileName(key);
            
            if (File.Exists(name))
            {
                File.Delete(name);
                RemoveFromIndex(key);
                return true;
            }
            return false;
        }
  

        private void RemoveFromIndex(TKey key)
        {  
            var searchQuery = CreateQuery(key);
            writer.DeleteDocuments(searchQuery);
            writer.Commit();
        }
        private void StoreResultInIndex(TKey key, string entityPath)
        {
            //could cache the data into a temporary hash table
            //then I can flush the writes every two seconds
            //or as needed

            var doc = new Document();
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(key);

            foreach (PropertyDescriptor property in properties)
            {

                var value = property.GetValue(key).ToString();
                var name = property.Name;
                doc.Add(new Field(name, value, Field.Store.YES, Field.Index.ANALYZED));
            }
            doc.Add(new Field("documentPath", entityPath, Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);
            writer.Commit();
        }


        public void Dispose()
        {
            analyzer.Close();
            writer.Dispose();
        }
     }
}
