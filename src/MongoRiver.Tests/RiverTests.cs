using System;
using MongoDB.Driver;
using Xunit;
using NSubstitute;
using MongoDB.Bson;
using System.Collections.Generic;

namespace MongoRiver.Tests
{
    public class RiverTests
    {
        private readonly Tailer m_tailer = Substitute.For<Tailer>();
        private readonly IOutlet m_outlet = Substitute.For<IOutlet>();
        private readonly Stream m_stream;
        private readonly BsonTimestamp m_timestamp = new BsonTimestamp(1, 0);

        public RiverTests()
        {
            m_stream = new Stream(m_tailer, m_outlet);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationInsert()
        {
            var insertedDocument = new BsonDocument("_id", "baz");
            Oplog op = CreateOperation("foo.bar", "i", insertedDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).Insert("foo", "bar", insertedDocument);
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationUpdate()
        {
            var updatedDocument = new BsonDocument("_id", "baz");
            var filterDocument = new BsonDocument("a", "b");
            Oplog op = CreateOperation("foo.bar", "u", updatedDocument, filterDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).Update("foo", "bar", filterDocument, updatedDocument);
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationDelete()
        {
            var filterDocument = new BsonDocument("_id", "baz");
            Oplog op = CreateOperation("foo.bar", "d", filterDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).Delete("foo", "bar", filterDocument);
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationCreateCollection()
        {
            var createDocument = new BsonDocument(new List<BsonElement>
            {
                new BsonElement("create", "bar"),
                new BsonElement("capped", true),
                new BsonElement("size", 10)
            });

            var receivedCreateDocument = new BsonDocument(new List<BsonElement>
            {
                new BsonElement("capped", true),
                new BsonElement("size", 10)
            });

            Oplog op = CreateOperation("foo.$cmd", "c", createDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).CreateCollection("foo", "bar", receivedCreateDocument);
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationRenameCollection()
        {
            var renameDocument = new BsonDocument(new List<BsonElement>
            {
                new BsonElement("renameCollection", "foo.bar"),
                new BsonElement("to", "foo.bar_2")
            });

            Oplog op = CreateOperation("admin.$cmd", "c", renameDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).RenameCollection("foo", "bar", "bar_2");
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationDeleteCollection()
        {
            var deleteDocument = new BsonDocument(new BsonElement("drop", "bar"));
            Oplog op = CreateOperation("foo.$cmd", "c", deleteDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).DeleteCollection("foo", "bar");
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationCreateIndex()
        {
            var indexKeyDocument = new BsonDocument("baz", 1);

            var optionsDocument = new BsonDocument(new List<BsonElement>
            {
                new BsonElement("_id", "index_id"),
                new BsonElement("name", "baz_1"),
                new BsonElement("ns", "foo.bar"),
                new BsonElement("key", indexKeyDocument)
            });

            var receivedOptionsDocument = new BsonDocument("name", "baz_1");

            Oplog op = CreateOperation("foo.system.indexes", "i", optionsDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).CreateIndex("foo", "bar", indexKeyDocument, receivedOptionsDocument);
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationDeleteIndex()
        {
            var deleteDocument = new BsonDocument(new List<BsonElement>
            {
                new BsonElement("dropIndexes", "bar"),
                new BsonElement("index", "baz_1")
            });

            Oplog op = CreateOperation("foo.$cmd", "c", deleteDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).DeleteIndex("foo", "bar", "baz_1");
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        [Fact]
        [Trait("Build", "Run")]
        public void Stream_HandleOperationDeleteDatabase()
        {
            var deleteDocument = new BsonDocument("dropDatabase", 1);
            Oplog op = CreateOperation("foo.$cmd", "c", deleteDocument);

            m_stream.HandleOperation(op);
            m_outlet.Received(1).DeleteDatabase("foo");
            m_outlet.Received(1).UpdateOptime(m_timestamp);
        }

        private Oplog CreateOperation(string ns, string operation, BsonDocument obj = null, BsonDocument obj2 = null)
        {
            return new Oplog
            {
                Namespace = ns,
                Operation = operation,
                Timestamp = m_timestamp,
                Object = obj,
                Object2 = obj2
            };
        }
    }
}
