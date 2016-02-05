using System;
using MongoDB.Bson;

namespace MongoRiver
{
    public class OpLogEvent
    {
        public string Type { get; set; }
        public BsonTimestamp Timestamp { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public BsonDocument Document { get; set; }
        public BsonDocument Filter { get; set; }
        public BsonDocument IndexDocument { get; set; }
        public BsonDocument OptionsDocument { get; set; }
        public string IndexName { get; set; }
        public string NewCollectionName { get; set; }
    }

    public class ObserverOutlet : MongoRiver.IOutlet
    {
        private readonly IObserver<OpLogEvent> _observer;

        public ObserverOutlet(IObserver<OpLogEvent> observer)
        {
            _observer = observer;
        }

        public void UpdateOptime(BsonTimestamp timestamp)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "UpdateOptime",
                Timestamp = timestamp
            });
        }

        public void Insert(string databaseName, string collectionName, BsonDocument insertedDocument)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "Insert",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                Document = insertedDocument
            });
        }

        public void Update(string databaseName, string collectionName, BsonDocument filterDocument, BsonDocument updatedDocument)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "Update",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                Filter = filterDocument,
                Document = updatedDocument
            });
        }

        public void Delete(string databaseName, string collectionName, BsonDocument filterDocument)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "Delete",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                Filter = filterDocument
            });
        }

        public void CreateIndex(string databaseName, string collectionName, BsonDocument indexDocument, BsonDocument optionsDocument)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "CreateIndex",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                IndexDocument = indexDocument,
                OptionsDocument = optionsDocument
            });
        }

        public void DeleteIndex(string databaseName, string collectionName, string indexName)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "DeleteIndex",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                IndexName = indexName
            });
        }

        public void CreateCollection(string databaseName, string collectionName, BsonDocument optionsDocument)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "CreateCollection",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                OptionsDocument = optionsDocument
            });
        }

        public void RenameCollection(string databaseName, string collectionName, string newCollectionName)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "RenameCollection",
                DatabaseName = databaseName,
                CollectionName = collectionName,
                NewCollectionName = newCollectionName
            });
        }

        public void DeleteCollection(string databaseName, string collectionName)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "DeleteCollection",
                DatabaseName = databaseName,
                CollectionName = collectionName
            });
        }

        public void DeleteDatabase(string databaseName)
        {
            _observer.OnNext(new OpLogEvent()
            {
                Type = "DeleteDatabase",
                DatabaseName = databaseName
            });
        }
    }
}
