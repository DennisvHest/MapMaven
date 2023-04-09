using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace MapMaven.Extensions
{
    public class ReactiveComponent : ComponentBase, IDisposable
    {
        private List<IDisposable> _subscriptions = new List<IDisposable>();

        [Inject]
        private ILogger<ReactiveComponent> Logger { get; set; }

        [Inject]
        private ISnackbar Snackbar { get; set; }

        protected IDisposable SubscribeAndBind<T>(IObservable<T> observable, Action<T> bindAction)
        {
            var subscription = observable.Subscribe(x =>
            {
                bindAction(x);
                InvokeAsync(StateHasChanged);
            }, exception =>
            {
                Logger.LogError(exception, "Error in observable.");
                Snackbar.Add($"An unhandled error has occured: {exception.Message}", Severity.Error, config =>
                {
                    config.VisibleStateDuration = int.MaxValue;
                });
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
