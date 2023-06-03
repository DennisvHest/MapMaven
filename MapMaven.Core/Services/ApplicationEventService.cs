using System.Reactive.Subjects;
using MapMaven.Core.Models;

namespace MapMaven.Core.Services
{
    public class ApplicationEventService
    {
        private readonly ReplaySubject<ErrorEvent> _errorRaised = new(10);

        public IObservable<ErrorEvent> ErrorRaised => _errorRaised;

        public void RaiseError(ErrorEvent error)
        {
            _errorRaised.OnNext(error);
        }
    }
}
