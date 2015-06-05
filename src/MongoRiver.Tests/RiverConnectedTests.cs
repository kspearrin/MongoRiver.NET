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

        [Fact]
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

            output.Received(9).UpdateOptime(Arg.Any<BsonTimestamp>());

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

        [Fact]
        public async Task Stream_CreateCollectionWithOptions()
        {
            var client = new MongoClient(MongoConnectionString);
            var tailer = new Tailer(client);
            IOutlet output = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, output);
            Oplog lastOplog = await tailer.GetMostRecentOplog();

            var collectionOptions = new CreateCollectionOptions { Capped = true, MaxSize = 10 };
            var collectionOptionsDocument = new BsonDocument(new List<BsonElement> { new BsonElement("capped", true), new BsonElement("size", 10) });

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";

            IMongoDatabase database = client.GetDatabase(databaseName);

            await database.CreateCollectionAsync(collectionName, collectionOptions);
            await client.DropDatabaseAsync(databaseName);

            await RunStream(stream, lastOplog);

            output.Received(2).UpdateOptime(Arg.Any<BsonTimestamp>());

            Received.InOrder(() =>
            {
                output.CreateCollection(databaseName, collectionName, collectionOptionsDocument);
                output.DeleteDatabase(databaseName);
            });
        }

        [Fact]
        public async Task Stream_IgnoresEverythingBeforeOperationPassedIn()
        {
            var client = new MongoClient(MongoConnectionString);
            var tailer = new Tailer(client);
            IOutlet output = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, output);

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";

            var insertedDocument = new FooBarDocument { Id = "foo", Bar = "baz" };

            IMongoDatabase database = client.GetDatabase(databaseName);
            IMongoCollection<FooBarDocument> collection = database.GetCollection<FooBarDocument>(collectionName);

            await collection.InsertOneAsync(insertedDocument);

            // Need a little delay to make sure previous insert is available in the oplog
            await Task.Delay(100);

            Oplog lastOplog = await tailer.GetMostRecentOplog();
            await client.DropDatabaseAsync(databaseName);
            await RunStream(stream, lastOplog);

            output.Received(1).UpdateOptime(Arg.Any<BsonTimestamp>());
            output.DidNotReceive().Insert(databaseName, collectionName, insertedDocument.ToBsonDocument());
            output.Received().DeleteDatabase(databaseName);
        }

        [Fact]
        public async Task Stream_IgnoresEverythingBeforeTimestampPassedIn()
        {
            var client = new MongoClient(MongoConnectionString);
            var tailer = new Tailer(client);
            IOutlet output = Substitute.For<IOutlet>();
            var stream = new Stream(tailer, output);

            var databaseName = "_Test_MongoRiver";
            var collectionName = "_Test_MongoRiver";

            var insertedDocument = new FooBarDocument { Id = "foo", Bar = "baz" };
            var filterDocument = new BsonDocument("_id", "foo");
            var updatedDocument = new FooBarDocument { Id = "foo", Bar = "qux" };

            IMongoDatabase database = client.GetDatabase(databaseName);
            IMongoCollection<FooBarDocument> collection = database.GetCollection<FooBarDocument>(collectionName);

            await collection.InsertOneAsync(insertedDocument);
            await collection.ReplaceOneAsync(filterDocument, updatedDocument);

            Oplog lastOplog = await tailer.GetMostRecentOplog(DateTime.UtcNow.AddSeconds(-3));
            await client.DropDatabaseAsync(databaseName);
            await RunStream(stream, lastOplog);

            output.Received(4).UpdateOptime(Arg.Any<BsonTimestamp>());
            output.Received().Insert(databaseName, collectionName, insertedDocument.ToBsonDocument());
            output.Received().Update(databaseName, collectionName, filterDocument.ToBsonDocument(), updatedDocument.ToBsonDocument());
            output.Received().DeleteDatabase(databaseName);
        }

        /// <summary>
        /// Run stream for 10 seconds.
        /// </summary>
        private async Task RunStream(Stream stream, Oplog startOplog)
        {
            var task = stream.RunForever(startOplog);
            await Task.WhenAny(task, Task.Delay(10000));
            stream.Stop();
        }
    }
}
