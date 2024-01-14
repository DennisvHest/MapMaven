using MapMaven.Core.Services.Interfaces;
using MudBlazor;

namespace MapMaven.Services
{
    public class ApplicationEventNotificationService
    {
        public ApplicationEventNotificationService(IApplicationEventService applicationEventService, UpdateService updateService, ISnackbar snackbar)
        {
            applicationEventService.ErrorRaised.Subscribe(error =>
            {
                snackbar.Add($"{error.Message} Error: {error.Exception.Message}", Severity.Error, config =>
                {
                    config.VisibleStateDuration = int.MaxValue;
                });
            });

            updateService.AvailableUpdate.Subscribe(update =>
            {
                snackbar.Add($"Update ({update?.TargetFullRelease?.Version}) has been downloaded. Restart to apply the update.", Severity.Normal, config =>
                {
                    config.VisibleStateDuration = int.MaxValue;
                    config.Action = "Restart";
                    config.Onclick = async snackbarItem => updateService.ApplyUpdatesAndRestart();
                });
            });
        }
    }
}
