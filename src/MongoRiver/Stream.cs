using System;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace MongoRiver
{
    public class Stream
    {
        private readonly Tailer m_tailer;
        private readonly IOutlet m_outlet;

        public Stream(Tailer tailer, IOutlet outlet)
        {
            if(tailer == null)
            {
                throw new ArgumentNullException("tailer");
            }

            if(outlet == null)
            {
                throw new ArgumentNullException("outlet");
            }

            m_tailer = tailer;
            m_outlet = outlet;
        }

        public async Task RunForever(DateTime startDate)
        {
            var startOplog = await m_tailer.GetMostRecentOplog(startDate);
            await RunForever(startOplog);
        }

        public async Task RunForever(BsonTimestamp startTime)
        {
            var startOplog = await m_tailer.GetMostRecentOplog(startTime);
            await RunForever(startOplog);
        }

        public async Task RunForever(Oplog startOplog = null)
        {
            await m_tailer.Tail(startOplog);
            await m_tailer.Stream(oplog => HandleOperation(oplog));
        }

        public void Stop()
        {
            m_tailer.Stop();
            m_tailer.Dispose();
        }

        public void HandleOperation(Oplog oplog)
        {
            if(oplog.Operation == "n")
            {
                // Operations we do not care about
                return;
            }

            switch(oplog.Operation)
            {
                case "i":
                    HandleInsert(oplog.Database, oplog.Collection, oplog.Object);
                    break;
                case "u":
                    m_outlet.Update(oplog.Database, oplog.Collection, oplog.Object2, oplog.Object);
                    break;
                case "d":
                    m_outlet.Delete(oplog.Database, oplog.Collection, oplog.Object);
                    break;
                case "c":
                    HandleCommand(oplog.Database, oplog.Collection, oplog.Object);
                    break;
                default:
                    // Unhandled operation
                    break;
            }

            m_outlet.UpdateOptime(oplog.Timestamp);
        }

        private void HandleInsert(string databaseName, string collectionName, BsonDocument document)
        {
            if(collectionName == "system.indexes")
            {
                HandleCreateIndex(document);
            }
            else
            {
                m_outlet.Insert(databaseName, collectionName, document);
            }
        }

        private void HandleCreateIndex(BsonDocument document)
        {
            var ns = new OplogNamespace(document["ns"]);

            if(document.Contains("v") && document["v"].ToInt32() != 1)
            {
                // Only v1 indexes are supported
                // ref: http://docs.mongodb.org/manual/tutorial/roll-back-to-v1.8-index/
                return;
            }

            var key = document["key"].ToBsonDocument();

            document.Remove("ns");
            document.Remove("key");
            document.Remove("_id");

            m_outlet.CreateIndex(ns.DatabaseName, ns.CollectionName, key, document);
        }

        private void HandleCommand(string databaseName, string collectionName, BsonDocument document)
        {
            if(collectionName != "$cmd")
            {
                return;
            }

            if(document.Contains("dropIndexes"))
            {
                m_outlet.DeleteIndex(databaseName, document["dropIndexes"].ToString(), document["index"].ToString());
            }
            else if(document.Contains("create"))
            {
                HandleCreateCollection(databaseName, document["create"].ToString(), document);
            }
            else if(document.Contains("drop"))
            {
                m_outlet.DeleteCollection(databaseName, document["drop"].ToString());
            }
            else if(document.Contains("renameCollection"))
            {
                var oldNs = new OplogNamespace(document["renameCollection"]);
                var newNs = new OplogNamespace(document["to"]);

                m_outlet.RenameCollection(oldNs.DatabaseName, oldNs.CollectionName, newNs.CollectionName);
            }
            else if(document.Contains("dropDatabase"))
            {
                m_outlet.DeleteDatabase(databaseName);
            }
            else
            {
                // Unrecognized command
            }
        }

        private void HandleCreateCollection(string databaseName, string collectionName, BsonDocument document)
        {
            document.Remove("create");
            m_outlet.CreateCollection(databaseName, collectionName, document);
        }
    }
}
