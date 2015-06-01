using System;

namespace MongoRiver
{
    public class MongoRiverException : ApplicationException
    {
        public MongoRiverException() { }

        public MongoRiverException(string message)
            : base(message) { }
    }
}
