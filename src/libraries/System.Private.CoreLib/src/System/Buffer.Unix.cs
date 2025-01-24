// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;

namespace System
{
    public static partial class Buffer
    {
        private static nuint MemmoveNativeThreshold
        {
            get
            {
                if (RuntimeInformation.ProcessArchitecture == ProcessorArchitecture.Arm64 || (RuntimeInformation.ProcessArchitecture == ProcessorArchitecture.LoongArch64)
                {
                    // Managed code is currently faster than glibc unoptimized memmove
                    // TODO-ARM64-UNIX-OPT revisit when glibc optimized memmove is in Linux distros
                    // https://github.com/dotnet/runtime/issues/8897
                    return nuint.MaxValue;
                }

                if (RuntimeInformation.ProcessArchitecture == ProcessorArchitecture.Arm)
                    return 512;

                return 2048;
            }
        }
    }
}
