﻿using MapMaven.Core.Services.Interfaces;
using MapMaven.Utility;
using MudBlazor;
using MapMaven.Components.Updates;

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
                    config.VisibleStateDuration = (int)TimeSpan.FromMinutes(5).TotalMilliseconds;
                });
            });

            updateService.AvailableUpdate.Subscribe(update =>
            {
                snackbar.Add<UpdateNotification>(new() { { nameof(UpdateNotification.Update), update } }, Severity.Normal, config =>
                {
                    config.VisibleStateDuration = int.MaxValue;
                    config.Action = "Restart";
                    config.Icon = Icons.Material.Filled.Update;
                    config.Onclick = async snackbarItem => ApplicationUtils.RestartApplication();
                });
            });
        }
    }
}
