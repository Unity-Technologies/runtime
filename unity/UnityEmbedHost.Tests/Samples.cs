// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace UnityEmbedHost.Tests;

class Mammal : Animal, IMammal
{
    public int EyeCount = 2;

    public int LegCount = 4;

    public void BreathAir()
    {
    }
}

class Cat : Mammal, ICat
{
    public static int NumberOfCats = 4;

    public int EarCount = 0;

    public static int StaticField = 0;

    public string Name = "Lion";

    public void Meow()
    {
    }
}

class CatOnlyInterface : ICat
{
}

class Rock : IRock
{
    public object RockField;
}

class Animal : IAnimal
{
}

class NoInterfaces
{
}

interface IAnimal
{
}

interface IMammal : IAnimal
{
}

interface ICat : IMammal
{
}

interface IRock
{
}

struct MyStruct
{

}

struct ValueMammal : IMammal
{
    public int LegCount;
    public int EyeCount;

    public ValueMammal()
    {
        EyeCount = 2;
        LegCount = 4;
    }
}

struct ValueCat : ICat
{
    public static int NumberOfCats = 4;
}

struct ValueRock : IRock
{
}

struct ValueAnimal : IAnimal
{
}

struct ValueNoInterfaces
{
}
