using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace MongoRiver
{
    public class ObservableFactory
    {
        private readonly MongoClient _client;

        public ObservableFactory(MongoClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException("client");
            }
            _client = client;
        }
        
        public IObservable<OpLogEvent> Create(DateTime startDate)
        {
            return CreateInternal((stream, _) => stream.RunForever(startDate));
        }

        public IObservable<OpLogEvent> Create(BsonTimestamp startTime)
        {
            return CreateInternal((stream, _) => stream.RunForever(startTime));
        }

        public IObservable<OpLogEvent> Create(Oplog startOplog = null)
        {
            return CreateInternal((stream, _) => stream.RunForever(startOplog));
        }

        public IObservable<OpLogEvent> CreateMostRecent()
        {
            return CreateInternal((stream, tailer) =>
            {
                var task = tailer.GetMostRecentOplog();
                task.Wait();
                return stream.RunForever(task.Result);
            });
        }

        private IObservable<OpLogEvent> CreateInternal(Func<Stream, Tailer, Task> runFunc)
        {
            var observable = Observable.Create<OpLogEvent>(observer =>
            {
                IOutlet output = new ObserverOutlet(observer);
                var tailer = new Tailer(_client);
                var stream = new Stream(tailer, output);
                runFunc(stream, tailer);
                return () =>
                {
                    stream.Stop();
                };
            });
            return observable;
        }
    }
}
