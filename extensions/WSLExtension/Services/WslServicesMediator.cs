﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics;
using Microsoft.Win32;
using WSLExtension.ClassExtensions;
using WSLExtension.Contracts;
using WSLExtension.Exceptions;
using WSLExtension.Helpers;
using WSLExtension.Models;
using static Microsoft.Win32.Registry;
using static WSLExtension.Constants;

namespace WSLExtension.Services;

/// <summary>
/// Class to interact with WSL services either through the wsl.exe process
/// or the registry.
/// </summary>
public class WslServicesMediator : IWslServicesMediator
{
    private const int FirstIndex = 0;

    private readonly PackageHelper _packageHelper = new();

    private readonly IProcessCreator _processCreator;

    public WslServicesMediator(IProcessCreator creator)
    {
        _processCreator = creator;
    }

    /// <inheritdoc cref="IWslServicesMediator.GetAllNamesOfRunningDistributions"/>
    public HashSet<string> GetAllNamesOfRunningDistributions()
    {
        var processData = _processCreator.CreateProcessWithoutWindow(WslExe, ListAllRunningDistributions);

        // wsl.exe returns an error code when there are no distributions running. But in that case
        // it will send at least one line to standard output e.g "There are no running distributions."
        if (!processData.ExitedSuccessfully() && string.IsNullOrEmpty(processData.StdOutput))
        {
            throw new WslServicesMediatorException($"Unable to get all running distribution data" +
                $" process data: {processData}");
        }

        var distributions = new HashSet<string>();
        using var reader = new StringReader(processData.StdOutput);

        // Results for executing wsl.exe --list --running.
        // When no distributions are running the first line is localized: "There are no running distributions."
        // So we can skip it.
        // When there are distributions the first line is: "Windows Subsystem for Linux Distributions"
        // The rest of the lines are the running distribution names e.g
        // Debian (Default)
        // OracleLinux_7_9
        //
        // Note: the distribution that's set up as the default, will contain (default) next to it. But for our purposes
        // we don't need to read that part. We only need to first word of the space separated line. Distribution
        // names cannot have spaces so we don't need to worry about that either.
        reader.ReadLine();
        while (reader.ReadLine() is { } line)
        {
            var spaceSeparatedArr = line.Split(" ");
            distributions.Add(spaceSeparatedArr[FirstIndex]);
        }

        return distributions;
    }

    /// <inheritdoc cref="IWslServicesMediator.IsDistributionRunning"/>
    public bool IsDistributionRunning(string distributionName)
    {
        return GetAllNamesOfRunningDistributions().Contains(distributionName);
    }

    /// <inheritdoc cref="IWslServicesMediator.GetAllRegisteredDistributions"/>
    /// <remarks>
    /// Method enumerates through the WSL registry location subkey location
    /// to retrieve information about each registered distribution.
    /// </remarks>
    public List<WslRegisteredDistribution> GetAllRegisteredDistributions()
    {
        var distributions = new List<WslRegisteredDistribution>();
        var linuxSubSystemKey = CurrentUser.OpenSubKey(WslRegistryLocation, false);

        if (linuxSubSystemKey == null)
        {
            return new();
        }

        var defaultDistribution = linuxSubSystemKey.GetValue(DefaultDistributionRegistryName) as string;

        foreach (var subKeyName in linuxSubSystemKey.GetSubKeyNames())
        {
            var subKey = linuxSubSystemKey.OpenSubKey(subKeyName);

            if (subKey == null)
            {
                continue;
            }

            var distribution = BuildDistributionInfoFromRegistry(subKey);
            if (string.IsNullOrEmpty(distribution.Name))
            {
                // distribution doesn't have a name. This would happen only if the users registry info
                // was messed up. WSL would likely not function properly either in these cases.
                continue;
            }

            // the last part of the registry subkey is the registered guid of the wsl distribution.
            var distributionGuid = subKey.Name.Split('\\').LastOrDefault() ?? string.Empty;
            if (!string.IsNullOrEmpty(defaultDistribution) &&
                defaultDistribution.Equals(distributionGuid, StringComparison.OrdinalIgnoreCase))
            {
                distribution.IsDefaultDistribution = true;
            }

            distributions.Add(distribution);
        }

        return distributions;
    }

    private WslRegisteredDistribution BuildDistributionInfoFromRegistry(RegistryKey registryKey)
    {
        var regDistributionName = registryKey.GetValue(DistributionRegistryName) as string ?? string.Empty;
        var subkeyName = registryKey.Name.Split('\\').LastOrDefault();
        var isVersion2 = registryKey.GetValue(WslVersion) as int? == WslVersion2;
        var packageFamilyName = registryKey.GetValue(PackageFamilyRegistryName) as string;
        return new WslRegisteredDistribution(regDistributionName, subkeyName, packageFamilyName, isVersion2);
    }

    /// <inheritdoc cref="IWslServicesMediator.UnregisterDistribution"/>
    public void UnregisterDistribution(string distributionName)
    {
        var processData = _processCreator.CreateProcessWithoutWindow(WslExe, UnregisterDistributionArgs.FormatArgs(distributionName));

        if (!processData.ExitedSuccessfully() || string.IsNullOrEmpty(processData.StdOutput))
        {
            throw new WslServicesMediatorException($"Unable to launch distribution {distributionName} : {processData}");
        }
    }

    /// <inheritdoc cref="IWslServicesMediator.LaunchDistribution"/>
    public void LaunchDistribution(string distributionName, string? windowsTerminalProfile)
    {
        var executable = GetFileNameForProcessLaunch();

        // Only launch with terminal profile if one is known
        if (executable.Equals(WindowsTerminalShimExe, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrEmpty(windowsTerminalProfile))
        {
            var terminalArgs = LaunchDistributionProfileArgs.FormatArgs(windowsTerminalProfile, distributionName);
            _processCreator.CreateProcessWithWindow(GetFileNameForProcessLaunch(), terminalArgs);
            return;
        }

        _processCreator.CreateProcessWithWindow(executable, LaunchDistributionNoProfileArgs.FormatArgs(distributionName));
    }

    /// <inheritdoc cref="IWslServicesMediator.TerminateDistribution"/>
    public void TerminateDistribution(string distributionName)
    {
        var processData = _processCreator.CreateProcessWithoutWindow(WslExe, TerminateDistributionArgs.FormatArgs(distributionName));

        if (!processData.ExitedSuccessfully() || string.IsNullOrEmpty(processData.StdOutput))
        {
            throw new WslServicesMediatorException($"Unable to terminate distribution {distributionName} : {processData}");
        }
    }

    /// <inheritdoc cref="IWslServicesMediator.InstallDistribution"/>
    public void InstallDistribution(string distributionName)
    {
        _processCreator.CreateProcessWithWindow(GetFileNameForProcessLaunch(), InstallDistributionArgs.FormatArgs(distributionName));
    }

    private string GetFileNameForProcessLaunch()
    {
        return _packageHelper.IsPackageInstalled(WindowsTerminalPackageFamilyName) ? WindowsTerminalShimExe : CommandPromptExe;
    }
}