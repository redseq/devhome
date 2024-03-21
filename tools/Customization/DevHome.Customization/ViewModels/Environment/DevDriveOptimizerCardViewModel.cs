﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using DevHome.Common.Environments.Models;
using DevHome.Common.Models;
using DevHome.Common.Services;
using DevHome.SetupFlow.Utilities;

using Dispatching = Microsoft.UI.Dispatching;

namespace DevHome.Customization.ViewModels.Environments;

/// <summary>
/// View model for the card that represents a dev drive on the setup target page.
/// </summary>
public partial class DevDriveOptimizerCardViewModel : ObservableObject
{
    private readonly Dispatching.DispatcherQueue _dispatcher;

    public string CacheToBeMoved { get; set; }

    public string ExistingCacheLocation { get; set; }

    public string ExampleLocationOnDevDrive { get; set; }

    public string EnvironmentVariableToBeSet { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private DevDriveState _cardState;

    [ObservableProperty]
    private CardStateColor _stateColor;

    public DevDriveOptimizerCardViewModel(string cacheToBeMoved, string existingCacheLocation, string exampleLocationOnDevDrive, string environmentVariableToBeSet)
    {
        _dispatcher = Dispatching.DispatcherQueue.GetForCurrentThread();

        CacheToBeMoved = cacheToBeMoved;
        ExistingCacheLocation = existingCacheLocation;
        ExampleLocationOnDevDrive = exampleLocationOnDevDrive;
        EnvironmentVariableToBeSet = environmentVariableToBeSet;
    }
}