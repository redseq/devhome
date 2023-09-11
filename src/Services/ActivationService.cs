﻿// Copyright (c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using DevHome.Activation;
using DevHome.Common.Contracts;
using DevHome.Common.Extensions;
using DevHome.Common.Helpers;
using DevHome.Contracts.Services;
using DevHome.Views;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace DevHome.Services;

public class ActivationService : IActivationService
{
    private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
    private readonly IEnumerable<IActivationHandler> _activationHandlers;
    private readonly IThemeSelectorService _themeSelectorService;
    private readonly ILocalSettingsService _localSettingsService;

    private bool _isInitialActivation = true;

    public ActivationService(
        ActivationHandler<LaunchActivatedEventArgs> defaultHandler,
        IEnumerable<IActivationHandler> activationHandlers,
        IThemeSelectorService themeSelectorService,
        ILocalSettingsService localSettingsService)
    {
        _defaultHandler = defaultHandler;
        _activationHandlers = activationHandlers;
        _themeSelectorService = themeSelectorService;
        _localSettingsService = localSettingsService;
    }

    public async Task ActivateAsync(object activationArgs)
    {
        if (_isInitialActivation)
        {
            _isInitialActivation = false;

            // Execute tasks before activation.
            await InitializeAsync();

            // Set the MainWindow Content.
            App.MainWindow.Content = await _localSettingsService.ReadSettingAsync<bool>(WellKnownSettingsKeys.IsNotFirstRun)
                ? Application.Current.GetService<ShellPage>()
                : Application.Current.GetService<InitializationPage>();

            // Activate the MainWindow.
            App.MainWindow.Activate();

            // Execute tasks after activation.
            await StartupAsync();

            // File activation should only be handled after the application is launched
            var activatedEventArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            if (activatedEventArgs.Kind == ExtendedActivationKind.File)
            {
                return;
            }
        }

        // Handle activation via ActivationHandlers.
        await HandleActivationAsync(activationArgs);
    }

    private async Task HandleActivationAsync(object activationArgs)
    {
        var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

        if (activationHandler != null)
        {
            await activationHandler.HandleAsync(activationArgs);
        }
    }

    private async Task InitializeAsync()
    {
        await _themeSelectorService.InitializeAsync().ConfigureAwait(false);
        await Task.CompletedTask;
    }

    private async Task StartupAsync()
    {
        // Subscribe to theme changes.
        _themeSelectorService.ThemeChanged += (_, theme) => App.MainWindow.SetRequestedTheme(theme);
        _themeSelectorService.SetRequestedTheme();
        await Task.CompletedTask;
    }
}
