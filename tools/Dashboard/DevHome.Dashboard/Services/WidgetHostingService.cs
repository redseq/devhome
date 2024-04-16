﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Hosts;
using Serilog;

namespace DevHome.Dashboard.Services;

public class WidgetHostingService : IWidgetHostingService
{
    private readonly ILogger _log = Log.ForContext("SourceContext", nameof(WidgetHostingService));

    private WidgetHost _widgetHost;
    private WidgetCatalog _widgetCatalog;

    // RPC error codes to recover from
    private const int RpcServerUnavailable = unchecked((int)0x800706BA);
    private const int RpcCallFailed = unchecked((int)0x800706BE);

    private const int MaxAttempts = 3;

    /// <summary>
    /// Get the list of current widgets from the WidgetService.
    /// </summary>
    /// <returns>A list of widgets, or null if there were no widgets or the list could not be retrieved.</returns>
    public async Task<Widget[]> GetWidgetsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(() => _widgetHost.GetWidgets());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetHost before trying again. Also reset the WidgetCatalog,
                // since if we lost the host we probably lost the catalog too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception getting widgets from service:");
            }
        }

        return null;
    }

    /// <summary>
    /// Create and return a new widget.
    /// </summary>
    /// <returns>The new widget, or null if one could not be created.</returns>
    public async Task<Widget> CreateWidgetAsync(string widgetDefinitionId, WidgetSize widgetSize)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetHost ??= await Task.Run(() => WidgetHost.Register(new WidgetHostContext("BAA93438-9B07-4554-AD09-7ACCD7D4F031")));
                return await Task.Run(async () => await _widgetHost.CreateWidgetAsync(widgetDefinitionId, widgetSize));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception creating a widget:");
            }
        }

        return null;
    }

    /// <summary>
    /// Get the catalog of widgets from the WidgetService.
    /// </summary>
    /// <returns>The catalog of widgets, or null if one could not be created.</returns>
    public async Task<WidgetCatalog> GetWidgetCatalogAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());

                // Need to use an arbitrary method to check if the COM object is still alive.
                await Task.Run(() => _widgetCatalog.GetWidgetDefinition("fakeWidgetDefinitionId"));

                // If the above call didn't throw, the object is still alive.
                return _widgetCatalog;
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
                _widgetCatalog = null;
            }
        }

        return _widgetCatalog;
    }

    /// <summary>
    /// Get the list of WidgetProviderDefinitions from the WidgetService.
    /// </summary>
    /// <returns>A list of WidgetProviderDefinitions, or an empty list if there were no widgets
    /// or the list could not be retrieved.</returns>
    public async Task<WidgetProviderDefinition[]> GetProviderDefinitionsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetProviderDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return [];
    }

    /// <summary>
    /// Get the list of WidgetDefinitions from the WidgetService.
    /// </summary>
    /// <returns>A list of WidgetDefinitions, or an empty list if there were no widgets
    /// or the list could not be retrieved.</returns>
    public async Task<WidgetDefinition[]> GetWidgetDefinitionsAsync()
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinitions());
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return [];
    }

    /// <summary>
    /// Get the WidgetDefinition for the given WidgetDefinitionId from the WidgetService.
    /// </summary>
    /// <returns>The WidgetDefinition, or null if the widget definition could not be found
    /// or there was an error retrieving it.</returns>
    public async Task<WidgetDefinition> GetWidgetDefinitionAsync(string widgetDefinitionId)
    {
        var attempt = 0;
        while (attempt++ < MaxAttempts)
        {
            try
            {
                _widgetCatalog ??= await Task.Run(() => WidgetCatalog.GetDefault());
                return await Task.Run(() => _widgetCatalog.GetWidgetDefinition(widgetDefinitionId));
            }
            catch (COMException ex) when (ex.HResult == RpcServerUnavailable || ex.HResult == RpcCallFailed)
            {
                _log.Warning(ex, $"Failed to operate on out-of-proc object with error code: 0x{ex.HResult:x}");

                // Force getting a new WidgetCatalog before trying again. Also reset the WidgetHost,
                // since if we lost the catalog we probably lost the host too.
                _widgetHost = null;
                _widgetCatalog = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Exception in GetWidgetDefinitionAsync:");
            }
        }

        return null;
    }
}
