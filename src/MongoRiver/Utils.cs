using System;

namespace MongoRiver
{
    public static class Utils
    {
        /// <summary>
        /// Convert a DateTime to number seconds from epoc (1/1/1970)
        /// </summary>
        /// <param name="dateTime">The date to convert</param>
        /// <returns>Number of seconds from epoc (1/1/1970)</returns>
        public static int ToInt(this DateTime dateTime)
        {
            TimeSpan t = dateTime - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return (int)Math.Floor(t.TotalSeconds);
        }

        /// <summary>
        /// Split a oplog namespace string into two parts from the first period in the string.
        /// </summary>
        /// <param name="ns">Oplog namespace</param>
        /// <returns>Two strings that make up the namespace. Usually [0] = database name and [1] = collection name.</returns>
        public static string[] SplitNamespace(string ns)
        {
            return ns.Split(new char[] { '.' }, 2);
        }
    }
}
