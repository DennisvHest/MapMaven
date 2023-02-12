using Microsoft.AspNetCore.Components;
namespace MapMaven.Extensions
{
    public class ReactiveComponent : ComponentBase, IDisposable
    {
        private List<IDisposable> _subscriptions = new List<IDisposable>();

        protected IDisposable SubscribeAndBind<T>(IObservable<T> observable, Action<T> bindAction)
        {
            var subscription = observable.Subscribe(x =>
            {
                bindAction(x);
                InvokeAsync(StateHasChanged);
            });

            _subscriptions.Add(subscription);

            return subscription;
        }

        public void Dispose()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
