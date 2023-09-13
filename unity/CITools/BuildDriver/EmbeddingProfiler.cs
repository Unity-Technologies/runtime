// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using NiceIO;

namespace BuildDriver;

public class EmbeddingProfiler
{
    public static void Build(GlobalConfig gConfig)
    {
        Console.WriteLine("***********************");
        Console.WriteLine("Unity: Building Embedding Profiler");
        Console.WriteLine("***********************");

        NPath workingDir = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ?
            Paths.UnityEmbedProfiler : Paths.UnityEmbedProfiler.CreateDirectory(gConfig.Configuration);

        string extraArchDefine = string.Empty;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && gConfig.Architecture.Equals("arm64"))
            extraArchDefine = "-DCMAKE_OSX_ARCHITECTURES=arm64 ";
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            extraArchDefine = Utils.WinArchitecture(gConfig.Architecture);

        ProcessStartInfo sInfo = new();
        sInfo.FileName = "cmake";
        sInfo.WorkingDirectory = workingDir;
        sInfo.Arguments = $". -A {extraArchDefine}";
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            sInfo.Arguments = $"{extraArchDefine}-DCMAKE_BUILD_TYPE={gConfig.Configuration} ..";

        Utils.RunProcess(sInfo, gConfig);

        sInfo.Arguments = "--build .";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            sInfo.Arguments = $"--build . --config {gConfig.Configuration}";

        Utils.RunProcess(sInfo, gConfig);
    }
}
