// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading;

public partial class Thread
{
#if FEATURE_RUNTIME_SHUTDOWN

    [UnmanagedCallersOnly]
    private static void InvokeShutdown(IntPtr state)
    {
        GCHandle shutdownAction = GCHandle.FromIntPtr(state);
        try
        {
            ((Action?)shutdownAction.Target)?.Invoke();
        }
        finally
        {
            shutdownAction.Free();
        }
    }

    [MethodImpl(MethodImplOptions.InternalCall)]
    private static extern unsafe void RegisterShutdownHandler(delegate* unmanaged<IntPtr, void> onShutdown, IntPtr state);

    internal static unsafe void RegisterShutdownHandler(Action onShutdown)
    {
        var gcHandle = GCHandle.Alloc(onShutdown);
        RegisterShutdownHandler(&InvokeShutdown, GCHandle.ToIntPtr(gcHandle));
    }

#endif
}
