using MapMaven.Core.Models;

namespace MapMaven.Core.Services.Interfaces
{
    public interface IApplicationEventService
    {
        IObservable<ErrorEvent> ErrorRaised { get; }

        void RaiseError(ErrorEvent error);
    }
}