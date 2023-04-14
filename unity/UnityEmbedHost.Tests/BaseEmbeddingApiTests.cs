﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Runtime.InteropServices;
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

    [TestCase(typeof(Animal))]
    [TestCase(typeof(ValueAnimal))]
    public void GetClass(Type type)
    {
        Assert.That(ClrHost.object_get_class(Activator.CreateInstance(type)!), Is.EqualTo(type));
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

    static List<object?> FlattenedArray(Array arr)
    {
        var result = new List<object?>();
        foreach(var value in arr)
            result.Add(value);
        return result;
    }
}
