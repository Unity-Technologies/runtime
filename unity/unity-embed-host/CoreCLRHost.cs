using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

[assembly:DisableRuntimeMarshalling]

namespace Unity.CoreCLRHelpers;

using StringPtr = IntPtr;
static unsafe partial class CoreCLRHost
{
    static ALCWrapper alcWrapper;
    static FieldInfo assemblyHandleField;

    internal static int InitMethod(HostStruct* functionStruct, int structSize)
    {
        if (Marshal.SizeOf<HostStruct>() != structSize)
            throw new Exception("Invalid struct size");

        alcWrapper = new ALCWrapper();
        assemblyHandleField = typeof(Assembly).Assembly.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        if (assemblyHandleField == null)
            throw new Exception("Failed to find RuntimeAssembly.m_assembly field.");

        InitHostStruct(functionStruct);

        return 0;
    }

    static partial void InitHostStruct(HostStruct* functionStruct);

    [NoNativeWrapper]
    public static IntPtr /*Assembly*/ load_assembly_from_data(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    [NoNativeWrapper]
    public static IntPtr /*Assembly*/ load_assembly_from_path(byte* path, int length)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyPath(Encoding.UTF8.GetString(path, length));
        return (IntPtr)assemblyHandleField.GetValue(assembly);

    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_from_utf16([NativeCallbackType("const gunichar2*")] ushort* text)
    {
        var s = new string((char*)text);
        return StringToPtr(s);

    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_new_len([NativeCallbackType("MonoDomain*")] void* domain /* unused */, [NativeCallbackType("const char*")] sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);

    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_new_utf16([NativeCallbackType("MonoDomain*")] void* domain /* unused */, ushort* text, [NativeCallbackType("gint32")] int length)
    {
        var s = new string((char*)text, 0, length);
        return StringToPtr(s);

    }

    [return: NativeCallbackType("uintptr_t")]
    public static IntPtr gchandle_new_v2([NativeCallbackType("MonoObject*")] IntPtr obj, bool pinned)
    {
        GCHandle handle = GCHandle.Alloc(Unsafe.As<IntPtr, Object>(ref obj), pinned ? GCHandleType.Pinned : GCHandleType.Normal);
        return GCHandle.ToIntPtr(handle);
    }

    [return: NativeCallbackType("uintptr_t")]
    public static IntPtr gchandle_new_weakref_v2([NativeCallbackType("MonoObject*")] IntPtr obj, bool track_resurrection)
    {
        GCHandle handle = GCHandle.Alloc(Unsafe.As<IntPtr, Object>(ref obj), track_resurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
        return GCHandle.ToIntPtr(handle);
    }

    [return: NativeWrapperType("MonoObject*")]
    public static IntPtr gchandle_get_target_v2([NativeWrapperType("uintptr_t")] IntPtr handleIn)
    {
        GCHandle handle = GCHandle.FromIntPtr(handleIn);
        object obj = handle.Target;
        return Unsafe.As<object, IntPtr>(ref obj);
    }

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr object_isinst([NativeCallbackType("MonoObject*")] IntPtr obj, [NativeCallbackType("MonoClass*")] IntPtr klass)
    {
        var instance = Unsafe.As<IntPtr, object>(ref obj);
        var type = Unsafe.As<IntPtr, Type>(ref klass);
        Type current = instance.GetType();

        while (current != null)
        {
            if (current == type)
                return obj;

            // TODO : Unclear what the value of this is.  The if above is always hit first preventing rank differences from being taken into consideration
            // if (current.IsArray && type.IsArray && current.GetArrayRank() == type.GetArrayRank() && current.GetElementType() == type.GetElementType())
            //     return obj;

            foreach (var iface in current.GetInterfaces())
            {
                if (iface == type)
                    return obj;
            }

            current = current.BaseType;
        }

        return nint.Zero;
    }

    static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
