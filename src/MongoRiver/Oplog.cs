using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoRiver
{
    [BsonIgnoreExtraElements]
    public class Oplog
    {
        [BsonElement("ts")]
        public BsonTimestamp Timestamp { get; set; }

        [BsonElement("ns")]
        public string Namespace { get; set; }

        [BsonElement("op")]
        public string Operation { get; set; }

        [BsonElement("o")]
        public BsonDocument Object { get; set; }

        [BsonElement("o2")]
        public BsonDocument Object2 { get; set; }

        [BsonIgnore]
        public string Database
        {
            get
            {
                var ns = new OplogNamespace(Namespace);
                return ns.DatabaseName;
            }
        }

        [BsonIgnore]
        public string Collection
        {
            get
            {
                var ns = new OplogNamespace(Namespace);
                return ns.CollectionName;
            }
        }
    }
}
