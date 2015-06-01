# MongoRiver.NET

MongoRiver.NET is a library to monitor updates to your MongoDB databases in
near-realtime. It provides a simple interface for you to take actions when
records are inserted, updated, or deleted.

MongoRiver.NET was inspired by Stripe's [Mongoriver](https://github.com/stripe/mongoriver)
library written in Ruby.


## How it works

MongoDB has an *oplog*, a log of all write operations. MongoRiver.NET monitors
updates to this oplog and exposes a simple interface for each operation.
See the[MongoDB documentation for its oplog](http://docs.mongodb.org/manual/core/replica-set-oplog/)
for more info.


## How to use it

### Step 1: Create an outlet

You'll need to write a class that implements the
[`MongoRiver.IOutlet`](https://github.com/kspearrin/MongoRiver.NET/blob/master/src/MongoRiver/IOutlet.cs) interface.

`IOutlet` interface exposes the following methods that you can implement in your class:

```csharp
void UpdateOptime(BsonTimestamp timestamp);

void Insert(string databaseName, string collectionName, BsonDocument insertedDocument);
void Update(string databaseName, string collectionName, BsonDocument filterDocument, BsonDocument updatedDocument);
void Delete(string databaseName, string collectionName, BsonDocument filterDocument);

void CreateIndex(string databaseName, string collectionName, BsonDocument indexKeyDocument, BsonDocument optionsDocument);
void DeleteIndex(string databaseName, string collectionName, string indexName);

void CreateCollection(string databaseName, string collectionName, BsonDocument optionsDocument);
void RenameCollection(string databaseName, string collectionName, string newCollectionName);
void DeleteCollection(string databaseName, string collectionName);

void DeleteDatabase(string databaseName);
```

You should think of these methods like callbacks -- if you want to do something
every time a document is inserted into the MongoDB database, implement the
`Insert` method.

### Step 2: Create and run a stream

Once you've written your class, you can start tailing the MongoDB oplog! Here's
the code you'll need to use:

```csharp
var client = new MongoClient(MongoConnectionString);
var tailer = new Tailer(client);
IOutlet output = new YourOutlet(); // Your class that implements IOutlet here
var stream = new Stream(tailer, output);
stream.RunForever(startOplog);
```

`startOplog` here is the oplog you want the tailer to start at. We use
this to resume interrupted tailers so that no information is lost.


## Changelog

### 1.0.0

Initial release.
