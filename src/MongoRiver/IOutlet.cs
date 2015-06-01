using MongoDB.Bson;

namespace MongoRiver
{
    public interface IOutlet
    {
        void UpdateOptime(BsonTimestamp timestamp);

        void Insert(string databaseName, string collectionName, BsonDocument insertedDocument);
        void Update(string databaseName, string collectionName, BsonDocument filterDocument, BsonDocument updatedDocument);
        void Delete(string databaseName, string collectionName, BsonDocument filterDocument);

        void CreateIndex(string databaseName, string collectionName, BsonDocument indexKeyDocument, BsonDocument optionsDocument);
        void DeleteIndex(string databaseName, string collectionName, string indexName);

        void CreateCollection(string databaseName, string collectionName, BsonDocument optionsDocument);
        void RenameCollection(string databaseName, string collectionName, string newCollectionName);
        void DeleteCollection(string databaseName, string collectionName);

        void DeleteDatabase(string databaseName);
    }
}
