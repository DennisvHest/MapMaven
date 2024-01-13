using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace MapMaven.Core.Utilities
{
    public class CachedValue<T> where T : new()
    {
        public IObservable<T> ValueObservable { get; private set; }

        public T Value { get; private set; } = new();
        
        private Subject<long> _update = new();

        public CachedValue(Func<Task<T>> valueGetter, TimeSpan updatePeriod)
        {
            var cachedObservable = Observable.Merge(Observable.Timer(DateTimeOffset.UtcNow, updatePeriod), _update)
                .Select(async _ => Value = await valueGetter())
                .Concat()
                .Replay(1);

            cachedObservable.Connect();

            ValueObservable = cachedObservable;
        }

        public void UpdateValue()
        {
            _update.OnNext(0);
        }
    }
}
