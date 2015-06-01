using MongoDB.Bson;

namespace MongoRiver
{
    public class OplogNamespace
    {
        public OplogNamespace() { }

        public OplogNamespace(BsonValue bsonValue)
        {
            if(bsonValue == null)
            {
                throw new MongoRiverException("Namespace not defined on document.");
            }

            BuildParts(bsonValue.ToString());
        }

        public OplogNamespace(string nsString)
        {
            BuildParts(nsString);
        }

        public string DatabaseName { get; set; }

        public string CollectionName { get; set; }

        private void BuildParts(string nsString)
        {
            if(string.IsNullOrWhiteSpace(nsString))
            {
                throw new MongoRiverException("Namespace not defined on document.");
            }

            string[] parts = Utils.SplitNamespace(nsString);

            if(parts.Length > 0)
            {
                DatabaseName = parts[0];
            }
            else
            {
                throw new MongoRiverException("Part (database name) is unavailable on document namespace.");
            }

            if(parts.Length > 1)
            {
                CollectionName = parts[1];
            }
            else
            {
                throw new MongoRiverException("Part (collection name) is unavailable on document namespace.");
            }
        }
    }
}
