﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using NUnit.Framework;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Tests;

public abstract class BaseEmbeddingApiTests
{
    internal abstract ICoreCLRHostWrapper ClrHost { get; }

    [Test]
    public void MonoObjectGetClassReturnsClass()
    {
        var obj = ClrHost.object_new(typeof(Single));
        Assert.NotNull(obj);
        Assert.That(typeof(Single), Is.EqualTo(ClrHost.object_get_class(obj)));
    }

    // TODO : See if the string test can be fixed, otherwise remove.
    // const string kHelloString = "Hello";
    // private const string kHelloWorldString = "Hello, World!";
    // private const string kHelloWorldStringWithEmbeddedNull = "Hello\0World";
    // private const string kHelloWorldStringWithUnicode = "Hello, 団結!";

    // [Test]
    // public void FirstAttempt()
    // {
    //     // CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldString, 13), kHelloWorldString, 13);
    //     // CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldString, 5), kHelloString, 5);
    //     // CheckString(mono_string_new_len(mono_domain_get(), kHelloWorldStringWithEmbeddedNull, 11), kHelloWorldStringWithEmbeddedNull, 11);
    //     var byteArray = Encoding.Unicode.GetBytes(kHelloString);
    //     var utf8 = Encoding.Convert(Encoding.Unicode, Encoding.UTF8, byteArray);
    //     // var utf16 = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, utf8);
    //     // var finalStr = Encoding.Unicode.GetString(utf16);
    //     unsafe
    //     {
    //         var ptr = CoreCLRHost.string_new_len(null, (sbyte*)&utf8, (uint)utf8.Length);
    //         var target = Unsafe.As<IntPtr, string>(ref ptr);
    //         var utf16 = Encoding.Convert(Encoding.UTF8, Encoding.Unicode, Encoding.UTF8.GetBytes(target));
    //         var finalStr = Encoding.Unicode.GetString(utf16);
    //     }
    // }

    [Test]
    public unsafe void ExceptionFromClassWorks()
    {
        string msg = "An Exception Message";
        byte[] msg_bytes = Encoding.UTF8.GetBytes(msg);
        byte[] name_bytes = "Exception"u8.ToArray();
        byte[] namespace_bytes = "System"u8.ToArray();
        Exception ex;
        IntPtr assembly = ClrHost.class_get_image(typeof(Exception));

        fixed (byte* p = msg_bytes, n = name_bytes, ns = namespace_bytes)
        {
            ex = ClrHost.exception_from_name_msg(assembly, (sbyte*)ns, (sbyte*)n, (sbyte*)p);
        }
        Assert.That(ex.Message, Is.EqualTo(msg));
    }

    [Test]
    public unsafe void ExceptionFromClassWorksNullMessage()
    {
        byte[] name_bytes = "Exception"u8.ToArray();
        byte[] namespace_bytes = "System"u8.ToArray();
        Exception ex;
        IntPtr assembly = ClrHost.class_get_image(typeof(Exception));

        fixed (byte* n = name_bytes, ns = namespace_bytes)
        {
            ex = ClrHost.exception_from_name_msg(assembly, (sbyte*)ns, (sbyte*)n, null);
        }
        Assert.That(ex.Message, Is.EqualTo(string.Empty));
    }

#pragma warning disable CS0612
    [TestCase(typeof(Bacon), nameof(Bacon.Fry), typeof(ObsoleteAttribute), typeof(ObsoleteAttribute))]
    [TestCase(typeof(Bacon), nameof(Bacon.Smoke), typeof(ObsoleteAttribute), null)]
    [TestCase(typeof(Bacon), nameof(Bacon.Smoke), typeof(FooParentAttribute), typeof(FooAttribute))]
    [TestCase(typeof(Bacon), nameof(Bacon.Smoke), typeof(FooAttribute), typeof(FooAttribute))]
    [TestCase(typeof(Bacon), nameof(Bacon.Fry), typeof(FooAttribute), null)]
    [TestCase(typeof(Bacon), nameof(Bacon.Fry), typeof(FooParentAttribute), typeof(FooParentAttribute))]
    public void MethodGetAttributeWorks(Type type, string methodName, Type attributeType, Type? expectedAttribute)
    {
        var attribute = ClrHost.unity_method_get_attribute(type.GetMethod(methodName)!.MethodHandle, attributeType);
        Assert.That(attribute?.GetType(), Is.EqualTo(expectedAttribute));
    }

    [TestCase(typeof(Bacon), typeof(ObsoleteAttribute), typeof(ObsoleteAttribute))]
    [TestCase(typeof(Mammal), typeof(ObsoleteAttribute), null)]
    [TestCase(typeof(Bacon), typeof(FooParentAttribute), typeof(FooAttribute))]
    public void ClassGetAttributeWorks(Type type, Type attributeType, Type? expectedAttribute)
    {
        var attribute = ClrHost.unity_class_get_attribute(type, attributeType);
        Assert.That(attribute?.GetType(), Is.EqualTo(expectedAttribute));
    }

    [TestCase(nameof(Bacon.applewood), typeof(ObsoleteAttribute), typeof(ObsoleteAttribute))]
    [TestCase(nameof(Bacon.applewood), typeof(FooAttribute), null)]
    [TestCase(nameof(Bacon.applewood), typeof(FooParentAttribute), typeof(FooParentAttribute))]
    [TestCase(nameof(Bacon.hickory), typeof(ObsoleteAttribute), null)]
    [TestCase(nameof(Bacon.hickory), typeof(FooParentAttribute), typeof(FooAttribute))]
    [TestCase(nameof(Bacon.hickory), typeof(FooAttribute), typeof(FooAttribute))]
    public void FieldGetAttributeWorks(string fieldName, Type attributeType, Type? expectedAttribute)
    {
        FieldInfo? info = typeof(Bacon).GetField(fieldName);
        Assert.NotNull(info);
        var attribute = ClrHost.unity_field_get_attribute(typeof(Bacon), info!.FieldHandle, attributeType);
        Assert.That(attribute?.GetType(), Is.EqualTo(expectedAttribute));
    }
#pragma warning restore CS0612

    [Test]
    public unsafe void ValueBoxWorks()
    {
        int b = 16;
        var val = ClrHost.value_box(b.GetType(), (IntPtr)(&b));
        Assert.That(val.GetType(), Is.EqualTo(b.GetType()));
        Assert.That((int)val, Is.EqualTo(b));
    }

    [Test]
    public unsafe void GetExceptionArgumentNullWorks()
    {
        string msg = "An Exception Message";
        byte[] bytes_array = Encoding.UTF8.GetBytes(msg);
        Exception ex;
        fixed (byte* p = bytes_array)
        {
            ex = ClrHost.get_exception_argument_null((sbyte*)p);
        }
        Assert.That(ex.GetType(), Is.EqualTo(typeof(ArgumentNullException)));
        Assert.That(msg, Is.EqualTo(((ArgumentException)ex).ParamName));
    }

    [Test]
    public void GCHandleNewAndGetTarget()
    {
        var obj = new object();
        var handle1 = ClrHost.gchandle_new_v2(obj, false);
        Assert.That(handle1, Is.Not.EqualTo(0));
        var result = ClrHost.gchandle_get_target_v2(handle1);
        Assert.That(obj, Is.EqualTo(result));

        var obj2 = new object();
        var handle2 = ClrHost.gchandle_new_v2(obj2, true);
        Assert.That(handle2, Is.Not.EqualTo(0));
        var result2 = ClrHost.gchandle_get_target_v2(handle2);
        Assert.That(obj2, Is.EqualTo(result2));

        Assert.That(handle1, Is.Not.EqualTo(handle2));

        GCHandle.FromIntPtr(handle1).Free();
        GCHandle.FromIntPtr(handle2).Free();
    }

    // Classes and classes
    [TestCase(typeof(object), typeof(object), true)]
    [TestCase(typeof(Mammal), typeof(Mammal), true)]
    [TestCase(typeof(Mammal), typeof(Animal), true)]
    [TestCase(typeof(Cat), typeof(Animal), true)]
    [TestCase(typeof(Rock), typeof(Animal), false)]

    // Classes and interfaces
    [TestCase(typeof(Mammal), typeof(IMammal), true)]
    [TestCase(typeof(Mammal), typeof(IAnimal), true)]
    [TestCase(typeof(Cat), typeof(IAnimal), true)]
    [TestCase(typeof(CatOnlyInterface), typeof(IAnimal), true)]

    [TestCase(typeof(Rock), typeof(IAnimal), false)]
    [TestCase(typeof(NoInterfaces), typeof(IAnimal), false)]
    [TestCase(typeof(object), typeof(IRock), false)]

    // Structs and ValueType
    [TestCase(typeof(ValueMammal), typeof(ValueType), true)]

    // Structs and interfaces
    [TestCase(typeof(ValueMammal), typeof(IMammal), true)]
    [TestCase(typeof(ValueMammal), typeof(IAnimal), true)]
    [TestCase(typeof(ValueCat), typeof(IAnimal), true)]

    [TestCase(typeof(ValueRock), typeof(IAnimal), false)]
    [TestCase(typeof(ValueNoInterfaces), typeof(IAnimal), false)]
    public void IsInst(Type obj, Type type, bool shouldBeInstanceOfType)
    {
        var instance = Activator.CreateInstance(obj)!;

        CheckObjectIsInstance(obj, type, shouldBeInstanceOfType, instance);
    }

    [TestCase(typeof(Mammal), 1, typeof(Mammal), 1, true)]
    [TestCase(typeof(Mammal), 1, typeof(Animal), 1, true)]

    [TestCase(typeof(Mammal), 1, typeof(Mammal), 3, false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), 1, true)]
    [TestCase(typeof(ValueMammal), 1, typeof(IMammal), 1, false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), 3, false)]

    [TestCase(typeof(Mammal), 1, typeof(IMammal), 1, true)]
    [TestCase(typeof(Mammal), 1, typeof(IAnimal), 1, true)]
    [TestCase(typeof(Mammal), 1, typeof(IMammal), 2, false)]
    public void IsInstArrays(Type obj, int rank, Type checkType, int checkRank, bool shouldBeInstanceOfType)
    {
        var instance = Array.CreateInstance(obj, new int[rank]);
        Type arrayType = Array.CreateInstance(checkType, new int[checkRank]).GetType();
        CheckObjectIsInstance(instance.GetType(), arrayType, shouldBeInstanceOfType, instance);
    }

    [TestCase(typeof(Mammal), typeof(Mammal), 1, false)]
    [TestCase(typeof(Mammal), typeof(Animal), 1, false)]

    [TestCase(typeof(Mammal), typeof(Mammal), 2, false)]

    [TestCase(typeof(ValueMammal),typeof(ValueMammal), 1, false)]
    [TestCase(typeof(ValueMammal), typeof(IMammal), 1, false)]

    [TestCase(typeof(ValueMammal), typeof(ValueMammal), 2, false)]

    [TestCase(typeof(Mammal), typeof(IMammal), 1, false)]
    [TestCase(typeof(Mammal), typeof(IAnimal), 1, false)]
    [TestCase(typeof(Mammal), typeof(IMammal), 2, false)]
    public void IsInstNoneArrayToArrays(Type obj, Type checkType, int checkRank, bool shouldBeInstanceOfType)
    {
        var instance = Activator.CreateInstance(obj)!;

        CheckObjectIsInstance(instance.GetType(), Array.CreateInstance(checkType, new int[checkRank]).GetType(), shouldBeInstanceOfType, instance);
    }

    [TestCase(typeof(Mammal), 1, typeof(Mammal), false)]
    [TestCase(typeof(Mammal), 1, typeof(Animal), false)]

    [TestCase(typeof(Mammal), 1, typeof(Mammal), false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), false)]
    [TestCase(typeof(ValueMammal), 1, typeof(IMammal), false)]

    [TestCase(typeof(ValueMammal), 1, typeof(ValueMammal), false)]

    [TestCase(typeof(Mammal), 1, typeof(IMammal), false)]
    [TestCase(typeof(Mammal), 1, typeof(IAnimal), false)]
    [TestCase(typeof(Mammal), 1, typeof(IMammal),  false)]
    public void IsInstArrayToNoneArray(Type obj, int rank, Type checkType, bool shouldBeInstanceOfType)
    {
        var instance = Array.CreateInstance(obj, new int[rank]);

        CheckObjectIsInstance(instance.GetType(), checkType, shouldBeInstanceOfType, instance);
    }

    private void CheckObjectIsInstance(Type obj, Type type, bool shouldBeInstanceOfType, object instance)
    {
        var result = ClrHost.object_isinst(instance, type);

        if (shouldBeInstanceOfType)
        {
            if (result == null)
                Assert.Fail($"Expected {obj} to be of type {type}, but {nameof(CoreCLRHost.object_isinst)} claimed it wasn't");

            Assert.That(result, Is.EqualTo(instance));
        }
        else
        {
            if (result != null)
                Assert.Fail($"Expected {obj} to NOT be of type {type}, but {nameof(CoreCLRHost.object_isinst)} claimed it was");

            Assert.That(result, Is.Null);
        }
    }

    [TestCase(typeof(Mammal))]
    [TestCase(typeof(ValueAnimal))]
    public void GetClass(Type type)
    {
        Assert.That(ClrHost.object_get_class(Activator.CreateInstance(type)!), Is.EqualTo(type));
    }

    [TestCase(typeof(Mammal))]
    [TestCase(typeof(ValueAnimal))]
    public void ClassFromSystemTypeInstance(Type type)
    {
        Assert.That(ClrHost.class_from_systemtypeinstance(Activator.CreateInstance(type)!.GetType()), Is.EqualTo(type));
    }

    [TestCase(typeof(int), 0)]
    [TestCase(typeof(int), 3)]
    [TestCase(typeof(string), 10)]
    [TestCase(typeof(ValueAnimal), 5)]
    [TestCase(typeof(int[]), 2)]
    [TestCase(typeof(int), 1000000)]
    public void ArrayNew(Type arrayType, int length)
    {
        Assert.That(ClrHost.array_new(arrayType, length), Is.EquivalentTo(Array.CreateInstance(arrayType, length)));
    }

    [TestCase(typeof(int), 0, 0)]
    [TestCase(typeof(int), 3, 10)]
    [TestCase(typeof(string), 10, 5)]
    [TestCase(typeof(ValueAnimal), 5, 5)]
    [TestCase(typeof(int[]), 2, 1)]
    [TestCase(typeof(int), 1000, 1000)]
    public void ArrayNew2d(Type arrayType, int size0, int size1)
    {
        var result = ClrHost.unity_array_new_2d(arrayType, size0, size1);
        var expected = Array.CreateInstance(arrayType, size0, size1);
        AssertMultiDimensionalArraysAreEquivalent(expected, result);
    }

    [TestCase(typeof(int), 0, 0, 0)]
    [TestCase(typeof(int), 3, 10, 1)]
    [TestCase(typeof(string), 10, 5, 3)]
    [TestCase(typeof(ValueAnimal), 5, 5, 5)]
    [TestCase(typeof(int[]), 2, 1, 2)]
    [TestCase(typeof(int), 100, 100, 100)]
    public void ArrayNew3d(Type arrayType, int size0, int size1, int size2)
    {
        var result = ClrHost.unity_array_new_3d(arrayType, size0, size1, size2);
        var expected = Array.CreateInstance(arrayType, size0, size1, size2);
        AssertMultiDimensionalArraysAreEquivalent(expected, result);
    }

    [TestCase(typeof(int), 0)]
    [TestCase(typeof(string), 10)]
    [TestCase(typeof(ValueAnimal), 5)]
    [TestCase(typeof(int[]), 2)]
    [TestCase(typeof(int), 1000000)]
    public void ArrayLength(Type arrayType, int length)
    {
        var arr = Array.CreateInstance(arrayType, length);
        Assert.That(ClrHost.array_length(arr), Is.EqualTo(arr.Length));
    }

    [TestCase(typeof(int), 0, 0)]
    [TestCase(typeof(int), 3, 10)]
    [TestCase(typeof(ValueAnimal), 5, 5)]
    [TestCase(typeof(int), 1000, 1000)]
    public void ArrayLengthOf2d(Type arrayType, int size0, int size1)
    {
        var arr = Array.CreateInstance(arrayType, size0, size1);
        Assert.That(ClrHost.array_length(arr), Is.EqualTo(arr.Length));
    }

    [TestCase(typeof(int), 0, 0, 0)]
    [TestCase(typeof(int), 3, 10, 1)]
    [TestCase(typeof(ValueAnimal), 5, 5, 5)]
    [TestCase(typeof(int[]), 2, 1, 2)]
    [TestCase(typeof(int), 100, 100, 100)]
    public void ArrayLengthOf3d(Type arrayType, int size0, int size1, int size2)
    {
        var arr = Array.CreateInstance(arrayType, size0, size1, size2);
        Assert.That(ClrHost.array_length(arr), Is.EqualTo(arr.Length));
    }

    [TestCase(typeof(Cat))]
    [TestCase(typeof(ValueAnimal))]
    [TestCase(typeof(int))]
    [TestCase(typeof(int[]))]
    public void TypeGetObject(Type type)
    {
        var result = ClrHost.type_get_object(type);
        Assert.That(result, Is.EqualTo(type));
    }

    [TestCase(typeof(Cat), nameof(Mammal.BreathAir))]
    [TestCase(typeof(Cat), nameof(Cat.Meow))]
    public void MethodGetObject(Type type, string methodName)
    {
        var methodInfo = type.GetMethod(methodName);
        Assert.That(methodName, Is.Not.Null);
        var result = ClrHost.method_get_object(methodInfo!.MethodHandle, type);
        Assert.That(result, Is.EqualTo(methodInfo));
    }

    [TestCase(typeof(Mammal), nameof(Mammal.EyeCount))]
    [TestCase(typeof(Cat), nameof(Cat.EarCount))]
    [TestCase(typeof(Cat), nameof(Cat.StaticField))]
    public void FieldGetObject(Type type, string fieldName)
    {
        var fieldInfo = type.GetField(fieldName);
        Assert.That(fieldName, Is.Not.Null);
        var result = ClrHost.field_get_object(type, fieldInfo!.FieldHandle);
        Assert.That(result, Is.EqualTo(fieldInfo));
    }

    [TestCase(typeof(Mammal), nameof(Mammal))]
    [TestCase(typeof(Cat), nameof(Cat))]
    [TestCase(typeof(NoInterfaces), nameof(NoInterfaces))]
    [TestCase(typeof(IAnimal), nameof(IAnimal))]
    public unsafe void ClassFromNameReturnsClass(Type type, string typeName)
    {
        Assert.NotNull(type.Namespace);
        byte[] name_space = Encoding.UTF8.GetBytes(type.Namespace!);
        byte[] name = Encoding.UTF8.GetBytes(typeName);
        Type t;
        fixed (byte* ns = name_space, n = name)
        {
            t = ClrHost.class_from_name(ClrHost.class_get_image(type), (sbyte*)ns, (sbyte*)n, false);
        }
        Assert.That(type, Is.EqualTo(t));
    }

    [TestCase(typeof(object))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Cat))]
    [TestCase(typeof(Rock))]
    [TestCase(typeof(CatOnlyInterface))]
    [TestCase(typeof(ValueMammal))]
    [TestCase(typeof(ValueCat))]
    [TestCase(typeof(ValueRock))]
    [TestCase(typeof(ValueNoInterfaces))]
    public void ClassGet(Type type)
    {
        uint MONO_TOKEN_TYPE_DEF = 0x02000000;
        bool found = false;
        for (uint i = 0; i < Assembly.GetAssembly(type)!.GetTypes().Length; i++)
        {
            var t = ClrHost.unity_class_get(ClrHost.class_get_image(type), MONO_TOKEN_TYPE_DEF | (i + 1));
            Assert.NotNull(t);
            if (t!.Equals(type))
            {
                found = true;
                break;
            }
        }
        Assert.IsTrue(found);
    }

    [TestCase(typeof(object))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Cat))]
    [TestCase(typeof(Rock))]
    [TestCase(typeof(CatOnlyInterface))]
    public void MethodGet(Type type)
    {
        // Test only methods on the type itself to ensure they belong to the same module
        var typeMethods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        foreach (var m in typeMethods)
        {
            var token = m.MetadataToken;
            var expected = m.MethodHandle;
            var actual = ClrHost.get_method(ClrHost.class_get_image(type), (uint)token, null);
            Assert.NotNull(actual);
            Assert.AreEqual(expected, actual);
        }
    }

    [TestCase(typeof(object))]
    [TestCase(typeof(Bacon))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Cat))]
    [TestCase(typeof(Rock))]
    [TestCase(typeof(CatOnlyInterface))]
    public unsafe void FieldGet(Type type)
    {
        // Test only methods on the type itself to ensure they belong to the same module
        var typeFields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        foreach (var f in typeFields)
        {
            var token = f.MetadataToken;
            var expectedFieldHandle = f.FieldHandle;
            Type expectedType = f.FieldType;
            IntPtr actualTypeHandleIntPtr = IntPtr.Zero;
            var actual = ClrHost.unity_field_from_token_checked(ClrHost.class_get_image(type), (uint)token,  new IntPtr(&actualTypeHandleIntPtr));
            Assert.NotNull(actual);
            Assert.AreEqual(expectedFieldHandle, actual);
            Assert.AreEqual(expectedType, actualTypeHandleIntPtr.TypeFromHandleIntPtr());
        }
    }

    [TestCase(typeof(object))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Socket))]
    public void ClassGetImage(Type type)
    {
        Assembly? assembly = Assembly.GetAssembly(type);
        Assembly? foundAssembly = ClrHost.class_get_image(type).AssemblyFromGCHandleIntPtr();
        Assert.NotNull(assembly);
        Assert.NotNull(foundAssembly);
        Assert.That(assembly, Is.EqualTo(foundAssembly));
    }

    [TestCase(typeof(IntPtr))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Socket))]
    public unsafe void ImageLoaded(Type type)
    {
        byte[] nameBytes = Encoding.UTF8.GetBytes(type.Assembly.GetName().Name!);
        Assembly? assembly = Assembly.GetAssembly(type);
        Assembly? foundAssembly;
        fixed (byte* b = nameBytes)
        {
            foundAssembly = ClrHost.image_loaded((sbyte*)b).AssemblyFromGCHandleIntPtr();
        }
        Assert.NotNull(assembly);
        Assert.NotNull(foundAssembly);
        Assert.That(assembly, Is.EqualTo(foundAssembly));
    }

    [Test]
    public void GetCorlib()
    {
        Assert.That(typeof(Object).Assembly, Is.EqualTo(ClrHost.get_corlib().AssemblyFromGCHandleIntPtr()));
    }

    [TestCase(typeof(IntPtr))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Socket))]
    public void ImageGetName(Type type)
    {
        string? name = Marshal.PtrToStringAnsi(ClrHost.image_get_name(ClrHost.class_get_image(type)));
        Assert.NotNull(name);
        Assert.That(type.Assembly.GetName().Name, Is.EqualTo(name));
    }

    [TestCase(typeof(IntPtr))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Socket))]
    public void ImageGetFileName(Type type)
    {
        string? filename = Marshal.PtrToStringAnsi(ClrHost.image_get_filename(ClrHost.class_get_image(type)));
        Assert.NotNull(filename);
        Assert.That(type.Assembly.Location, Is.EqualTo(filename));
    }

    [TestCase(typeof(IntPtr))]
    [TestCase(typeof(Mammal))]
    [TestCase(typeof(Socket))]
    public void AssemblyGetObject(Type type)
    {
        var result = (Assembly)ClrHost.assembly_get_object(ClrHost.class_get_image(type));
        Assert.That(result.Location, Is.EqualTo(type.Assembly.Location));
    }

    [Test]
    public void AssemblyGetObjectFromLoad()
    {
        var location = GetType().Assembly.Location;
        unsafe
        {
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(location);
            fixed (byte* bytes = utf8Bytes)
            {
                var asm = CoreCLRHost.load_assembly_from_path(bytes, utf8Bytes.Length);
                var result = (Assembly)ClrHost.assembly_get_object(asm);
                Assert.That(result.Location, Is.EqualTo(GetType().Assembly.Location));
            }
        }
    }

    [Test]
    public void AssemblyGetObjectCoreLib()
    {
        var result = (Assembly)ClrHost.assembly_get_object(ClrHost.get_corlib());
        Assert.That(result.Location, Is.EqualTo(typeof(object).Assembly.Location));
    }

    // The exact same MethodInfo instance isn't returned but it's still the same method
    [TestCase(typeof(Cat), typeof(Mammal), nameof(Mammal.VirtualOnAnimalNotOverridden))]
    // Base Classes
    [TestCase(typeof(Mammal), typeof(Animal), nameof(Animal.AbstractOnAnimal))]
    [TestCase(typeof(Cat), typeof(Animal), nameof(Animal.AbstractOnAnimal))]
    [TestCase(typeof(Cat), typeof(Mammal), nameof(Mammal.AbstractOnAnimal))]
    [TestCase(typeof(ValueCat), typeof(object), nameof(object.ToString))]
    [TestCase(typeof(Cat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimal))]
    [TestCase(typeof(Mammal), typeof(IMammal), nameof(IMammal.InterfaceMethodOnIMammal))]
    // Interfaces
    [TestCase(typeof(Cat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimal))]
    [TestCase(typeof(ValueCat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimal))]
    [TestCase(typeof(ValueCat), typeof(IAnimal), nameof(IAnimal.ExplicitlyImplementedByMany))]
    [TestCase(typeof(Cat), typeof(IAnimal), nameof(IAnimal.ExplicitlyImplementedByMany))]
    [TestCase(typeof(Cat), typeof(IMammal), nameof(IMammal.ExplicitlyImplementedByMany))]
    [TestCase(typeof(Cat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimalWithParameters), new [] {typeof(int)})]
    [TestCase(typeof(Cat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimalWithParameters), new [] {typeof(string)})]
    [TestCase(typeof(Cat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimalWithParameters), new [] {typeof(int), typeof(int)})]
    public void ObjectGetVirtualMethodWhenObjectHasOverride(Type objType, Type baseType, string methodName, Type[]? parameters = null)
    {
        var obj = Activator.CreateInstance(objType);
        var result = ClrHost.object_get_virtual_method(obj, baseType.FindInstanceMethodByName(methodName, parameters).MethodHandle);
        var resultMethodInfo = MethodBase.GetMethodFromHandle(result)!;
        var expectedMethodInfo = objType.FindInstanceMethodByNameOrExplicitInterfaceName(baseType, methodName, parameters);
        Assert.That(resultMethodInfo.MetadataToken, Is.EqualTo(expectedMethodInfo.MetadataToken));
    }

    // Self
    [TestCase(typeof(Cat), typeof(Cat), nameof(Cat.NonVirtualMethodOnCat))]
    [TestCase(typeof(Cat), typeof(Cat), nameof(Cat.VirtualMethodOnCat))]
    // Base
    [TestCase(typeof(Cat), typeof(Animal), nameof(Animal.NonVirtualMethodOnAnimal))]
    public void ObjectGetVirtualMethodWhenBaseMethodIsReturned(Type objType, Type baseType, string methodName)
    {
        var obj = Activator.CreateInstance(objType);
        var baseMethodInfo = baseType.FindInstanceMethodByName(methodName, Array.Empty<Type>());
        var result = ClrHost.object_get_virtual_method(obj, baseMethodInfo.MethodHandle);
        var resultMethodInfo = MethodBase.GetMethodFromHandle(result)!;
        Assert.That(resultMethodInfo, Is.EqualTo(baseMethodInfo));
    }

    [TestCase(typeof(CatOnlyInterface), typeof(Animal), nameof(Animal.AbstractOnAnimal))]
    // Interfaces - Type does not implement interfaces
    [TestCase(typeof(CatOnlyInterface), typeof(IImposterAnimal), nameof(IImposterAnimal.InterfaceMethodOnIAnimal))]
    [TestCase(typeof(ImposterCat), typeof(IAnimal), nameof(IAnimal.InterfaceMethodOnIAnimalWithParameters), new [] {typeof(int)})]
    public void ObjectGetVirtualMethodNoMatch(Type objType, Type baseType, string methodName, Type[]? parameters = null)
    {
        var obj = Activator.CreateInstance(objType);
        var result = ClrHost.object_get_virtual_method_raw_return_only(obj, baseType.FindInstanceMethodByName(methodName, parameters).MethodHandle);
        Assert.That(result, Is.EqualTo(IntPtr.Zero));
    }

    /// <summary>
    /// NUnit's `Is.EquivalentTo` cannot handle multi-dimensional arrays.  It crashes on GetValue calls.
    /// </summary>
    /// <param name="expected"></param>
    /// <param name="actual"></param>
    static void AssertMultiDimensionalArraysAreEquivalent(Array expected, Array actual)
    {
        Assert.AreEqual(expected.Rank, actual.Rank);
        if (expected.Rank == 1)
        {
            Assert.That(actual, Is.EquivalentTo(expected));
            return;
        }

        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < expected.Rank; i++)
        {
            Assert.AreEqual(expected.GetLowerBound(i), actual.GetLowerBound(i));
            Assert.AreEqual(expected.GetUpperBound(i), actual.GetUpperBound(i));
        }

        // This seemed like the easiest way to compare all the values in the arrays
        var expectedFlat = FlattenedArray(expected);
        var actualFlat = FlattenedArray(actual);
        Assert.That(actualFlat, Is.EquivalentTo(expectedFlat));
    }

    [TestCase(typeof(Cat),                     typeof(Animal),                     true,   true)]
    [TestCase(typeof(Cat),                     typeof(Animal),                     false,  true)]
    [TestCase(typeof(Cat[]),                   typeof(Animal[]),                   false, false)]
    [TestCase(typeof(Cat[]),                   typeof(Animal[]),                   true,  false)]
    [TestCase(typeof(Cat[]),                   typeof(Animal),                     false, false)]
    [TestCase(typeof(Animal),                  typeof(IAnimal),                    false, false)]
    [TestCase(typeof(Animal),                  typeof(IAnimal),                    true,   true)]
    [TestCase(typeof(Animal[]),                typeof(IAnimal[]),                  true,  false)]
    [TestCase(typeof(IAnimal),                 typeof(Animal),                     true,  false)]
    [TestCase(typeof(IAnimal),                 typeof(Animal),                     false, false)]
    [TestCase(typeof(Animal),                  typeof(Cat),                        true,  false)]
    [TestCase(typeof(Animal),                  typeof(Cat),                        false, false)]
    [TestCase(typeof(GenericCat<, >),          typeof(GenericAnimal<, >),          true,  false)]
    [TestCase(typeof(GenericCat<int, string>), typeof(GenericAnimal<int, string>), false,  true)]
    [TestCase(typeof(GenericCat<int, string>), typeof(GenericAnimal<, >),          false, false)]
    [TestCase(typeof(GenericCat<int, string>), typeof(GenericAnimal<bool, int>),   false, false)]
    public void ClassIsSubclassOfReturnsProperValue(Type klass, Type parentClass, bool check_interfaces, bool expectedResult)
    {
        bool isSubclass = ClrHost.class_is_subclass_of(klass, parentClass, check_interfaces);
        Assert.That(isSubclass, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(Animal),                        false)]
    [TestCase(typeof(List<>),                         true)]
    [TestCase(typeof(GenericAnimal<, >),              true)]
    [TestCase(typeof(GenericAnimal<int, string>),    false)]
#pragma warning disable CS8500
    [TestCase(typeof(GenericAnimal<int, string>*),   false)]
#pragma warning restore CS8500
    [TestCase(typeof(GenericAnimal<int, string>[]),  false)]
    public void ClassIsGenericReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isGeneric = ClrHost.class_is_generic(klass);
        Assert.That(isGeneric, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(List<>),                       false)]
    public void ClassIsGenericByRefReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isGeneric = ClrHost.class_is_generic(klass.MakeByRefType());
        Assert.That(isGeneric, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(Animal),                        false)]
    [TestCase(typeof(List<>),                        false)]
    [TestCase(typeof(List<string>),                   true)]
    [TestCase(typeof(GenericAnimal<, >),             false)]
    [TestCase(typeof(GenericAnimal<int, string>),     true)]
#pragma warning disable CS8500
    [TestCase(typeof(GenericAnimal<int, string>*),   false)]
#pragma warning restore CS8500
    [TestCase(typeof(GenericAnimal<int, string>[]),  false)]
    public void ClassIsInflatedReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isInflated = ClrHost.class_is_inflated(klass);
        Assert.That(isInflated, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(Classification),                    true)]
    [TestCase(typeof(Classification*),                  false)]
    [TestCase(typeof(Classification[]),                 false)]
    [TestCase(typeof(Animal),                           false)]
    [TestCase(typeof(IAnimal),                          false)]
    [TestCase(typeof(GenericAnimal<int, string>),       false)]
    [TestCase(typeof(StructContainingOnlyAnEnum),       false)]
    [TestCase(typeof(StructContainingOnlyAnEnum.AnEnum), true)]
    public void ClassIsEnumReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isEnum = ClrHost.class_is_enum(klass);
        Assert.That(isEnum, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(Animal),                              false)]
    [TestCase(typeof(GenericAnimal<Classification, bool>), false)]
    [TestCase(typeof(ValueMammal*),                        false)]
    [TestCase(typeof(ValueMammal),                          true)]
    [TestCase(typeof(Classification),                       true)]
    public void ClassIsValuetypeReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isValueType = ClrHost.class_is_valuetype(klass);
        Assert.That(isValueType, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(ValueMammal), false)]
    public void ClassIsValuetypeByRefReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isValueType = ClrHost.class_is_valuetype(klass.MakeByRefType());
        Assert.That(isValueType, Is.EqualTo(expectedResult));
    }


    [TestCase(typeof(Animal),                                true)]
    [TestCase(typeof(GenericAnimal<Classification, bool>),  false)]
    [TestCase(typeof(ValueMammal*),                         false)]
    [TestCase(typeof(ValueMammal),                          false)]
    [TestCase(typeof(Classification),                       false)]
    public void UnityClassIsAbstractReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isAbstract = ClrHost.unity_class_is_abstract(klass);
        Assert.That(isAbstract, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(int),           true)]
    [TestCase(typeof(bool),          true)]
    [TestCase(typeof(char),          true)]
    [TestCase(typeof(string),       false)]
    [TestCase(typeof(MyStruct),      true)]
    [TestCase(typeof(Cat),          false)]
    [TestCase(typeof(RockLover),    false)]
    [TestCase(typeof(Classification),true)]
    [TestCase(typeof(IntPtr),        true)]
    [TestCase(typeof(int*),         false)]
    [TestCase(typeof(List<>),       false)]
    [TestCase(typeof(List<int>),    false)]
    public void ClassIsBlittableReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isBlittable = ClrHost.class_is_blittable(klass);
        Assert.That(isBlittable, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(Animal),                               false)]
    [TestCase(typeof(GenericAnimal<Classification, bool>),  false)]
    [TestCase(typeof(ValueMammal),                          false)]
    [TestCase(typeof(ValueMammal*),                         false)]
    [TestCase(typeof(IAnimal),                               true)]
    [TestCase(typeof(Classification),                       false)]
    public void UnityClassIsInterfaceReturnsProperValue(Type klass, bool expectedResult)
    {
        bool isInterface = ClrHost.unity_class_is_interface(klass);
        Assert.That(isInterface, Is.EqualTo(expectedResult));
    }

    [TestCase(typeof(RockLover), nameof(RockLover.GenericMethod), null,  true)]
    [TestCase(typeof(Cat),       nameof(Cat.Meow),                null, false)]
    [TestCase(typeof(Cat),       nameof(Cat.VirtualMethodOnCat),  null, false)]
    [TestCase(typeof(Cat),       nameof(Cat.AbstractOnAnimal),    null, false)]
    public void UnityMethodIsGenericReturnsProperValue(Type objType, string methodName, Type[]? parameters, bool expectedResult)
    {
        var baseMethodInfo = objType.FindInstanceMethodByName(methodName, parameters);

        bool isGeneric = ClrHost.unity_mono_method_is_generic(baseMethodInfo.MethodHandle);
        Assert.That(isGeneric, Is.EqualTo(expectedResult));
    }

    [Test]
    public void GcGetHeapSizeReturnsProperValue()
    {
        GC.Collect(0,GCCollectionMode.Forced, true);
        long heapSize = ClrHost.gc_get_heap_size();
        Assert.NotZero(heapSize);
        int dataSize = 1024 * 1024 * 100;
        int[] data = new int[dataSize];
        GC.Collect();
        heapSize = ClrHost.gc_get_heap_size();
        Assert.Greater(heapSize, dataSize * sizeof(int));
        GC.KeepAlive(data);
    }

    [Test]
    public void GcGetUsedSizeReturnsProperValue()
    {
        GC.Collect(0,GCCollectionMode.Forced, true);
        long usedSize = ClrHost.gc_get_used_size();
        Assert.NotZero(usedSize);
        int dataSize = 1024 * 1024 * 100;
        int[] data = new int[dataSize];
        GC.Collect();
        usedSize = ClrHost.gc_get_used_size();
        long heapSize = ClrHost.gc_get_heap_size();
        Assert.Greater(usedSize, dataSize * sizeof(int));
        Assert.GreaterOrEqual(heapSize, usedSize);
        GC.KeepAlive(data);
    }

    static List<object?> FlattenedArray(Array arr)
    {
        var result = new List<object?>();
        foreach(var value in arr)
            result.Add(value);
        return result;
    }
}
