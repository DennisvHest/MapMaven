using Microsoft.AspNetCore.Components;
namespace BeatSaberTools.Extensions
{
    public class ReactiveComponent : ComponentBase
    {
        protected IDisposable SubscribeAndBind<T>(IObservable<T> observable, Action<T> bindAction)
        {
            return observable.Subscribe(x =>
            {
                bindAction(x);
                InvokeAsync(StateHasChanged);
            });
        }
    }
}
