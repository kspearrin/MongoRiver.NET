using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoRiver;
using NSubstitute;
using Xunit;

namespace MongoRiver.Tests
{
    /// <summary>
    /// These tests require you to have a replica set running that you can connect to.
    /// Set MongoConnectionString to this replica set.
    /// </summary>
    public class RiverConnectedTests
    {
        private const string MongoConnectionString = "mongodb://localhost:27017/?replicaSet=rs1";

        private readonly IMongoClient m_client;

        public RiverConnectedTests()
        {
            m_client = new MongoClient(MongoConnectionString);
        }

        [Fact]
        public async Task Stream_ExecuteCorrectOperationsInCorrectOrder()
        {
            var tailer = new Tailer(m_client);
            IOutlet outlet = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, outlet);
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

            IMongoDatabase database = m_client.GetDatabase(databaseName);
            IMongoCollection<FooBarDocument> collection = database.GetCollection<FooBarDocument>(collectionName);

            await collection.InsertOneAsync(insertedDocument);
            await collection.ReplaceOneAsync(filterDocument, updatedDocument);
            await collection.DeleteOneAsync(filterDocument);

            IndexKeysDefinition<FooBarDocument> indexDef = new IndexKeysDefinitionBuilder<FooBarDocument>().Ascending(d => d.Bar);
            await collection.Indexes.CreateOneAsync(indexDef, new CreateIndexOptions { Name = indexName });
            await collection.Indexes.DropOneAsync(indexName);

            await database.RenameCollectionAsync(collectionName, newCollectionName);
            await database.DropCollectionAsync(newCollectionName);
            await m_client.DropDatabaseAsync(databaseName);

            await RunStream(stream, lastOplog);

            outlet.Received(9).UpdateOptime(Arg.Any<BsonTimestamp>());

            Received.InOrder(() =>
            {
                outlet.CreateCollection(databaseName, collectionName, new BsonDocument());

                outlet.Insert(databaseName, collectionName, insertedDocument.ToBsonDocument());
                outlet.Update(databaseName, collectionName, filterDocument, updatedDocument.ToBsonDocument());
                outlet.Delete(databaseName, collectionName, filterDocument);

                outlet.CreateIndex(databaseName, collectionName, indexKeyDocument, indexOptionsDocument);
                outlet.DeleteIndex(databaseName, collectionName, indexName);

                outlet.RenameCollection(databaseName, collectionName, newCollectionName);
                outlet.DeleteCollection(databaseName, newCollectionName);
                outlet.DeleteDatabase(databaseName);
            });
        }

        [Fact]
        public async Task Stream_CreateCollectionWithOptions()
        {
            var tailer = new Tailer(m_client);
            IOutlet outlet = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, outlet);
            Oplog lastOplog = await tailer.GetMostRecentOplog();

            var collectionOptions = new CreateCollectionOptions { Capped = true, MaxSize = 10 };
            var collectionOptionsDocument = new BsonDocument(new List<BsonElement> { new BsonElement("capped", true), new BsonElement("size", 10) });

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";

            IMongoDatabase database = m_client.GetDatabase(databaseName);

            await database.CreateCollectionAsync(collectionName, collectionOptions);
            await m_client.DropDatabaseAsync(databaseName);

            await RunStream(stream, lastOplog);

            outlet.Received(2).UpdateOptime(Arg.Any<BsonTimestamp>());

            Received.InOrder(() =>
            {
                outlet.CreateCollection(databaseName, collectionName, collectionOptionsDocument);
                outlet.DeleteDatabase(databaseName);
            });
        }

        [Fact]
        public async Task Stream_IgnoresEverythingBeforeOperationPassedIn()
        {
            var tailer = new Tailer(m_client);
            IOutlet outlet = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, outlet);

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";

            var insertedDocument = new FooBarDocument { Id = "foo", Bar = "baz" };

            IMongoDatabase database = m_client.GetDatabase(databaseName);
            IMongoCollection<FooBarDocument> collection = database.GetCollection<FooBarDocument>(collectionName);

            await collection.InsertOneAsync(insertedDocument);

            // Need a little delay to make sure previous insert is available in the oplog
            await Task.Delay(100);

            Oplog lastOplog = await tailer.GetMostRecentOplog();
            await m_client.DropDatabaseAsync(databaseName);
            await RunStream(stream, lastOplog);

            outlet.Received(1).UpdateOptime(Arg.Any<BsonTimestamp>());
            outlet.DidNotReceive().Insert(databaseName, collectionName, insertedDocument.ToBsonDocument());
            outlet.Received().DeleteDatabase(databaseName);
        }

        [Fact]
        public async Task Stream_IgnoresEverythingBeforeTimestampPassedIn()
        {
            var tailer = new Tailer(m_client);
            IOutlet outlet = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, outlet);

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";

            var insertedDocument = new FooBarDocument { Id = "foo", Bar = "baz" };
            var filterDocument = new BsonDocument("_id", "foo");
            var updatedDocument = new FooBarDocument { Id = "foo", Bar = "qux" };

            IMongoDatabase database = m_client.GetDatabase(databaseName);
            IMongoCollection<FooBarDocument> collection = database.GetCollection<FooBarDocument>(collectionName);

            await collection.InsertOneAsync(insertedDocument);
            await collection.ReplaceOneAsync(filterDocument, updatedDocument);

            Oplog lastOplog = await tailer.GetMostRecentOplog(DateTime.UtcNow.AddSeconds(-3));
            await m_client.DropDatabaseAsync(databaseName);
            await RunStream(stream, lastOplog);

            outlet.Received(4).UpdateOptime(Arg.Any<BsonTimestamp>());
            outlet.Received().Insert(databaseName, collectionName, insertedDocument.ToBsonDocument());
            outlet.Received().Update(databaseName, collectionName, filterDocument.ToBsonDocument(), updatedDocument.ToBsonDocument());
            outlet.Received().DeleteDatabase(databaseName);
        }

        /// <summary>
        /// Run stream for 5 seconds.
        /// </summary>
        private async Task RunStream(Stream stream, Oplog startOplog)
        {
            var task = stream.RunForever(startOplog);
            await Task.WhenAny(task, Task.Delay(5000));
            stream.Stop();
        }
    }
}
