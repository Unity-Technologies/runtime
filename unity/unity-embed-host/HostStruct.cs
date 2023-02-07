using System;

namespace Unity.CoreCLRHelpers;

using StringPtr = IntPtr;

unsafe struct HostStruct
{
    public delegate* unmanaged<byte*, long, IntPtr> LoadFromMemory;
    public delegate* unmanaged<byte*, int, IntPtr> LoadFromPath;

    public delegate* unmanaged<ushort*, StringPtr> string_from_utf16;
    public delegate* unmanaged<void* /*domain*/, sbyte* /*text*/, uint /*length*/, StringPtr> string_new_len;
    public delegate* unmanaged<void* /*domain*/, ushort* /*text*/, uint /*length*/, StringPtr> string_new_utf16;
    public delegate* unmanaged<sbyte* /*text*/, StringPtr> string_new_wrapper;
    public delegate* unmanaged<IntPtr, void*, void**, void**, void*> runtime_invoke;
}
