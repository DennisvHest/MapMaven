using System.Reactive.Linq;

namespace MapMaven.Core.Extensions
{
    public static class ObservableExtensions
    {
        public static IDisposable SubscribeAsync<TResult>(this IObservable<TResult> source, Func<TResult, Task> action) =>
            source.Select(x => Observable.FromAsync(() => action(x)))
                .Concat()
                .Subscribe();
    }
}
