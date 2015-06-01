using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoRiver.Tests
{
    public class FooBarDocument
    {
        [BsonId]
        public string Id { get; set; }

        public string Bar { get; set; }
    }
}
