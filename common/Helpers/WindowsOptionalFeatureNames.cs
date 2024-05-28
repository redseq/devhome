﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using DevHome.Common.Environments.Helpers;

namespace DevHome.Common.Helpers;

public static class WindowsOptionalFeatureNames
{
    public const string Containers = "Containers";
    public const string GuardedHost = "HostGuardian";
    public const string HyperV = "Microsoft-Hyper-V-All";
    public const string HyperVManagementTools = "Microsoft-Hyper-V-Tools-All";
    public const string HyperVPlatform = "Microsoft-Hyper-V";
    public const string VirtualMachinePlatform = "VirtualMachinePlatform";
    public const string WindowsHypervisorPlatform = "HypervisorPlatform";
    public const string WindowsSandbox = "Containers-DisposableClientVM";
    public const string WindowsSubsystemForLinux = "Microsoft-Windows-Subsystem-Linux";

    public static IEnumerable<string> VirtualMachineFeatures => new[]
    {
        Containers,
        GuardedHost,
        HyperV,
        HyperVManagementTools,
        HyperVPlatform,
        VirtualMachinePlatform,
        WindowsHypervisorPlatform,
        WindowsSandbox,
        WindowsSubsystemForLinux,
    };

    public static readonly Dictionary<string, string> FeatureDescriptions = new()
    {
        { Containers, GetFeatureDescription(nameof(Containers)) },
        { GuardedHost, GetFeatureDescription(nameof(GuardedHost)) },
        { HyperV, GetFeatureDescription(nameof(HyperV)) },
        { HyperVManagementTools, GetFeatureDescription(nameof(HyperVManagementTools)) },
        { HyperVPlatform, GetFeatureDescription(nameof(HyperVPlatform)) },
        { VirtualMachinePlatform, GetFeatureDescription(nameof(VirtualMachinePlatform)) },
        { WindowsHypervisorPlatform, GetFeatureDescription(nameof(WindowsHypervisorPlatform)) },
        { WindowsSandbox, GetFeatureDescription(nameof(WindowsSandbox)) },
        { WindowsSubsystemForLinux, GetFeatureDescription(nameof(WindowsSubsystemForLinux)) },
    };

    private static string GetFeatureDescription(string featureName)
    {
        return StringResourceHelper.GetResource(featureName + "Description");
    }
}