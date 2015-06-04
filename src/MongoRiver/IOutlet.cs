using MongoDB.Bson;

namespace MongoRiver
{
    public interface IOutlet
    {
        /// <summary>
        /// Called when a oplog has been handled by an outlet.
        /// </summary>
        /// <param name="timestamp">The timestamp of the oplog. Also know as the "ts" field of the oplog.</param>
        void UpdateOptime(BsonTimestamp timestamp);

        /// <summary>
        /// Called when a document has been inserted.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The affected collection name.</param>
        /// <param name="insertedDocument">The inserted document.</param>
        void Insert(string databaseName, string collectionName, BsonDocument insertedDocument);
        /// <summary>
        /// Called when a document has been updated.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The affected collection name.</param>
        /// <param name="filterDocument">The document decribing the filter used to taget the updated document.</param>
        /// <param name="updatedDocument">The updated document.</param>
        void Update(string databaseName, string collectionName, BsonDocument filterDocument, BsonDocument updatedDocument);
        /// <summary>
        /// Called when a document has been deleted.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The affected collection name.</param>
        /// <param name="filterDocument">The document decribing the filter used to taget the deleted document.</param>
        void Delete(string databaseName, string collectionName, BsonDocument filterDocument);

        /// <summary>
        /// Called when an index has been created.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The affected collection name.</param>
        /// <param name="indexDocument">The document decribing the index.</param>
        /// <param name="optionsDocument">The document describing the additional options that exist on the index.</param>
        void CreateIndex(string databaseName, string collectionName, BsonDocument indexDocument, BsonDocument optionsDocument);
        /// <summary>
        /// Called when an index has been deleted.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The affected collection name.</param>
        /// <param name="indexName">The deleted index name.</param>
        void DeleteIndex(string databaseName, string collectionName, string indexName);

        /// <summary>
        /// Called when a collection has been created.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The created collection name.</param>
        /// <param name="optionsDocument">The document describing the additional options that exist on the collection.</param>
        void CreateCollection(string databaseName, string collectionName, BsonDocument optionsDocument);
        /// <summary>
        /// Called when a collection has been renamed.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The old collection name.</param>
        /// <param name="newCollectionName">The new collection name.</param>
        void RenameCollection(string databaseName, string collectionName, string newCollectionName);
        /// <summary>
        /// Called when a collection has been deleted.
        /// </summary>
        /// <param name="databaseName">The affected database name.</param>
        /// <param name="collectionName">The deleted collection name.</param>
        void DeleteCollection(string databaseName, string collectionName);

        /// <summary>
        /// Called when a database has been deleted.
        /// </summary>
        /// <param name="databaseName">The deleted database name.</param>
        void DeleteDatabase(string databaseName);
    }
}
