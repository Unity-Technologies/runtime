// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;


namespace System
{
    public static partial class Buffer
    {
        private static nuint MemmoveNativeThreshold
        {
            get
            {
                if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
                {
                    // Determine optimal value for Windows.
                    // https://github.com/dotnet/runtime/issues/8896
                    return nuint.MaxValue;
                }
                return 2048;
            }
        }
    }
}
