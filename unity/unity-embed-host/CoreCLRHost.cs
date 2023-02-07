using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly:DisableRuntimeMarshalling]

namespace Unity.CoreCLRHelpers;

using StringPtr = IntPtr;
static unsafe class CoreCLRHost
{
    static ALCWrapper alcWrapper;
    static FieldInfo assemblyHandleField;

    public static int InitMethod(HostStruct* functionStruct, int structSize)
    {
        if (functionStruct == null)
            return 1;

        if (Marshal.SizeOf<HostStruct>() != structSize)
            return 3;

        alcWrapper = new ALCWrapper();
        assemblyHandleField = typeof(Assembly).Assembly.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        if (assemblyHandleField == null)
            return 4;

        functionStruct->LoadFromMemory = &CallLoadFromAssemblyData;
        functionStruct->LoadFromPath = &CallLoadFromAssemblyPath;
        functionStruct->string_from_utf16 = &string_from_utf16;
        functionStruct->string_new_len = &string_new_len;
        functionStruct->string_new_utf16 = &string_new_utf16;
        functionStruct->string_new_wrapper = &string_new_wrapper;
        functionStruct->runtime_invoke = &runtime_invoke;

        return 0;
    }

    [UnmanagedCallersOnly]
    static IntPtr /*Assembly*/ CallLoadFromAssemblyData(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    [UnmanagedCallersOnly]
    static IntPtr /*Assembly*/ CallLoadFromAssemblyPath(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    delegate void* Invoker(void* method, void* obj, void** args);
    private static Dictionary<IntPtr, Invoker> invokeCache = new Dictionary<IntPtr, Invoker>();

    [UnmanagedCallersOnly]
    static void* runtime_invoke(IntPtr methodPointer, void* obj, void** args, void* exc)
    {
        var handle = RuntimeMethodHandle.FromIntPtr(methodPointer);
        if (!invokeCache.TryGetValue(methodPointer, out var func))
        {
            var method = (MethodInfo)MethodBase.GetMethodFromHandle(handle);
            var parameters = method.GetParameters();

            var signature = new Type[parameters.Length];
            for (var i = 0; i < parameters.Length; ++i)
            {
                signature[i] = ReduceType(parameters[i].ParameterType);
            }
            var returnType = ReduceType(method.ReturnType);

            var invokeArgs = new Type[] { typeof(void*), typeof(void*), typeof(void**)};

            var dynamicMethod = new DynamicMethod(
                "Invoke",
                typeof(void*),
                invokeArgs,
                typeof(CoreCLRHost).Module);

            var ilGen = dynamicMethod.GetILGenerator();
            ilGen.Emit(OpCodes.Ldarg_0);
            ilGen.EmitCalli(OpCodes.Calli, CallingConventions.Standard, returnType, signature, null);
            if (returnType == typeof(void))
                ilGen.Emit(OpCodes.Ldnull);
            ilGen.Emit(OpCodes.Ret);

            func = dynamicMethod.CreateDelegate<Invoker>();

            invokeCache.Add(methodPointer, func);
        }

        try
        {
            return func((void*)handle.GetFunctionPointer(), obj, args);
        }
        catch (Exception ex)
        {
            if (exc != null)
                Unsafe.AsRef<Exception>(exc) = ex;

            return null;
        }
    }

    static Type ReduceType(Type type)
    {
        return type;
    }

    [UnmanagedCallersOnly]
    static StringPtr string_from_utf16(ushort* text)
    {
        var s = new string((char*)text);
        return StringToPtr(s);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_new_len(void* domain /* unused */, sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_new_utf16(void* domain /* unused */, ushort* text, uint length)
    {
        var s = new string((char*)text, 0, (int)length);
        return StringToPtr(s);

    }

    [UnmanagedCallersOnly]
    static StringPtr string_new_wrapper(sbyte* text)
    {
        var s = new string(text);
        return StringToPtr(s);

    }

    static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
