using MapMaven.Core.Services;
using MapMaven.Core.Services.Interfaces;
using MudBlazor;

namespace MapMaven.Services
{
    public class ApplicationEventNotificationService
    {
        public ApplicationEventNotificationService(IApplicationEventService applicationEventService, ISnackbar snackbar)
        {
            applicationEventService.ErrorRaised.Subscribe(error =>
            {
                snackbar.Add($"{error.Message} Error: {error.Exception.Message}", Severity.Error, config =>
                {
                    config.VisibleStateDuration = int.MaxValue;
                });
            });
        }
    }
}
