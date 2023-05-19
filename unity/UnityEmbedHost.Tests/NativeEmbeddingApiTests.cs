// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

#if !TESTING_UNITY_CORECLR
[Ignore("This suite can only be ran against unity coreclr")]
#endif
[TestFixture]
public class NativeEmbeddingApiTests : BaseEmbeddingApiTests
{
    internal override ICoreCLRHostWrapper ClrHost { get; } = new CoreCLRHostNativeWrappers();

    // [Test]
    // public unsafe override void FieldSetValue_Reference()
    // {
    //     Type type = typeof(Rock);
    //     var fieldInfo = type.GetField(nameof(Rock.RockField));
    //     var instance = Activator.CreateInstance(type);
    //     var value = new object();
    //     CoreCLRHostNative.mono_field_set_value(instance.ToNativeRepresentation(), fieldInfo!.FieldHandle.FieldHandleIntPtr(), Unsafe.AsPointer(ref value));
    //     ClrHost.field_set_value(instance, fieldInfo!.FieldHandle, value);
    //     Assert.AreEqual(value, fieldInfo.GetValue(instance));
    // }
}
