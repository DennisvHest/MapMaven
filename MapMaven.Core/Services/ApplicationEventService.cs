using System.Reactive.Subjects;
using MapMaven.Core.Models;
using Microsoft.Extensions.Logging;

namespace MapMaven.Core.Services
{
    public class ApplicationEventService
    {
        private readonly ILogger<ApplicationEventService> _logger;

        private readonly ReplaySubject<ErrorEvent> _errorRaised = new(10);

        public IObservable<ErrorEvent> ErrorRaised => _errorRaised;

        public ApplicationEventService(ILogger<ApplicationEventService> logger)
        {
            _logger = logger;
        }

        public void RaiseError(ErrorEvent error)
        {
            _logger.LogError(error.Exception, $"Error raised. Message: {error.Message}");
            _errorRaised.OnNext(error);
        }
    }
}
