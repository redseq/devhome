﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using DevHome.Common.TelemetryEvents.RepositoryManagement;
using DevHome.Common.Windows.FileDialog;
using DevHome.RepositoryManagement.Services;
using DevHome.Telemetry;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;

namespace DevHome.RepositoryManagement.ViewModels;

public partial class RepositoryManagementItemViewModel
{
    public const string EventName = "DevHome_RepositorySpecific_Event";

    public const string ErrorEventName = "DevHome_RepositorySpecificError_Event";

    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(RepositoryManagementItemViewModel));

    private readonly Window _window;

    private readonly RepositoryManagementDataAccessService _dataAccess;

    private string _repositoryName;

    /// <summary>
    /// Gets or sets the name of the repository.  Nulls are converted to string.empty.
    /// </summary>
    public string RepositoryName
    {
        get => _repositoryName ?? string.Empty;

        set => _repositoryName = value ?? string.Empty;
    }

    private string _clonePath;

    /// <summary>
    /// Gets or sets the local path the repository is cloned to.  Nulls are converted to string.empty.
    /// </summary>
    public string ClonePath
    {
        get => _clonePath ?? string.Empty;

        set => _clonePath = value ?? string.Empty;
    }

    private string _latestCommit;

    /// <summary>
    /// Gets or sets the latest commit.  Nulls are converted to string.empty.
    /// </summary>
    /// <remarks>
    /// TODO: Test values are strings only.
    /// </remarks>
    public string LatestCommit
    {
        get => _latestCommit ?? string.Empty;

        set => _latestCommit = value ?? string.Empty;
    }

    private string _branch;

    /// <summary>
    /// Gets or sets the local branch name.  Nulls are converted to string.empty.
    /// </summary>
    public string Branch
    {
        get => _branch ?? string.Empty;
        set => _branch = value ?? string.Empty;
    }

    public bool IsHiddenFromPage { get; set; }

    [RelayCommand]
    public async Task OpenInFileExplorer()
    {
        // Ask the user if they can point DevHome to the correct location
        if (!Directory.Exists(Path.GetFullPath(ClonePath)))
        {
            await CloneLocationNotFoundNotifyUser(RepositoryName);
        }

        OpenRepositoryInFileExplorer(RepositoryName, ClonePath, nameof(OpenInFileExplorer));
    }

    [RelayCommand]
    public async Task OpenInCMD()
    {
        // Ask the user if they can point DevHome to the correct location
        if (!Directory.Exists(Path.GetFullPath(ClonePath)))
        {
            await CloneLocationNotFoundNotifyUser(RepositoryName);
        }

        OpenRepositoryinCMD(RepositoryName, ClonePath, nameof(OpenInCMD));
    }

    [RelayCommand]
    public async Task MoveRepository()
    {
        var newLocation = await PickNewLocationForRepositoryAsync();
        if (string.IsNullOrEmpty(newLocation))
        {
            _log.Information("The path from the folder picker is either null or empty.  Not updating the clone path");
            return;
        }

        var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

        // The user clicked on this menu from the repository management page.
        // The repository should be in the database.
        // Somehow getting the repository returned null.
        if (repository is null)
        {
            _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
            TelemetryFactory.Get<ITelemetry>().Log(
                EventName,
                LogLevel.Critical,
                new RepositoryLineItemEvent(nameof(OpenInFileExplorer), RepositoryName));

            return;
        }

        var newDirectoryInfo = new DirectoryInfo(Path.Join(newLocation, RepositoryName));
        var currentDirectoryInfo = new DirectoryInfo(Path.GetFullPath(ClonePath));
        currentDirectoryInfo.MoveTo(newDirectoryInfo.FullName);

        // The repository exists at the location stored in the Database
        // and the new location is set.
        var didUpdate = _dataAccess.UpdateCloneLocation(repository, newDirectoryInfo.FullName);

        if (!didUpdate)
        {
            _log.Warning($"Could not update the database.  Check logs");
        }
    }

    [RelayCommand]
    public void DeleteRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void MakeConfigurationFileWithThisRepository()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void OpenFileExplorerToConfigurationsFolder()
    {
        throw new NotImplementedException();
    }

    [RelayCommand]
    public void RemoveThisRepositoryFromTheList()
    {
        throw new NotImplementedException();
    }

    public RepositoryManagementItemViewModel(Window window, RepositoryManagementDataAccessService dataAccess)
    {
        _window = window;
        _dataAccess = dataAccess;
    }

    private void OpenRepositoryInFileExplorer(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in File Explorer at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(action, repositoryName));

        var processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = true,

            // Not catching PathTooLongException.  If the file was in a location that had a too long path,
            // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
            FileName = Path.GetFullPath(cloneLocation),
        };

        StartProcess(processStartInfo, action);
    }

    private void OpenRepositoryinCMD(string repositoryName, string cloneLocation, string action)
    {
        _log.Information($"Showing {repositoryName} in CMD at location {cloneLocation}");
        TelemetryFactory.Get<ITelemetry>().Log(
            EventName,
            LogLevel.Critical,
            new RepositoryLineItemEvent(action, repositoryName));

        var processStartInfo = new ProcessStartInfo
        {
            UseShellExecute = true,

            // Not catching PathTooLongException.  If the file was in a location that had a too long path,
            // the repo, when cloning, would run into a PathTooLongException and repo would not be cloned.
            FileName = "CMD",
            WorkingDirectory = Path.GetFullPath(cloneLocation),
        };

        StartProcess(processStartInfo, action);
    }

    private void StartProcess(ProcessStartInfo processStartInfo, string operation)
    {
        try
        {
            Process.Start(processStartInfo);
        }
        catch (Exception e)
        {
            SendTelemetryAndLogError(operation, e);
        }
    }

    /// <summary>
    /// Opens the directory picker
    /// </summary>
    /// <returns>A string that is the full path the user chose.</returns>
    private async Task<string> PickNewLocationForRepositoryAsync()
    {
        try
        {
            _log.Information("Opening folder picker to select a new location");
            using var folderPicker = new WindowOpenFolderDialog();
            var newLocation = await folderPicker.ShowAsync(_window);
            if (newLocation != null && newLocation.Path.Length > 0)
            {
                _log.Information($"Selected '{newLocation.Path}' for the repository path.");
                return Path.GetFullPath(newLocation.Path);
            }
            else
            {
                _log.Information("Didn't select a location to clone to");
                return null;
            }
        }
        catch (Exception e)
        {
            _log.Error(e, "Failed to open folder picker");
            return null;
        }
    }

    private void SendTelemetryAndLogError(string operation, Exception ex)
    {
        TelemetryFactory.Get<ITelemetry>().LogError(
        ErrorEventName,
        LogLevel.Critical,
        new RepositoryLineItemErrorEvent(operation, ex.HResult, ex.Message, RepositoryName));

        _log.Error(ex, string.Empty);
    }

    private async Task CloneLocationNotFoundNotifyUser(
        string repositoryName)
    {
        // strings need to be localized
        var cantFindRepositoryDialog = new ContentDialog()
        {
            XamlRoot = _window.Content.XamlRoot,
            Title = $"Can not find {RepositoryName}.",
            Content = $"Cannot find {RepositoryName} at {Path.GetFullPath(ClonePath)}.  Do you know where it is?",
            PrimaryButtonText = $"Locate {RepositoryName} via File Explorer.",
            SecondaryButtonText = "Remove from list",
            CloseButtonText = "Cancel",
        };

        var dialogResult = await cantFindRepositoryDialog.ShowAsync();

        // User will show DevHome where the repository is.
        // Open the folder picker.
        // Maybe don't close the dialog until the user is done
        // with the folder picker.
        if (dialogResult == ContentDialogResult.Primary)
        {
            var newLocation = await PickNewLocationForRepositoryAsync();
            if (string.IsNullOrEmpty(newLocation))
            {
                _log.Information("The path from the folder picker is either null or empty.  Not updating the clone path");
                return;
            }

            var repository = _dataAccess.GetRepository(RepositoryName, ClonePath);

            // The user clicked on this menu from the repository management page.
            // The repository should be in the database.
            // Somehow getting the repository returned null.
            if (repository is null)
            {
                _log.Warning($"The repository with name {RepositoryName} and clone location {ClonePath} is not in the database when it is expected to be there.");
                TelemetryFactory.Get<ITelemetry>().Log(
                    EventName,
                    LogLevel.Critical,
                    new RepositoryLineItemEvent(nameof(OpenInFileExplorer), repositoryName));

                return;
            }

            // The repository exists at the location stored in the Database
            // and the new location is set.
            var didUpdate = _dataAccess.UpdateCloneLocation(repository, Path.Combine(newLocation, RepositoryName));

            if (!didUpdate)
            {
                _log.Warning($"Could not update the database.  Check logs");
            }
        }
        else if (dialogResult == ContentDialogResult.Secondary)
        {
            RemoveThisRepositoryFromTheList();
            return;
        }
    }
}
