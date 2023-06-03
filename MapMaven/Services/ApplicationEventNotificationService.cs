using MapMaven.Core.Services;
using MudBlazor;

namespace MapMaven.Services
{
    public class ApplicationEventNotificationService
    {
        public ApplicationEventNotificationService(ApplicationEventService applicationEventService, ISnackbar snackbar)
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
