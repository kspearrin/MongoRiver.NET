using System;
using System.Collections;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoRiver
{
    public class Tailer : IDisposable
    {
        protected readonly IMongoCollection<Oplog> m_oplogCollection;

        protected IAsyncCursor<Oplog> m_cursor;
        protected bool m_stopped = false;
        protected bool m_streaming = false;

        public Tailer() { }

        public Tailer(IMongoClient client, MongoCollectionSettings oplogCollectionSettings = null, string oplogCollectionName = "oplog.rs")
        {
            if(client == null)
            {
                throw new ArgumentNullException("client");
            }

            if(string.IsNullOrWhiteSpace(client.Settings.ReplicaSetName))
            {
                throw new MongoRiverException("Mongo client is not configured as a replica set");
            }

            if(oplogCollectionSettings == null)
            {
                oplogCollectionSettings = new MongoCollectionSettings
                {
                    // You probably don't want to be doing this on your primary
                    ReadPreference = ReadPreference.Secondary
                };
            }

            m_oplogCollection = client.GetDatabase("local").GetCollection<Oplog>(oplogCollectionName, oplogCollectionSettings);
        }

        public virtual bool Tailing
        {
            get
            {
                return !m_stopped && m_streaming;
            }
        }

        public virtual async Task<Oplog> GetMostRecentOplog(DateTime beforeDate)
        {
            return await GetMostRecentOplog(new BsonTimestamp(beforeDate.ToInt(), 0));
        }

        public virtual async Task<Oplog> GetMostRecentOplog(BsonTimestamp beforeTime = null)
        {
            SortDefinition<Oplog> sort = Builders<Oplog>.Sort.Descending("$natural");

            FilterDefinition<Oplog> filter = new BsonDocument();
            if(beforeTime != null)
            {
                filter = Builders<Oplog>.Filter.Lte(o => o.Timestamp, beforeTime);
            }

            Oplog record = await m_oplogCollection.Find(filter).Sort(sort).FirstOrDefaultAsync();
            if(record != null)
            {
                return record;
            }

            return null;
        }

        public virtual async Task Tail(Oplog startOplog = null)
        {
            if(m_cursor != null)
            {
                throw new MongoRiverException("We're already talking to the oplog");
            }

            FilterDefinition<Oplog> filter = new BsonDocument();
            if(startOplog != null)
            {
                filter = Builders<Oplog>.Filter.Gt(o => o.Timestamp, startOplog.Timestamp);
            }

            var options = new FindOptions<Oplog, Oplog>
            {
                CursorType = CursorType.TailableAwait,
                NoCursorTimeout = true
            };

            m_cursor = await m_oplogCollection.FindAsync<Oplog>(filter, options);
        }

        public virtual async Task Stream(Action<Oplog> handleOperation, int? limit = null)
        {
            m_streaming = true;

            var count = 0;
            while(!m_stopped && await m_cursor.MoveNextAsync())
            {
                count++;
                if(limit.HasValue && count >= limit)
                {
                    break;
                }

                foreach(var oplog in m_cursor.Current)
                {
                    handleOperation(oplog);
                }
            }

            m_streaming = false;
        }

        public virtual void Stop()
        {
            m_stopped = true;
        }

        public virtual void Dispose()
        {
            if(m_cursor != null)
            {
                m_cursor.Dispose();
                m_cursor = null;
            }

            m_stopped = false;
        }
    }
}
