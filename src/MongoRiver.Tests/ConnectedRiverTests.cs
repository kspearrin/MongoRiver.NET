using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoRiver;
using NSubstitute;

namespace MongoRiver.Tests
{
    [TestClass]
    public class ConnectedRiverTests
    {
        private const string MongoConnectionString = "mongodb://localhost:27017/?replicaSet=rs1";

        [TestMethod]
        public async Task Stream_ExecuteCorrectOperationsInCorrectOrder()
        {
            var client = new MongoClient(MongoConnectionString);
            var tailer = new Tailer(client);
            IOutlet output = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, output);
            Oplog lastOplog = await tailer.GetMostRecentOplog();

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";
            var newCollectionName = string.Concat(collectionName, "_foo");
            var insertedDocument = new FooBarDocument { Id = "foo", Bar = "baz" };
            var filterDocument = new BsonDocument("_id", "foo");
            var updatedDocument = new FooBarDocument { Id = "foo", Bar = "qux" };
            var indexName = "FooBar_Index";
            var indexKeyDocument = new BsonDocument("Bar", 1);
            var indexOptionsDocument = new BsonDocument("name", indexName);

            IMongoDatabase database = client.GetDatabase(databaseName);
            IMongoCollection<FooBarDocument> collection = database.GetCollection<FooBarDocument>(collectionName);

            await collection.InsertOneAsync(insertedDocument);
            await collection.ReplaceOneAsync(filterDocument, updatedDocument);
            await collection.DeleteOneAsync(filterDocument);

            IndexKeysDefinition<FooBarDocument> indexDef = new IndexKeysDefinitionBuilder<FooBarDocument>().Ascending(d => d.Bar);
            await collection.Indexes.CreateOneAsync(indexDef, new CreateIndexOptions { Name = indexName });
            await collection.Indexes.DropOneAsync(indexName);

            await database.RenameCollectionAsync(collectionName, newCollectionName);
            await database.DropCollectionAsync(newCollectionName);
            await client.DropDatabaseAsync(databaseName);

            await RunStream(stream, lastOplog);

            Received.InOrder(() =>
            {
                output.CreateCollection(databaseName, collectionName, new BsonDocument());

                output.Insert(databaseName, collectionName, insertedDocument.ToBsonDocument());
                output.Update(databaseName, collectionName, filterDocument, updatedDocument.ToBsonDocument());
                output.Delete(databaseName, collectionName, filterDocument);

                output.CreateIndex(databaseName, collectionName, indexKeyDocument, indexOptionsDocument);
                output.DeleteIndex(databaseName, collectionName, indexName);

                output.RenameCollection(databaseName, collectionName, newCollectionName);
                output.DeleteCollection(databaseName, newCollectionName);
                output.DeleteDatabase(databaseName);
            });
        }

        /// <summary>
        /// Run stream for 15 seconds.
        /// </summary>
        private async Task RunStream(Stream stream, Oplog startOplog)
        {
            var task = stream.RunForever(startOplog);
            await Task.WhenAny(task, Task.Delay(15000));
            stream.Stop();
        }
    }
}
