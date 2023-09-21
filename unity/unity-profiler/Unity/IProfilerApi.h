#pragma once

namespace ScriptingCoreCLR
{
    struct IProfilerApi
    {
        virtual void Test() = 0;
        virtual void RunLeakDetection() = 0;
    };
}
