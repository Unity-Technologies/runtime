using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
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

        InitState();

        InitHostStruct(functionStruct);

        return 0;
    }

    internal static void InitState()
    {
        alcWrapper = new ALCWrapper();
        assemblyHandleField = typeof(Assembly).Assembly.GetType("System.Reflection.RuntimeAssembly").GetField("m_assembly", BindingFlags.Instance | BindingFlags.NonPublic);
        if (assemblyHandleField == null)
            throw new Exception("Failed to find RuntimeAssembly.m_assembly field.");
    }

    static partial void InitHostStruct(HostStruct* functionStruct);

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static IntPtr /*Assembly*/ load_assembly_from_data(byte* data, long size)
    {
        var assembly = alcWrapper.CallLoadFromAssemblyData(data, size);
        return (IntPtr)assemblyHandleField.GetValue(assembly);
    }

    [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
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
    public static StringPtr string_new_len(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] void* domain /* unused */,
        [NativeCallbackType("const char*")] sbyte* text, uint length)
    {
        var s = new string(text, 0, (int)length, Encoding.UTF8);
        return StringToPtr(s);

    }

    [return: NativeWrapperType("MonoString*")]
    [return: NativeCallbackType("ManagedStringPtr_t")]
    public static StringPtr string_new_utf16(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] void* domain /* unused */,
        ushort* text, [NativeCallbackType("gint32")] int length)
    {
        var s = new string((char*)text, 0, length);
        return StringToPtr(s);

    }

    [return: NativeCallbackType("uintptr_t")]
    public static IntPtr gchandle_new_v2([NativeCallbackType("MonoObject*")] IntPtr obj, bool pinned)
    {
        GCHandle handle = GCHandle.Alloc(obj.ToManagedRepresentation(), pinned ? GCHandleType.Pinned : GCHandleType.Normal);
        return GCHandle.ToIntPtr(handle);
    }

    [return: NativeCallbackType("uintptr_t")]
    public static IntPtr gchandle_new_weakref_v2([NativeCallbackType("MonoObject*")] IntPtr obj, bool track_resurrection)
    {
        GCHandle handle = GCHandle.Alloc(obj.ToManagedRepresentation(), track_resurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
        return GCHandle.ToIntPtr(handle);
    }

    [return: NativeWrapperType("MonoObject*")]
    public static IntPtr gchandle_get_target_v2([NativeWrapperType("uintptr_t")] IntPtr handleIn)
        => handleIn.ToGCHandle().Target.ToNativeRepresentation();

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr object_isinst([NativeCallbackType("MonoObject*")] IntPtr obj, [NativeCallbackType("MonoClass*")] IntPtr klass)
        => obj.ToManagedRepresentation().GetType().IsAssignableTo(klass.TypeFromHandleIntPtr()) ? obj : nint.Zero;

    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr object_get_class([NativeCallbackType("MonoObject*")] IntPtr obj)
        => obj.ToManagedRepresentation().TypeHandleIntPtr();

    [NativeFunction("coreclr_class_from_systemtypeinstance")]
    [return: NativeCallbackType("MonoClass*")]
    public static IntPtr class_from_systemtypeinstance(
        [ManagedWrapperOptions(ManagedWrapperOptions.Custom, nameof(Type))]
        [NativeCallbackType("MonoObject*")]
        IntPtr systemTypeInstance)
        => ((Type)systemTypeInstance.ToManagedRepresentation()).TypeHandle.Value;

    [return: NativeCallbackType("MonoArray*")]
    public static IntPtr array_new(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass, [NativeCallbackType("guint32")] int n)
        => Array.CreateInstance(klass.TypeFromHandleIntPtr(), n).ToNativeRepresentation();

    [return: NativeCallbackType("MonoArray*")]
    public static IntPtr unity_array_new_2d(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("size_t")] int size0, [NativeCallbackType("size_t")] int size1)
        => Array.CreateInstance(klass.TypeFromHandleIntPtr(), size0, size1).ToNativeRepresentation();

    [return: NativeCallbackType("MonoArray*")]
    public static IntPtr unity_array_new_3d(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("size_t")] int size0, [NativeCallbackType("size_t")] int size1, [NativeCallbackType("size_t")] int size2)
        => Array.CreateInstance(klass.TypeFromHandleIntPtr(), size0, size1, size2).ToNativeRepresentation();

    [NativeFunction("coreclr_array_length")]
    [return: NativeCallbackType("int")]
    public static int array_length([NativeCallbackType("MonoArray*")] IntPtr array)
        => ((Array)array.ToManagedRepresentation()).Length;

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr value_box(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("gpointer")] IntPtr val) =>
        Marshal.PtrToStructure(val, klass.TypeFromHandleIntPtr()).ToNativeRepresentation();

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr object_new(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoDomain*")] IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass)
        => FormatterServices.GetUninitializedObject(klass.TypeFromHandleIntPtr()).ToNativeRepresentation();

    [return: NativeCallbackType("MonoObject*")]
    public static IntPtr type_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoType*")] IntPtr type)
        => type.TypeFromHandleIntPtr().ToNativeRepresentation();

    [return: NativeCallbackType("MonoReflectionMethod*")]
    public static IntPtr method_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoMethod*")] IntPtr method,
        [NativeCallbackType("MonoClass*")] IntPtr refclass) =>
        MethodInfo.GetMethodFromHandle(method.MethodHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(refclass)).ToNativeRepresentation();

    [return: NativeCallbackType("MonoReflectionField*")]
    public static IntPtr field_get_object(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)] [NativeCallbackType("MonoDomain*")]
        IntPtr domain,
        [NativeCallbackType("MonoClass*")] IntPtr klass,
        [NativeCallbackType("MonoClassField*")]
        IntPtr field) =>
        FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr(), RuntimeTypeHandle.FromIntPtr(klass)).ToNativeRepresentation();

    // [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static void field_get_value(
        [NativeCallbackType("MonoObject*")] IntPtr obj,
        [NativeCallbackType("MonoClassField*")] IntPtr field,
        [NativeCallbackType("void*")] void* value)
    {
        var fieldInfo = FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr()/*,obj.ToManagedRepresentation().GetType().TypeHandle*/);
        var managedObject = obj.ToManagedRepresentation();
        var tmp = fieldInfo.GetValue(managedObject);
        Unsafe.Write(value, tmp);
        // if(fieldInfo.FieldType.IsValueType)
        //     Unsafe.Write(value, Unsafe.Unbox<int>(tmp!));
        // // if(tmp is int i)
        // //     Unsafe.Write(value, i);
        // // else
        //     Unsafe.Write(value, tmp);
    }

    // [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static void field_static_get_value(
        [ManagedWrapperOptions(ManagedWrapperOptions.Exclude)]
        [NativeCallbackType("MonoVTable*")] IntPtr obj,
        [NativeCallbackType("MonoClassField*")] IntPtr field,
        [NativeCallbackType("void*")]
        // [ManagedWrapperOptions(ManagedWrapperOptions.Custom, "ref object")]
        void* value)
    {
        var fieldInfo = FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr());
        var tmp = fieldInfo.GetValue(null);
        Unsafe.Write(value, tmp);
    }

    // [NativeFunction(NativeFunctionOptions.DoNotGenerate)]
    public static void field_set_value(
        [NativeCallbackType("MonoObject*")] IntPtr obj,
        [NativeCallbackType("MonoClassField*")] IntPtr field,
        [NativeCallbackType("void*")]
        [ManagedWrapperOptions(ManagedWrapperOptions.Custom, "object")]
        void* value)
    {
        var fieldInfo = FieldInfo.GetFieldFromHandle(field.FieldHandleFromHandleIntPtr());
        var managedObject = obj.ToManagedRepresentation();
        // Unsafe.Read<>()
        // object tmp = Unsafe.As<object>(Unsafe.AsRef<object>(value));
        // object tmp = Unsafe.AsRef<object>(value);
        object tmp = Unsafe.Read<object>(value);
        fieldInfo.SetValue(managedObject, tmp);
    }

    static StringPtr StringToPtr(string s)
    {
        // Return raw object pointer for now with the NullGC.
        // This will become a GCHandle in the future.
        return Unsafe.As<string, StringPtr>(ref s);
    }
}
