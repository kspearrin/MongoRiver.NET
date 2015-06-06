# MongoRiver.NET ![Logo](http://i.imgur.com/9M2oAwT.png)

[![Build Status](https://travis-ci.org/kspearrin/MongoRiver.NET.svg?branch=master)](https://travis-ci.org/kspearrin/MongoRiver.NET)

MongoRiver.NET is a library to monitor updates to your MongoDB databases in
near-realtime. It provides a simple interface for you to take actions when
records are inserted, updated, or deleted.

MongoRiver.NET was inspired by Stripe's [Mongoriver](https://github.com/stripe/mongoriver)
library written in Ruby.


## How it works

MongoDB has an *oplog*, a log of all write operations. MongoRiver.NET monitors
updates to this oplog and exposes a simple interface for each operation.
See the [MongoDB documentation for its oplog](http://docs.mongodb.org/manual/core/replica-set-oplog/)
for more info.


## How to use it

### Step 1: Install and use

You can install MongoRiver.NET from [nuget](https://www.nuget.org/packages/MongoRiver.NET):

    PM> Install-Package MongoRiver.NET

Or download and build the source from here and reference the `MongoRiver.dll`.

Then add using statements:

```csharp
using MongoRiver;
```

### Step 2: Create an outlet

You'll need to write a class that implements the
[`MongoRiver.IOutlet`](https://github.com/kspearrin/MongoRiver.NET/blob/master/src/MongoRiver/IOutlet.cs) interface.

The `IOutlet` interface exposes the following methods that you can implement in your class:

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

### Step 3: Create and run a stream

Once you've written your class, you can start tailing the MongoDB oplog! Here's
the code you'll need to use:

```csharp
var client = new MongoClient(YourMongoConnectionString);
var tailer = new Tailer(client);
IOutlet output = new YourOutlet(); // Your class that implements IOutlet here
var stream = new Stream(tailer, output);
stream.RunForever(startOplog);
```

`startOplog` here is the oplog you want the tailer to start at. We use
this to resume interrupted tailers so that no information is lost.

You should save the oplog timestamp each time the `UpdateOptime` method is called.
This allows you to retreive the `startOplog` when resuming a previous running
tailer that was interrupted:

The `tailer` object exposes methods that you can use to get the start oplog.

```csharp
Oplog startOplog = await tailer.GetMostRecentOplog();

// or
Oplog startOplog = await tailer.GetMostRecentOplog(lastKnowProcessedOplogTimestamp);

// or
Oplog startOplog = await tailer.GetMostRecentOplog(lastKnowProcessedOplogDateTime);
```

## Changelog

### 1.0.0

Initial release.
