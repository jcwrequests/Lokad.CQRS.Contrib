using Lokad.Cqrs.AtomicStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lokad.Portable.Contrib.AtomicStorage
{
    public class LuceneDocumentStore : IDocumentStore
    {
        public IEnumerable<DocumentRecord> EnumerateContents(string bucket)
        {
            throw new NotImplementedException();
        }

        public IDocumentReader<TKey, TEntity> GetReader<TKey, TEntity>()
        {
            throw new NotImplementedException();
        }

        public IDocumentWriter<TKey, TEntity> GetWriter<TKey, TEntity>()
        {
            throw new NotImplementedException();
        }

        public void Reset(string bucket)
        {
            throw new NotImplementedException();
        }

        public IDocumentStrategy Strategy
        {
            get { throw new NotImplementedException(); }
        }

        public void WriteContents(string bucket, IEnumerable<DocumentRecord> records)
        {
            throw new NotImplementedException();
        }
    }
}
