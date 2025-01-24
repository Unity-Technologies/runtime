// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using static System.Array;

#pragma warning disable SA1121 // We use our own aliases since they differ per platform
namespace System.Runtime.InteropServices
{
    /// <summary>Defines an immutable value type that represents a floating type that has the same size as the native integer size.</summary>
    /// <remarks>It is meant to be used as an exchange type at the managed/unmanaged boundary to accurately represent in managed code unmanaged APIs that use a type alias for C or C++'s <c>float</c> on 32-bit platforms or <c>double</c> on 64-bit platforms, such as the CGFloat type in libraries provided by Apple.</remarks>
    [Intrinsic]
    [NonVersionable] // This only applies to field layout
    public readonly struct NFloat
        : IBinaryFloatingPointIeee754<NFloat>,
          IMinMaxValue<NFloat>,
          IUtf8SpanFormattable
    {
        private const NumberStyles DefaultNumberStyles = NumberStyles.Float | NumberStyles.AllowThousands;

        private struct NativeValueContiner
        {
            private UIntPtr _value;

            public static unsafe implicit operator double(NativeValueContiner value)
            {
                Debug.Assert(NativeTypeIsDouble);
                double d;
                Unsafe.Copy(&d, in value._value);
                return d;
            }

            public static unsafe implicit operator float(NativeValueContiner value)
            {
                Debug.Assert(NativeTypeIsDouble);
                float f;
                Unsafe.Copy(&f, in value._value);
                return f;
            }
        }

        private readonly NativeValueContiner _value;

        private static bool NativeTypeIsDouble
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return IntPtr.Zero == sizeof(double);
            }
        }

        private static int SizeOfExponent
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return NativeTypeIsDouble ? sizeof(Int16) : sizeof(SByte);
            }
        }

        private static int SizeOfSignitcand
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return NativeTypeIsDouble ? sizeof(UInt64) : sizeof(UInt64);
            }
        }

        /// <summary>Constructs an instance from a 32-bit floating point value.</summary>
        /// <param name="value">The floating-point value.</param>
        [NonVersionable]
        public unsafe NFloat(float value)
        {
            if (NativeTypeIsDouble)
            {
                double d = value;
                Unsafe.Copy(ref _value, &d);
            }
            else
            {
                Unsafe.Copy(ref _value, &value);
            }
        }

        /// <summary>Constructs an instance from a 64-bit floating point value.</summary>
        /// <param name="value">The floating-point value.</param>
        [NonVersionable]
        public unsafe NFloat(double value)
        {
            if (NativeTypeIsDouble)
            {
                Unsafe.Copy(ref _value, &value);
            }
            else
            {
                float f = (float)value;
                Unsafe.Copy(ref _value, &f);
            }
        }

        /// <summary>Represents the smallest positive NFloat value that is greater than zero.</summary>
        public static NFloat Epsilon
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? new NFloat(double.Epsilon) : new NFloat(float.Epsilon);
        }

        /// <summary>Represents the largest finite value of a NFloat.</summary>
        public static NFloat MaxValue
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? new NFloat(double.MaxValue) : new NFloat(float.MaxValue);
        }

        /// <summary>Represents the smallest finite value of a NFloat.</summary>
        public static NFloat MinValue
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? new NFloat(double.MinValue) : new NFloat(float.MinValue);
        }

        /// <summary>Represents a value that is not a number (NaN).</summary>
        public static NFloat NaN
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? new NFloat(double.NaN) : new NFloat(float.NaN);
        }

        /// <summary>Represents negative infinity.</summary>
        public static NFloat NegativeInfinity
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? new NFloat(double.NegativeInfinity) : new NFloat(float.NegativeInfinity);
        }

        /// <summary>Represents positive infinity.</summary>
        public static NFloat PositiveInfinity
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? new NFloat(double.PositiveInfinity) : new NFloat(float.PositiveInfinity);
        }

        /// <summary>Gets the size, in bytes, of an NFloat.</summary>
        public static int Size
        {
            [NonVersionable]
            get => UIntPtr.Size;
        }

        /// <summary>The underlying floating-point value of this instance.</summary>
        public double Value
        {
            [NonVersionable]
            get => NativeTypeIsDouble ? _value : _value;
        }

        //
        // Unary Arithmetic
        //

        /// <summary>Computes the unary plus of a value.</summary>
        /// <param name="value">The value for which to compute its unary plus.</param>
        /// <returns>The unary plus of <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator +(NFloat value) => value;

        /// <summary>Computes the unary negation of a value.</summary>
        /// <param name="value">The value for which to compute its unary negation.</param>
        /// <returns>The unary negation of <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator -(NFloat value) => NativeTypeIsDouble ? new NFloat(value._value) : new NFloat(-value._value);

        /// <summary>Increments a value.</summary>
        /// <param name="value">The value to increment.</param>
        /// <returns>The result of incrementing <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator ++(NFloat value)
        {
            if (NativeTypeIsDouble)
            {
                double tmp = value._value;
                ++tmp;
                return new NFloat(tmp);
            }
            else
            {
                float tmp = value._value;
                ++tmp;
                return new NFloat(tmp);
            }
        }

        /// <summary>Decrements a value.</summary>
        /// <param name="value">The value to decrement.</param>
        /// <returns>The result of decrementing <paramref name="value" />.</returns>
        [NonVersionable]
        public static NFloat operator --(NFloat value)
        {
            if (NativeTypeIsDouble)
            {
                double tmp = value._value;
                --tmp;
                return new NFloat(tmp);
            }
            else
            {
                float tmp = value._value;
                --tmp;
                return new NFloat(tmp);
            }
        }

        //
        // Binary Arithmetic
        //

        /// <summary>Adds two values together to compute their sum.</summary>
        /// <param name="left">The value to which <paramref name="right" /> is added.</param>
        /// <param name="right">The value which is added to <paramref name="left" />.</param>
        /// <returns>The sum of <paramref name="left" /> and <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator +(NFloat left, NFloat right) => new NFloat(NativeTypeIsDouble ? left._value + right._value : left._value + right._value);

        /// <summary>Subtracts two values to compute their difference.</summary>
        /// <param name="left">The value from which <paramref name="right" /> is subtracted.</param>
        /// <param name="right">The value which is subtracted from <paramref name="left" />.</param>
        /// <returns>The difference of <paramref name="right" /> subtracted from <paramref name="left" />.</returns>
        [NonVersionable]
        public static NFloat operator -(NFloat left, NFloat right) => new NFloat(NativeTypeIsDouble ? left._value - right._value : left._value - right._value);

        /// <summary>Multiplies two values together to compute their product.</summary>
        /// <param name="left">The value which <paramref name="right" /> multiplies.</param>
        /// <param name="right">The value which multiplies <paramref name="left" />.</param>
        /// <returns>The product of <paramref name="left" /> multiplied-by <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator *(NFloat left, NFloat right) => new NFloat(NativeTypeIsDouble ? left._value * right._value : left._value * right._value);

        /// <summary>Divides two values together to compute their quotient.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The quotient of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator /(NFloat left, NFloat right) => new NFloat(NativeTypeIsDouble ? left._value / right._value : left._value / right._value);

        /// <summary>Divides two values together to compute their remainder.</summary>
        /// <param name="left">The value which <paramref name="right" /> divides.</param>
        /// <param name="right">The value which divides <paramref name="left" />.</param>
        /// <returns>The remainder of <paramref name="left" /> divided-by <paramref name="right" />.</returns>
        [NonVersionable]
        public static NFloat operator %(NFloat left, NFloat right) => new NFloat(NativeTypeIsDouble ? left._value % right._value : left._value % right._value);

        //
        // Comparisons
        //

        /// <summary>Compares two values to determine equality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator ==(NFloat left, NFloat right) => NativeTypeIsDouble ? left._value == right._value : left._value == right._value;

        /// <summary>Compares two values to determine inequality.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is not equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator !=(NFloat left, NFloat right) => NativeTypeIsDouble ? left._value != right._value : left._value != right._value;

        /// <summary>Compares two values to determine which is less.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator <(NFloat left, NFloat right) => NativeTypeIsDouble ? left._value < right._value : left._value < right._value;

        /// <summary>Compares two values to determine which is less or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is less than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator <=(NFloat left, NFloat right) => NativeTypeIsDouble ? left._value <= right._value : left._value <= right._value;

        /// <summary>Compares two values to determine which is greater.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator >(NFloat left, NFloat right) => NativeTypeIsDouble ? left._value > right._value : left._value > right._value;

        /// <summary>Compares two values to determine which is greater or equal.</summary>
        /// <param name="left">The value to compare with <paramref name="right" />.</param>
        /// <param name="right">The value to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if <paramref name="left" /> is greater than or equal to <paramref name="right" />; otherwise, <c>false</c>.</returns>
        [NonVersionable]
        public static bool operator >=(NFloat left, NFloat right) => NativeTypeIsDouble ? left._value >= right._value : left._value >= right._value;

        //
        // Explicit Convert To NFloat
        //

        /// <summary>Explicitly converts a <see cref="decimal" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(decimal value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Explicitly converts a <see cref="double" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(double value) => new NFloat(value);

        //
        // Explicit Convert From NFloat
        //

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="byte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        [NonVersionable]
        public static explicit operator byte(NFloat value) => NativeTypeIsDouble ? (byte)(double)value._value : (byte)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="byte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="byte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="byte" />.</exception>
        [NonVersionable]
        public static explicit operator checked byte(NFloat value) => checked(NativeTypeIsDouble ? (byte)(double)value._value : (byte)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="char" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        [NonVersionable]
        public static explicit operator char(NFloat value) => NativeTypeIsDouble ? (char)(double)value._value : (char)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="char" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="char" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="char" />.</exception>
        [NonVersionable]
        public static explicit operator checked char(NFloat value) => checked(NativeTypeIsDouble ? (char)(double)value._value : (char)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="decimal" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="decimal" /> value.</returns>
        [NonVersionable]
        public static explicit operator decimal(NFloat value) => NativeTypeIsDouble ? (decimal)(double)value._value : (decimal)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="Half" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Half" /> value.</returns>
        [NonVersionable]
        public static explicit operator Half(NFloat value) => NativeTypeIsDouble ? (Half)(double)value._value : (Half)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="short" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        [NonVersionable]
        public static explicit operator short(NFloat value) => NativeTypeIsDouble ? (short)(double)value._value : (short)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="short" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="short" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="short" />.</exception>
        [NonVersionable]
        public static explicit operator checked short(NFloat value) => checked(NativeTypeIsDouble ? (short)(double)value._value : (short)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="int" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        [NonVersionable]
        public static explicit operator int(NFloat value) => NativeTypeIsDouble ? (int)(double)value._value : (int)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="int" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="int" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="int" />.</exception>
        [NonVersionable]
        public static explicit operator checked int(NFloat value) => checked(NativeTypeIsDouble ? (int)(double)value._value : (int)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="long" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        [NonVersionable]
        public static explicit operator long(NFloat value) => NativeTypeIsDouble ? (long)(double)value._value : (long)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="long" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="long" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="long" />.</exception>
        [NonVersionable]
        public static explicit operator checked long(NFloat value) => checked(NativeTypeIsDouble ? (long)(double)value._value : (long)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="Int128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Int128" /> value.</returns>
        [NonVersionable]
        public static explicit operator Int128(NFloat value) => NativeTypeIsDouble ? (Int128)(double)value._value : (Int128)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="Int128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="Int128" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="Int128" />.</exception>
        [NonVersionable]
        public static explicit operator checked Int128(NFloat value) => checked(NativeTypeIsDouble ? (Int128)(double)value._value : (Int128)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="IntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        [NonVersionable]
        public static explicit operator nint(NFloat value) => NativeTypeIsDouble ? (nint)(double)value._value : (nint)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="IntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="IntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="IntPtr" />.</exception>
        [NonVersionable]
        public static explicit operator checked nint(NFloat value) => checked(NativeTypeIsDouble ? (nint)(double)value._value : (nint)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="sbyte" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator sbyte(NFloat value) => NativeTypeIsDouble ? (sbyte)(double)value._value : (sbyte)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="sbyte" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="sbyte" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="sbyte" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked sbyte(NFloat value) => checked(NativeTypeIsDouble ? (sbyte)(double)value._value : (sbyte)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="float" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="float" /> value.</returns>
        [NonVersionable]
        public static explicit operator float(NFloat value) => NativeTypeIsDouble ? (float)(double)value._value : (float)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ushort" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator ushort(NFloat value) => NativeTypeIsDouble ? (ushort)(double)value._value : (ushort)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ushort" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ushort" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ushort" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked ushort(NFloat value) => checked(NativeTypeIsDouble ? (ushort)(double)value._value : (ushort)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="uint" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator uint(NFloat value) => NativeTypeIsDouble ? (uint)(double)value._value : (uint)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="uint" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="uint" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="uint" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked uint(NFloat value) => checked(NativeTypeIsDouble ? (uint)(double)value._value : (uint)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ulong" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator ulong(NFloat value) => NativeTypeIsDouble ? (ulong)(double)value._value : (ulong)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="ulong" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="ulong" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="ulong" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked ulong(NFloat value) => checked(NativeTypeIsDouble ? (ulong)(double)value._value : (ulong)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UInt128" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UInt128" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator UInt128(NFloat value) => NativeTypeIsDouble ? (UInt128)(double)value._value : (UInt128)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UInt128" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UInt128" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UInt128" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked UInt128(NFloat value) => checked(NativeTypeIsDouble ? (UInt128)(double)value._value : (UInt128)(float)value._value);

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UIntPtr" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator nuint(NFloat value) => NativeTypeIsDouble ? (nuint)(double)value._value : (nuint)(float)value._value;

        /// <summary>Explicitly converts a native-sized floating-point value to its nearest representable <see cref="UIntPtr" /> value, throwing an overflow exception for any values that fall outside the representable range.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="UIntPtr" /> value.</returns>
        /// <exception cref="OverflowException"><paramref name="value" /> is not representable by <see cref="UIntPtr" />.</exception>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator checked nuint(NFloat value) => checked(NativeTypeIsDouble ? (nuint)(double)value._value : (nuint)(float)value._value);

        //
        // Implicit Convert To NFloat
        //

        /// <summary>Implicitly converts a <see cref="byte" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(byte value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="char" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(char value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="Half" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(Half value) => (NFloat)(float)value;

        /// <summary>Implicitly converts a <see cref="short" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(short value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="int" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(int value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="long" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(long value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Explicitly converts a <see cref="Int128" /> to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static explicit operator NFloat(Int128 value)
        {
            if (Int128.IsNegative(value))
            {
                value = -value;
                return -(NFloat)(UInt128)(value);
            }
            return (NFloat)(UInt128)(value);
        }

        /// <summary>Implicitly converts a <see cref="System.IntPtr" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(nint value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="sbyte" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(sbyte value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="float" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        public static implicit operator NFloat(float value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="ushort" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(ushort value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="uint" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(uint value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Implicitly converts a <see cref="ulong" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(ulong value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        /// <summary>Explicitly converts <see cref="UInt128"/> to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static explicit operator NFloat(UInt128 value) => (NFloat)(double)(value);

        /// <summary>Implicitly converts a <see cref="System.UIntPtr" /> value to its nearest representable native-sized floating-point value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable native-sized floating-point value.</returns>
        [NonVersionable]
        [CLSCompliant(false)]
        public static implicit operator NFloat(nuint value) => NativeTypeIsDouble ? new NFloat((double)value) : new NFloat((float)value);

        //
        // Implicit Convert From NFloat
        //

        /// <summary>Implicitly converts a native-sized floating-point value to its nearest representable <see cref="double" /> value.</summary>
        /// <param name="value">The value to convert.</param>
        /// <returns><paramref name="value" /> converted to its nearest representable <see cref="double" /> value.</returns>
        public static implicit operator double(NFloat value) => NativeTypeIsDouble ? (double)(value._value) : (double)(value._value);

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is finite (zero, subnormal or normal); <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsFinite(NFloat value) => NativeTypeIsDouble ? double.IsFinite(value._value) : float.IsFinite(value._value);

        /// <summary>Determines whether the specified value is infinite (positive or negative infinity).</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is infinite (positive or negative infinity); <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsInfinity(NFloat value) => NativeTypeIsDouble ? double.IsInfinity(value._value) : float.IsInfinity(value._value);

        /// <summary>Determines whether the specified value is NaN (not a number).</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is NaN (not a number); <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNaN(NFloat value) => NativeTypeIsDouble ? double.IsNaN(value._value) : float.IsNaN(value._value);

        /// <summary>Determines whether the specified value is negative.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is negative; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNegative(NFloat value) => NativeTypeIsDouble ? double.IsNegative(value._value) : float.IsNegative(value._value);

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is negative infinity; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNegativeInfinity(NFloat value) => NativeTypeIsDouble ? double.IsNegativeInfinity(value._value) : float.IsNegativeInfinity(value._value);

        /// <summary>Determines whether the specified value is normal.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is normal; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsNormal(NFloat value) => NativeTypeIsDouble ? double.IsNormal(value._value) : float.IsNormal(value._value);

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is positive infinity; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsPositiveInfinity(NFloat value) => NativeTypeIsDouble ? double.IsPositiveInfinity(value._value) : float.IsPositiveInfinity(value._value);

        /// <summary>Determines whether the specified value is subnormal.</summary>
        /// <param name="value">The floating-point value.</param>
        /// <returns><c>true</c> if the value is subnormal; <c>false</c> otherwise.</returns>
        [NonVersionable]
        public static bool IsSubnormal(NFloat value) => NativeTypeIsDouble ? double.IsSubnormal(value._value) : float.IsSubnormal(value._value);

        /// <summary>Converts the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s)
        {
            if (NativeTypeIsDouble)
            {
                var result = double.Parse(s);
                return new NFloat(result);
            }
            else
            {
                var result = float.Parse(s);
                return new NFloat(result);
            }
        }

        /// <summary>Converts the string representation of a number in a specified style to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s, NumberStyles style)
        {
            if (NativeTypeIsDouble)
            {
                var result = double.Parse(s, style);
                return new NFloat(result);
            }
            else
            {
                var result = float.Parse(s, style);
                return new NFloat(result);
            }
        }

        /// <summary>Converts the string representation of a number in a specified culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s, IFormatProvider? provider)
        {
            if (NativeTypeIsDouble)
            {
                var result = double.Parse(s, provider);
                return new NFloat(result);
            }
            else
            {
                var result = float.Parse(s, provider);
                return new NFloat(result);

            }
        }

        /// <summary>Converts the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A string that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="s" /> is <c>null</c>.</exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(string s, NumberStyles style, IFormatProvider? provider)
        {
            if (NativeTypeIsDouble)
            {
                var result = double.Parse(s, style, provider);
                return new NFloat(result);
            }
            else
            {
                var result = float.Parse(s, style, provider);
                return new NFloat(result);
            }
        }

        /// <summary>Converts a character span that contains the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A character span that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <returns>A floating-point number that is equivalent to the numeric value or symbol specified in <paramref name="s" />.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
        /// </exception>
        /// <exception cref="FormatException"><paramref name="s" /> does not represent a number in a valid format.</exception>
        public static NFloat Parse(ReadOnlySpan<char> s, NumberStyles style = DefaultNumberStyles, IFormatProvider? provider = null)
        {
            if (NativeTypeIsDouble)
            {
                var result = double.Parse(s, style, provider);
                return new NFloat(result);
            }
            else
            {
                var result = float.Parse(s, style, provider);
                return new NFloat(result);

            }
        }

        /// <summary>Tries to convert the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <c>null</c>, <see cref="string.Empty" />, or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse([NotNullWhen(true)] string? s, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeTypeIsDouble ? double.TryParse(s, out Unsafe.As<NFloat, double>(ref result)) : float.TryParse(s, out Unsafe.As<NFloat, float>(ref result));
        }

        /// <summary>Tries to convert a character span containing the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <see cref="ReadOnlySpan{T}.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<char> s, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeTypeIsDouble ? double.TryParse(s, out Unsafe.As<NFloat, double>(ref result)) : float.TryParse(s, out Unsafe.As<NFloat, float>(ref result));
        }

        /// <summary>Tries to convert a UTF-8 character span containing the string representation of a number to its floating-point number equivalent.</summary>
        /// <param name="utf8Text">A read-only UTF-8 character span that contains the number to convert.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="utf8Text" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="utf8Text" /> is <see cref="ReadOnlySpan{T}.Empty" /> or is not in a valid format. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="utf8Text" /> was converted successfully; otherwise, false.</returns>
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeTypeIsDouble ? double.TryParse(utf8Text, out Unsafe.As<NFloat, double>(ref result)) : float.TryParse(utf8Text, out Unsafe.As<NFloat, float>(ref result));
        }

        /// <summary>Tries to convert the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <c>null</c>, <see cref="string.Empty" />, or is not in a format compliant with <paramref name="style" />, or if <paramref name="style" /> is not a valid combination of <see cref="NumberStyles" /> enumeration constants. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
        /// </exception>
        public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeTypeIsDouble ? double.TryParse(s, style, provider, out Unsafe.As<NFloat, double>(ref result)) : float.TryParse(s, style, provider, out Unsafe.As<NFloat, float>(ref result));
        }

        /// <summary>Tries to convert a character span containing the string representation of a number in a specified style and culture-specific format to its floating-point number equivalent.</summary>
        /// <param name="s">A read-only character span that contains the number to convert.</param>
        /// <param name="style">A bitwise combination of enumeration values that indicate the style elements that can be present in <paramref name="s" />.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information about <paramref name="s" />.</param>
        /// <param name="result">When this method returns, contains a floating-point number equivalent of the numeric value or symbol contained in <paramref name="s" /> if the conversion succeeded or zero if the conversion failed. The conversion fails if the <paramref name="s" /> is <see cref="string.Empty" /> or is not in a format compliant with <paramref name="style" />, or if <paramref name="style" /> is not a valid combination of <see cref="NumberStyles" /> enumeration constants. This parameter is passed uninitialized; any value originally supplied in result will be overwritten.</param>
        /// <returns><c>true</c> if <paramref name="s" /> was converted successfully; otherwise, false.</returns>
        /// <exception cref="ArgumentException">
        ///    <para><paramref name="style" /> is not a <see cref="NumberStyles" /> value.</para>
        ///    <para>-or-</para>
        ///    <para><paramref name="style" /> includes the <see cref="NumberStyles.AllowHexSpecifier" /> or <see cref="NumberStyles.AllowBinarySpecifier" /> value.</para>
        /// </exception>
        public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeTypeIsDouble ? double.TryParse(s, style, provider, out Unsafe.As<NFloat, double>(ref result)) : float.TryParse(s, style, provider, out Unsafe.As<NFloat, float>(ref result));
        }

        /// <summary>Compares this instance to a specified object and returns an integer that indicates whether the value of this instance is less than, equal to, or greater than the value of the specified object.</summary>
        /// <param name="obj">An object to compare, or <c>null</c>.</param>
        /// <returns>
        ///     <para>A signed number indicating the relative values of this instance and <paramref name="obj" />.</para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance is less than <paramref name="obj" />, or this instance is not a number and <paramref name="obj" /> is a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance is equal to <paramref name="obj" />, or both this instance and <paramref name="obj" /> are not a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>This instance is greater than <paramref name="obj" />, or this instance is a number and <paramref name="obj" /> is not a number or <paramref name="obj" /> is <c>null</c>.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        /// <exception cref="ArgumentException"><paramref name="obj" /> is not a <see cref="NFloat" />.</exception>
        public int CompareTo(object? obj)
        {
            if (obj is NFloat other)
            {
                if (NativeTypeIsDouble)
                {
                    double value = _value;
                    double otherValue = other._value;
                    if (value < otherValue) return -1;
                    if (value > otherValue) return 1;
                    if (value == otherValue) return 0;

                    // At least one of the values is NaN.
                    if (double.IsNaN(value))
                    {
                        return double.IsNaN(otherValue) ? 0 : -1;
                    }
                }
                else
                {
                    float value = _value;
                    float otherValue = other._value;
                    if (value < otherValue) return -1;
                    if (value > otherValue) return 1;
                    if (value == otherValue) return 0;

                    // At least one of the values is NaN.
                    if (double.IsNaN(value))
                    {
                        return double.IsNaN(otherValue) ? 0 : -1;
                    }
                }

                return 1;
            }
            else if (obj is null)
            {
                return 1;
            }

            throw new ArgumentException(SR.Arg_MustBeNFloat);
        }

        /// <summary>Compares this instance to a specified floating-point number and returns an integer that indicates whether the value of this instance is less than, equal to, or greater than the value of the specified floating-point number.</summary>
        /// <param name="other">A floating-point number to compare.</param>
        /// <returns>
        ///     <para>A signed number indicating the relative values of this instance and <paramref name="other" />.</para>
        ///     <list type="table">
        ///         <listheader>
        ///             <term>Return Value</term>
        ///             <description>Description</description>
        ///         </listheader>
        ///         <item>
        ///             <term>Less than zero</term>
        ///             <description>This instance is less than <paramref name="other" />, or this instance is not a number and <paramref name="other" /> is a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Zero</term>
        ///             <description>This instance is equal to <paramref name="other" />, or both this instance and <paramref name="other" /> are not a number.</description>
        ///         </item>
        ///         <item>
        ///             <term>Greater than zero</term>
        ///             <description>This instance is greater than <paramref name="other" />, or this instance is a number and <paramref name="other" /> is not a number.</description>
        ///         </item>
        ///     </list>
        /// </returns>
        public int CompareTo(NFloat other) => NativeTypeIsDouble ? ((double)_value).CompareTo(other._value) : ((float)_value).CompareTo(other._value);

        /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
        /// <param name="obj">An object to compare with this instance.</param>
        /// <returns><c>true</c> if <paramref name="obj"/> is an instance of <see cref="NFloat"/> and equals the value of this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals([NotNullWhen(true)] object? obj) => (obj is NFloat other) && Equals(other);

        /// <summary>Returns a value indicating whether this instance is equal to a specified <see cref="NFloat" /> value.</summary>
        /// <param name="other">An <see cref="NFloat"/> value to compare to this instance.</param>
        /// <returns><c>true</c> if <paramref name="other"/> has the same value as this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(NFloat other) => NativeTypeIsDouble ? ((double)_value).Equals(other._value) : ((float)_value).Equals(other._value);

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode() => NativeTypeIsDouble ? ((double)_value).GetHashCode() : ((float)_value).GetHashCode();

        /// <summary>Converts the numeric value of this instance to its equivalent string representation.</summary>
        /// <returns>The string representation of the value of this instance.</returns>
        public override string ToString() => NativeTypeIsDouble ? ((double)_value).ToString() : ((float)_value).ToString();

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified format.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format" />.</returns>
        /// <exception cref="FormatException"><paramref name="format" /> is invalid.</exception>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format) => NativeTypeIsDouble ? ((double)_value).ToString(format) : ((float)_value).ToString(format);

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified culture-specific format information.</summary>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="provider" />.</returns>
        public string ToString(IFormatProvider? provider)=> NativeTypeIsDouble ? ((double)_value).ToString(provider) : ((float)_value).ToString(provider);

        /// <summary>Converts the numeric value of this instance to its equivalent string representation using the specified format and culture-specific format information.</summary>
        /// <param name="format">A numeric format string.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information.</param>
        /// <returns>The string representation of the value of this instance as specified by <paramref name="format" /> and <paramref name="provider" />.</returns>
        /// <exception cref="FormatException"><paramref name="format" /> is invalid.</exception>
        public string ToString([StringSyntax(StringSyntaxAttribute.NumericFormat)] string? format, IFormatProvider? provider) => NativeTypeIsDouble ? ((double)_value).ToString(format, provider) : ((float)_value).ToString(format, provider);

        /// <summary>Tries to format the value of the current instance into the provided span of characters.</summary>
        /// <param name="destination">The span in which to write this instance's value formatted as a span of characters.</param>
        /// <param name="charsWritten">When this method returns, contains the number of characters that were written in <paramref name="destination" />.</param>
        /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format for <paramref name="destination" />.</param>
        /// <param name="provider">An optional object that supplies culture-specific formatting information for <paramref name="destination" />.</param>
        /// <returns><c>true</c> if the formatting was successful; otherwise, <c>false</c>.</returns>
        public bool TryFormat(Span<char> destination, out int charsWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) => NativeTypeIsDouble ? ((double)_value).TryFormat(destination, out charsWritten, format, provider) : ((float)_value).TryFormat(destination, out charsWritten, format, provider);

        /// <inheritdoc cref="IUtf8SpanFormattable.TryFormat" />
        public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, [StringSyntax(StringSyntaxAttribute.NumericFormat)] ReadOnlySpan<char> format = default, IFormatProvider? provider = null) => NativeTypeIsDouble ? ((double)_value).TryFormat(utf8Destination, out bytesWritten, format, provider) : ((float)_value).TryFormat(utf8Destination, out bytesWritten, format, provider);

        //
        // IAdditiveIdentity
        //

        /// <inheritdoc cref="IAdditiveIdentity{TSelf, TResult}.AdditiveIdentity" />
        static NFloat IAdditiveIdentity<NFloat, NFloat>.AdditiveIdentity => NativeTypeIsDouble ? new NFloat(double.AdditiveIdentity) : new NFloat(float.AdditiveIdentity);

        //
        // IBinaryNumber
        //

        /// <inheritdoc cref="IBinaryNumber{TSelf}.AllBitsSet" />
        static NFloat IBinaryNumber<NFloat>.AllBitsSet
        {
            [NonVersionable]
            get
            {
                return NativeTypeIsDouble ? (NFloat)BitConverter.UInt64BitsToDouble(0xFFFF_FFFF_FFFF_FFFF) : BitConverter.UInt32BitsToSingle(0xFFFF_FFFF);
            }
        }

        /// <inheritdoc cref="IBinaryNumber{TSelf}.IsPow2(TSelf)" />
        public static bool IsPow2(NFloat value) => NativeTypeIsDouble ? double.IsPow2(value._value) : float.IsPow2(value._value);

        /// <inheritdoc cref="IBinaryNumber{TSelf}.Log2(TSelf)" />
        public static NFloat Log2(NFloat value) => NativeTypeIsDouble ? new NFloat(double.Log2(value._value)) : new NFloat(float.Log2(value._value));

        //
        // IBitwiseOperators
        //

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseAnd(TSelf, TOther)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator &(NFloat left, NFloat right)
        {
            if (!NativeTypeIsDouble)
            {
                uint bits = BitConverter.SingleToUInt32Bits(left._value) & BitConverter.SingleToUInt32Bits(right._value);
                float result = BitConverter.UInt32BitsToSingle(bits);
                return new NFloat(result);
            }
            else
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(left._value) & BitConverter.DoubleToUInt64Bits(right._value);
                double result = BitConverter.UInt64BitsToDouble(bits);
                return new NFloat(result);
            }
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_BitwiseOr(TSelf, TOther)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator |(NFloat left, NFloat right)
        {
            if (!NativeTypeIsDouble)
            {
                uint bits = BitConverter.SingleToUInt32Bits(left._value) | BitConverter.SingleToUInt32Bits(right._value);
                float result = BitConverter.UInt32BitsToSingle(bits);
                return new NFloat(result);
            }
            else
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(left._value) | BitConverter.DoubleToUInt64Bits(right._value);
                double result = BitConverter.UInt64BitsToDouble(bits);
                return new NFloat(result);
            }
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_ExclusiveOr(TSelf, TOther)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator ^(NFloat left, NFloat right)
        {
            if (!NativeTypeIsDouble)
            {
                uint bits = BitConverter.SingleToUInt32Bits(left._value) ^ BitConverter.SingleToUInt32Bits(right._value);
                float result = BitConverter.UInt32BitsToSingle(bits);
                return new NFloat(result);
            }
            else
            {
                ulong bits = BitConverter.DoubleToUInt64Bits(left._value) ^ BitConverter.DoubleToUInt64Bits(right._value);
                double result = BitConverter.UInt64BitsToDouble(bits);
                return new NFloat(result);
            }
        }

        /// <inheritdoc cref="IBitwiseOperators{TSelf, TOther, TResult}.op_OnesComplement(TSelf)" />
        static NFloat IBitwiseOperators<NFloat, NFloat, NFloat>.operator ~(NFloat value)
        {
            if (!NativeTypeIsDouble)
            {
                uint bits = ~BitConverter.SingleToUInt32Bits(value._value);
                float result = BitConverter.UInt32BitsToSingle(bits);
                return new NFloat(result);
            }
            else
            {
                ulong bits = ~BitConverter.DoubleToUInt64Bits(value._value);
                double result = BitConverter.UInt64BitsToDouble(bits);
                return new NFloat(result);
            }
        }

        //
        // IExponentialFunctions
        //

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp" />
        public static NFloat Exp(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Exp(x._value)) : new NFloat(float.Exp(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.ExpM1(TSelf)" />
        public static NFloat ExpM1(NFloat x) => NativeTypeIsDouble ? new NFloat(double.ExpM1(x._value)) : new NFloat(float.ExpM1(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2(TSelf)" />
        public static NFloat Exp2(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Exp2(x._value)) : new NFloat(float.Exp2(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp2M1(TSelf)" />
        public static NFloat Exp2M1(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Exp2M1(x._value)) : new NFloat(float.Exp2M1(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10(TSelf)" />
        public static NFloat Exp10(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Exp10(x._value)) : new NFloat(float.Exp10(x._value));

        /// <inheritdoc cref="IExponentialFunctions{TSelf}.Exp10M1(TSelf)" />
        public static NFloat Exp10M1(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Exp10M1(x._value)) : new NFloat(float.Exp10M1(x._value));

        //
        // IFloatingPoint
        //

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Ceiling(TSelf)" />
        public static NFloat Ceiling(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Ceiling(x._value)) : new NFloat(float.Ceiling(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Floor(TSelf)" />
        public static NFloat Floor(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Floor(x._value)) : new NFloat(float.Floor(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf)" />
        public static NFloat Round(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Round(x._value)) : new NFloat(float.Round(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int)" />
        public static NFloat Round(NFloat x, int digits) => NativeTypeIsDouble ? new NFloat(double.Round(x._value, digits)) : new NFloat(float.Round(x._value, digits));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, MidpointRounding)" />
        public static NFloat Round(NFloat x, MidpointRounding mode) => NativeTypeIsDouble ? new NFloat(double.Round(x._value, mode)) : new NFloat(float.Round(x._value, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Round(TSelf, int, MidpointRounding)" />
        public static NFloat Round(NFloat x, int digits, MidpointRounding mode) => NativeTypeIsDouble ? new NFloat(double.Round(x._value, digits, mode)) : new NFloat(float.Round(x._value, digits, mode));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.Truncate(TSelf)" />
        public static NFloat Truncate(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Truncate(x._value)) : new NFloat(float.Truncate(x._value));

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentByteCount()" />
        int IFloatingPoint<NFloat>.GetExponentByteCount() => SizeOfExponent;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetExponentShortestBitLength()" />
        int IFloatingPoint<NFloat>.GetExponentShortestBitLength()
        {
            if (NativeTypeIsDouble)
            {
                Int16 exponent = ((double)_value).Exponent;

                if (exponent >= 0)
                {
                    return (sizeof(Int16) * 8) - Int16.LeadingZeroCount(exponent);
                }
                else
                {
                    return (sizeof(Int16) * 8) + 1 - Int16.LeadingZeroCount((Int16)(~exponent));
                }
            }
            else
            {
                SByte exponent = ((float)_value).Exponent;

                if (exponent >= 0)
                {
                    return (sizeof(SByte) * 8) - SByte.LeadingZeroCount(exponent);
                }
                else
                {
                    return (sizeof(SByte) * 8) + 1 - SByte.LeadingZeroCount((SByte)(~exponent));
                }

            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandByteCount()" />
        int IFloatingPoint<NFloat>.GetSignificandByteCount() => SizeOfSignitcand;

        /// <inheritdoc cref="IFloatingPoint{TSelf}.GetSignificandBitLength()" />
        int IFloatingPoint<NFloat>.GetSignificandBitLength()
        {
            if (!NativeTypeIsDouble)
                return 24;
            else
                return 53;
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteExponentBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= SizeOfExponent)
            {
                if (NativeTypeIsDouble)
                {
                    var exponent = ((double)_value).Exponent;

                    if (BitConverter.IsLittleEndian)
                    {
                        exponent = BinaryPrimitives.ReverseEndianness(exponent);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
                }
                else
                {
                    var exponent = ((float)_value).Exponent;

                    if (BitConverter.IsLittleEndian)
                    {
                        exponent = BinaryPrimitives.ReverseEndianness(exponent);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                }

                bytesWritten = SizeOfExponent;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteExponentLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteExponentLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= SizeOfExponent)
            {
                if (NativeTypeIsDouble)
                {
                    var exponent = ((double)_value).Exponent;

                    if (!BitConverter.IsLittleEndian)
                    {
                        exponent = BinaryPrimitives.ReverseEndianness(exponent);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);
                }
                else
                {
                    var exponent = ((float)_value).Exponent;

                    if (!BitConverter.IsLittleEndian)
                    {
                        exponent = BinaryPrimitives.ReverseEndianness(exponent);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), exponent);

                }

                bytesWritten = SizeOfExponent;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandBigEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteSignificandBigEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= SizeOfSignitcand)
            {
                if (NativeTypeIsDouble)
                {
                    var significand = ((double)_value).Significand;

                    if (BitConverter.IsLittleEndian)
                    {
                        significand = BinaryPrimitives.ReverseEndianness(significand);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
                }
                else
                {
                    var significand = ((double)_value).Significand;

                    if (BitConverter.IsLittleEndian)
                    {
                        significand = BinaryPrimitives.ReverseEndianness(significand);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
                }

                bytesWritten = SizeOfSignitcand;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        /// <inheritdoc cref="IFloatingPoint{TSelf}.TryWriteSignificandLittleEndian(Span{byte}, out int)" />
        bool IFloatingPoint<NFloat>.TryWriteSignificandLittleEndian(Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length >= SizeOfSignitcand)
            {
                if (NativeTypeIsDouble)
                {
                    var significand = ((double)_value).Significand;

                    if (!BitConverter.IsLittleEndian)
                    {
                        significand = BinaryPrimitives.ReverseEndianness(significand);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);
                }
                else
                {
                    var significand = ((float)_value).Significand;

                    if (!BitConverter.IsLittleEndian)
                    {
                        significand = BinaryPrimitives.ReverseEndianness(significand);
                    }

                    Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), significand);

                }

                bytesWritten = SizeOfSignitcand;
                return true;
            }
            else
            {
                bytesWritten = 0;
                return false;
            }
        }

        //
        // IFloatingPointConstants
        //

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.E" />
        public static NFloat E => NativeTypeIsDouble ? new NFloat(double.E) : new NFloat(float.E);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Pi" />
        public static NFloat Pi => NativeTypeIsDouble ? new NFloat(double.Pi) : new NFloat(float.Pi);

        /// <inheritdoc cref="IFloatingPointConstants{TSelf}.Tau" />
        public static NFloat Tau => NativeTypeIsDouble ? new NFloat(double.Tau) : new NFloat(float.Tau);

        //
        // IFloatingPointIeee754
        //

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.NegativeZero" />
        public static NFloat NegativeZero => NativeTypeIsDouble ? new NFloat(double.NegativeZero) : new NFloat(float.NegativeZero);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2(TSelf, TSelf)" />
        public static NFloat Atan2(NFloat y, NFloat x) => NativeTypeIsDouble ? new NFloat(double.Atan2(y._value, x._value)) : new NFloat(float.Atan2(y._value, x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Atan2Pi(TSelf, TSelf)" />
        public static NFloat Atan2Pi(NFloat y, NFloat x) => NativeTypeIsDouble ? new NFloat(double.Atan2Pi(y._value, x._value)) : new NFloat(float.Atan2Pi(y._value, x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitDecrement(TSelf)" />
        public static NFloat BitDecrement(NFloat x) => NativeTypeIsDouble ? new NFloat(double.BitDecrement(x._value)) : new NFloat(float.BitDecrement(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.BitIncrement(TSelf)" />
        public static NFloat BitIncrement(NFloat x) => NativeTypeIsDouble ? new NFloat(double.BitIncrement(x._value)) : new NFloat(float.BitIncrement(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.FusedMultiplyAdd(TSelf, TSelf, TSelf)" />
        public static NFloat FusedMultiplyAdd(NFloat left, NFloat right, NFloat addend) => NativeTypeIsDouble ? new NFloat(double.FusedMultiplyAdd(left._value, right._value, addend._value)) : new NFloat(float.FusedMultiplyAdd(left._value, right._value, addend._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Ieee754Remainder(TSelf, TSelf)" />
        public static NFloat Ieee754Remainder(NFloat left, NFloat right) => NativeTypeIsDouble ? new NFloat(double.Ieee754Remainder(left._value, right._value)) : new NFloat(float.Ieee754Remainder(left._value, right._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ILogB(TSelf)" />
        public static int ILogB(NFloat x) => NativeTypeIsDouble ? double.ILogB(x._value) : float.ILogB(x._value);

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Lerp(TSelf, TSelf, TSelf)" />
        public static NFloat Lerp(NFloat value1, NFloat value2, NFloat amount) => NativeTypeIsDouble ? new NFloat(double.Lerp(value1._value, value2._value, amount._value)) : new NFloat(float.Lerp(value1._value, value2._value, amount._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalEstimate(TSelf)" />
        public static NFloat ReciprocalEstimate(NFloat x) => NativeTypeIsDouble ? new NFloat(double.ReciprocalEstimate(x._value)) : new NFloat(float.ReciprocalEstimate(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ReciprocalSqrtEstimate(TSelf)" />
        public static NFloat ReciprocalSqrtEstimate(NFloat x) => NativeTypeIsDouble ? new NFloat(double.ReciprocalSqrtEstimate(x._value)) : new NFloat(float.ReciprocalSqrtEstimate(x._value));

        /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.ScaleB(TSelf, int)" />
        public static NFloat ScaleB(NFloat x, int n) => NativeTypeIsDouble ? new NFloat(double.ScaleB(x._value, n)) : new NFloat(float.ScaleB(x._value, n));

        // /// <inheritdoc cref="IFloatingPointIeee754{TSelf}.Compound(TSelf, TSelf)" />
        // public static NFloat Compound(NFloat x, NFloat n) => NativeTypeIsDouble ? new NFloat(double.Compound(x._value, n._value)) : new NFloat(float.Compound(x._value, n._value));

        //
        // IHyperbolicFunctions
        //

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Acosh(TSelf)" />
        public static NFloat Acosh(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Acosh(x._value)) : new NFloat(float.Acosh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Asinh(TSelf)" />
        public static NFloat Asinh(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Asinh(x._value)) : new NFloat(float.Asinh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Atanh(TSelf)" />
        public static NFloat Atanh(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Atanh(x._value)) : new NFloat(float.Atanh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Cosh(TSelf)" />
        public static NFloat Cosh(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Cosh(x._value)) : new NFloat(float.Cosh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Sinh(TSelf)" />
        public static NFloat Sinh(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Sinh(x._value)) : new NFloat(float.Sinh(x._value));

        /// <inheritdoc cref="IHyperbolicFunctions{TSelf}.Tanh(TSelf)" />
        public static NFloat Tanh(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Tanh(x._value)) : new NFloat(float.Tanh(x._value));

        //
        // ILogarithmicFunctions
        //

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf)" />
        public static NFloat Log(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Log(x._value)) : new NFloat(float.Log(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log(TSelf, TSelf)" />
        public static NFloat Log(NFloat x, NFloat newBase) => NativeTypeIsDouble ? new NFloat(double.Log(x._value, newBase._value)) : new NFloat(float.Log(x._value, newBase._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.LogP1(TSelf)" />
        public static NFloat LogP1(NFloat x) => NativeTypeIsDouble ? new NFloat(double.LogP1(x._value)) : new NFloat(float.LogP1(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log2P1(TSelf)" />
        public static NFloat Log2P1(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Log2P1(x._value)) : new NFloat(float.Log2P1(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10(TSelf)" />
        public static NFloat Log10(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Log10(x._value)) : new NFloat(float.Log10(x._value));

        /// <inheritdoc cref="ILogarithmicFunctions{TSelf}.Log10P1(TSelf)" />
        public static NFloat Log10P1(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Log10P1(x._value)) : new NFloat(float.Log10P1(x._value));

        //
        // IMultiplicativeIdentity
        //

        /// <inheritdoc cref="IMultiplicativeIdentity{TSelf, TResult}.MultiplicativeIdentity" />
        static NFloat IMultiplicativeIdentity<NFloat, NFloat>.MultiplicativeIdentity => NativeTypeIsDouble ? new NFloat(double.MultiplicativeIdentity) : new NFloat(float.MultiplicativeIdentity);

        //
        // INumber
        //

        /// <inheritdoc cref="INumber{TSelf}.Clamp(TSelf, TSelf, TSelf)" />
        public static NFloat Clamp(NFloat value, NFloat min, NFloat max) => NativeTypeIsDouble ? new NFloat(double.Clamp(value._value, min._value, max._value)) : new NFloat(float.Clamp(value._value, min._value, max._value));

        /// <inheritdoc cref="INumber{TSelf}.CopySign(TSelf, TSelf)" />
        public static NFloat CopySign(NFloat value, NFloat sign) => NativeTypeIsDouble ? new NFloat(double.CopySign(value._value, sign._value)) : new NFloat(float.CopySign(value._value, sign._value));

        /// <inheritdoc cref="INumber{TSelf}.Max(TSelf, TSelf)" />
        public static NFloat Max(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.Max(x._value, y._value)) : new NFloat(float.Max(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MaxNumber(TSelf, TSelf)" />
        public static NFloat MaxNumber(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.MaxNumber(x._value, y._value)) : new NFloat(float.MaxNumber(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Min(TSelf, TSelf)" />
        public static NFloat Min(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.Min(x._value, y._value)) : new NFloat(float.Min(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.MinNumber(TSelf, TSelf)" />
        public static NFloat MinNumber(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.MinNumber(x._value, y._value)) : new NFloat(float.MinNumber(x._value, y._value));

        /// <inheritdoc cref="INumber{TSelf}.Sign(TSelf)" />
        public static int Sign(NFloat value) => NativeTypeIsDouble ? double.Sign(value._value) : float.Sign(value._value);

        //
        // INumberBase
        //

        /// <inheritdoc cref="INumberBase{TSelf}.One" />
        static NFloat INumberBase<NFloat>.One => NativeTypeIsDouble ? new NFloat(double.One) : new NFloat(float.One);

        /// <inheritdoc cref="INumberBase{TSelf}.Radix" />
        static int INumberBase<NFloat>.Radix => 2;

        /// <inheritdoc cref="INumberBase{TSelf}.Zero" />
        static NFloat INumberBase<NFloat>.Zero => NativeTypeIsDouble ? new NFloat(double.Zero) : new NFloat(float.Zero);

        /// <inheritdoc cref="INumberBase{TSelf}.Abs(TSelf)" />
        public static NFloat Abs(NFloat value) => NativeTypeIsDouble ? new NFloat(double.Abs(value._value)) : new NFloat(float.Abs(value._value));

        /// <inheritdoc cref="INumberBase{TSelf}.CreateChecked{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateChecked<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            NFloat result;

            if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToChecked(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateSaturating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateSaturating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            NFloat result;

            if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToSaturating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.CreateTruncating{TOther}(TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NFloat CreateTruncating<TOther>(TOther value)
            where TOther : INumberBase<TOther>
        {
            NFloat result;

            if (typeof(TOther) == typeof(NFloat))
            {
                result = (NFloat)(object)value;
            }
            else if (!TryConvertFrom(value, out result) && !TOther.TryConvertToTruncating(value, out result))
            {
                ThrowHelper.ThrowNotSupportedException();
            }

            return result;
        }

        /// <inheritdoc cref="INumberBase{TSelf}.IsCanonical(TSelf)" />
        static bool INumberBase<NFloat>.IsCanonical(NFloat value) => true;

        /// <inheritdoc cref="INumberBase{TSelf}.IsComplexNumber(TSelf)" />
        static bool INumberBase<NFloat>.IsComplexNumber(NFloat value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsEvenInteger(TSelf)" />
        public static bool IsEvenInteger(NFloat value) => NativeTypeIsDouble ? double.IsEvenInteger(value._value) : float.IsEvenInteger(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsImaginaryNumber(TSelf)" />
        static bool INumberBase<NFloat>.IsImaginaryNumber(NFloat value) => false;

        /// <inheritdoc cref="INumberBase{TSelf}.IsInteger(TSelf)" />
        public static bool IsInteger(NFloat value) => NativeTypeIsDouble ? double.IsInteger(value._value) : float.IsInteger(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsOddInteger(TSelf)" />
        public static bool IsOddInteger(NFloat value) => NativeTypeIsDouble ? double.IsOddInteger(value._value) : float.IsOddInteger(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsPositive(TSelf)" />
        public static bool IsPositive(NFloat value) => NativeTypeIsDouble ? double.IsPositive(value._value) : float.IsPositive(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsRealNumber(TSelf)" />
        public static bool IsRealNumber(NFloat value) => NativeTypeIsDouble ? double.IsRealNumber(value._value) : float.IsRealNumber(value._value);

        /// <inheritdoc cref="INumberBase{TSelf}.IsZero(TSelf)" />
        static bool INumberBase<NFloat>.IsZero(NFloat value) => (value == 0);

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitude(TSelf, TSelf)" />
        public static NFloat MaxMagnitude(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.MaxMagnitude(x._value, y._value)) : new NFloat(float.MaxMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.MaxMagnitudeNumber(TSelf, TSelf)" />
        public static NFloat MaxMagnitudeNumber(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.MaxMagnitudeNumber(x._value, y._value)) : new NFloat(float.MaxMagnitudeNumber(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitude(TSelf, TSelf)" />
        public static NFloat MinMagnitude(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.MinMagnitude(x._value, y._value)) : new NFloat(float.MinMagnitude(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.MinMagnitudeNumber(TSelf, TSelf)" />
        public static NFloat MinMagnitudeNumber(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.MinMagnitudeNumber(x._value, y._value)) : new NFloat(float.MinMagnitudeNumber(x._value, y._value));

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromChecked{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertFromChecked<TOther>(TOther value, out NFloat result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromSaturating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertFromSaturating<TOther>(TOther value, out NFloat result)
        {
            return TryConvertFrom(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertFromTruncating{TOther}(TOther, out TSelf)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertFromTruncating<TOther>(TOther value, out NFloat result)
        {
            return TryConvertFrom(value, out result);
        }

        private static bool TryConvertFrom<TOther>(TOther value, out NFloat result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualValue = (byte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualValue = (char)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualValue = (decimal)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualValue = (double)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualValue = (Half)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualValue = (short)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualValue = (int)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualValue = (long)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualValue = (Int128)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualValue = (nint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualValue = (sbyte)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualValue = (float)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualValue = (ushort)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualValue = (uint)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualValue = (ulong)(object)value;
                result = actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualValue = (UInt128)(object)value;
                result = (NFloat)actualValue;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualValue = (nuint)(object)value;
                result = actualValue;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToChecked{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertToChecked<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = checked((byte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = checked((char)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = checked((decimal)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = checked((short)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = checked((int)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = checked((long)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = checked((Int128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = checked((nint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = checked((sbyte)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = checked((ushort)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = checked((uint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = checked((ulong)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = checked((UInt128)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                nuint actualResult = checked((nuint)value);
                result = (TOther)(object)actualResult;
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToSaturating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertToSaturating<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryConvertToTruncating{TOther}(TSelf, out TOther)" />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool INumberBase<NFloat>.TryConvertToTruncating<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
        {
            return TryConvertTo(value, out result);
        }

        private static bool TryConvertTo<TOther>(NFloat value, [MaybeNullWhen(false)] out TOther result)
            where TOther : INumberBase<TOther>
        {
            if (typeof(TOther) == typeof(byte))
            {
                byte actualResult = (value >= byte.MaxValue) ? byte.MaxValue :
                                    (value <= byte.MinValue) ? byte.MinValue : (byte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(char))
            {
                char actualResult = (value >= char.MaxValue) ? char.MaxValue :
                                    (value <= char.MinValue) ? char.MinValue : (char)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(decimal))
            {
                decimal actualResult = (value >= +79228162514264337593543950336.0f) ? decimal.MaxValue :
                                       (value <= -79228162514264337593543950336.0f) ? decimal.MinValue :
                                       IsNaN(value) ? 0.0m : (decimal)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(double))
            {
                double actualResult = value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Half))
            {
                Half actualResult = (Half)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(short))
            {
                short actualResult = (value >= short.MaxValue) ? short.MaxValue :
                                     (value <= short.MinValue) ? short.MinValue : (short)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(int))
            {
                int actualResult = (value >= int.MaxValue) ? int.MaxValue :
                                   (value <= int.MinValue) ? int.MinValue : (int)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(long))
            {
                long actualResult = (value >= long.MaxValue) ? long.MaxValue :
                                    (value <= long.MinValue) ? long.MinValue : (long)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(Int128))
            {
                Int128 actualResult = (value >= +170141183460469231731687303715884105727.0) ? Int128.MaxValue :
                                      (value <= -170141183460469231731687303715884105728.0) ? Int128.MinValue : (Int128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nint))
            {
                nint actualResult = (value >= nint.MaxValue) ? nint.MaxValue :
                                    (value <= nint.MinValue) ? nint.MinValue : (nint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(sbyte))
            {
                sbyte actualResult = (value >= sbyte.MaxValue) ? sbyte.MaxValue :
                                     (value <= sbyte.MinValue) ? sbyte.MinValue : (sbyte)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(float))
            {
                float actualResult = (float)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ushort))
            {
                ushort actualResult = (value >= ushort.MaxValue) ? ushort.MaxValue :
                                      (value <= ushort.MinValue) ? ushort.MinValue : (ushort)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(uint))
            {
                uint actualResult = (value >= uint.MaxValue) ? uint.MaxValue :
                                    (value <= uint.MinValue) ? uint.MinValue : (uint)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(ulong))
            {
                ulong actualResult = (value >= ulong.MaxValue) ? ulong.MaxValue :
                                     (value <= ulong.MinValue) ? ulong.MinValue :
                                     IsNaN(value) ? 0 : (ulong)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(UInt128))
            {
                UInt128 actualResult = (value >= 340282366920938463463374607431768211455.0) ? UInt128.MaxValue :
                                       (value <= 0.0) ? UInt128.MinValue : (UInt128)value;
                result = (TOther)(object)actualResult;
                return true;
            }
            else if (typeof(TOther) == typeof(nuint))
            {
                if (RuntimeHelpers.TargetIs32Bit)
                {
                    nuint actualResult = (value >= uint.MaxValue) ? unchecked((nuint)uint.MaxValue) :
                                         (value <= uint.MinValue) ? unchecked((nuint)uint.MinValue) : (nuint)value;
                    result = (TOther)(object)actualResult;
                    return true;
                }
                else
                {
                    nuint actualResult = (value >= ulong.MaxValue) ? unchecked((nuint)ulong.MaxValue) :
                                         (value <= ulong.MinValue) ? unchecked((nuint)ulong.MinValue) : (nuint)value;
                    result = (TOther)(object)actualResult;
                    return true;
                }
            }
            else
            {
                result = default;
                return false;
            }
        }

        //
        // IParsable
        //

        /// <inheritdoc cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" />
        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out NFloat result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // IPowerFunctions
        //

        /// <inheritdoc cref="IPowerFunctions{TSelf}.Pow(TSelf, TSelf)" />
        public static NFloat Pow(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.Pow(x._value, y._value)) : new NFloat(float.Pow(x._value, y._value));

        //
        // IRootFunctions
        //

        /// <inheritdoc cref="IRootFunctions{TSelf}.Cbrt(TSelf)" />
        public static NFloat Cbrt(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Cbrt(x._value)) : new NFloat(float.Cbrt(x._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Hypot(TSelf, TSelf)" />
        public static NFloat Hypot(NFloat x, NFloat y) => NativeTypeIsDouble ? new NFloat(double.Hypot(x._value, y._value)) : new NFloat(float.Hypot(x._value, y._value));

        /// <inheritdoc cref="IRootFunctions{TSelf}.RootN(TSelf, int)" />
        public static NFloat RootN(NFloat x, int n) => NativeTypeIsDouble ? new NFloat(double.RootN(x._value, n)) : new NFloat(float.RootN(x._value, n));

        /// <inheritdoc cref="IRootFunctions{TSelf}.Sqrt(TSelf)" />
        public static NFloat Sqrt(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Sqrt(x._value)) : new NFloat(float.Sqrt(x._value));

        //
        // ISignedNumber
        //

        /// <inheritdoc cref="ISignedNumber{TSelf}.NegativeOne" />
        static NFloat ISignedNumber<NFloat>.NegativeOne => NativeTypeIsDouble ? new NFloat(double.NegativeOne) : new NFloat(float.NegativeOne);

        //
        // ISpanParsable
        //

        /// <inheritdoc cref="ISpanParsable{TSelf}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
        public static NFloat Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="ISpanParsable{TSelf}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, out NFloat result) => TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);

        //
        // ITrigonometricFunctions
        //

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Acos(TSelf)" />
        public static NFloat Acos(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Acos(x._value)) : new NFloat(float.Acos(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AcosPi(TSelf)" />
        public static NFloat AcosPi(NFloat x) => NativeTypeIsDouble ? new NFloat(double.AcosPi(x._value)) : new NFloat(float.AcosPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Asin(TSelf)" />
        public static NFloat Asin(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Asin(x._value)) : new NFloat(float.Asin(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AsinPi(TSelf)" />
        public static NFloat AsinPi(NFloat x) => NativeTypeIsDouble ? new NFloat(double.AsinPi(x._value)) : new NFloat(float.AsinPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Atan(TSelf)" />
        public static NFloat Atan(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Atan(x._value)) : new NFloat(float.Atan(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.AtanPi(TSelf)" />
        public static NFloat AtanPi(NFloat x) => NativeTypeIsDouble ? new NFloat(double.AtanPi(x._value)) : new NFloat(float.AtanPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Cos(TSelf)" />
        public static NFloat Cos(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Cos(x._value)) : new NFloat(float.Cos(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.CosPi(TSelf)" />
        public static NFloat CosPi(NFloat x) => NativeTypeIsDouble ? new NFloat(double.CosPi(x._value)) : new NFloat(float.CosPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.DegreesToRadians(TSelf)" />
        public static NFloat DegreesToRadians(NFloat degrees)
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return NativeTypeIsDouble ? new NFloat(double.DegreesToRadians(degrees._value)) : new NFloat(float.DegreesToRadians(degrees._value));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.RadiansToDegrees(TSelf)" />
        public static NFloat RadiansToDegrees(NFloat radians)
        {
            // NOTE: Don't change the algorithm without consulting the DIM
            // which elaborates on why this implementation was chosen

            return NativeTypeIsDouble ? new NFloat(double.RadiansToDegrees(radians._value)) : new NFloat(float.RadiansToDegrees(radians._value));
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Sin(TSelf)" />
        public static NFloat Sin(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Sin(x._value)) : new NFloat(float.Sin(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (NFloat Sin, NFloat Cos) SinCos(NFloat x)
        {
            if (NativeTypeIsDouble)
            {
                var (sin, cos) = double.SinCos(x._value);
                return (new NFloat(sin), new NFloat(cos));
            }
            else
            {
                var (sin, cos) = float.SinCos(x._value);
                return (new NFloat(sin), new NFloat(cos));
            }
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinCos(TSelf)" />
        public static (NFloat SinPi, NFloat CosPi) SinCosPi(NFloat x)
        {
            if (NativeTypeIsDouble)
            {
                var (sin, cos) = double.SinCosPi(x._value);
                return (new NFloat(sin), new NFloat(cos));
            }
            else
            {
                var (sin, cos) = float.SinCosPi(x._value);
                return (new NFloat(sin), new NFloat(cos));
            }
        }

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.SinPi(TSelf)" />
        public static NFloat SinPi(NFloat x) => NativeTypeIsDouble ? new NFloat(double.SinPi(x._value)) : new NFloat(float.SinPi(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.Tan(TSelf)" />
        public static NFloat Tan(NFloat x) => NativeTypeIsDouble ? new NFloat(double.Tan(x._value)) : new NFloat(float.Tan(x._value));

        /// <inheritdoc cref="ITrigonometricFunctions{TSelf}.TanPi(TSelf)" />
        public static NFloat TanPi(NFloat x) => NativeTypeIsDouble ? new NFloat(double.TanPi(x._value)) : new NFloat(float.TanPi(x._value));

        //
        // IUtf8SpanParsable
        //

        /// <inheritdoc cref="INumberBase{TSelf}.Parse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?)" />
        public static NFloat Parse(ReadOnlySpan<byte> utf8Text, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
        {
            if (NativeTypeIsDouble)
            {
                var result = double.Parse(utf8Text, style, provider);
                return new NFloat(result);
            }
            else
            {
                var result = float.Parse(utf8Text, style, provider);
                return new NFloat(result);
            }
        }

        /// <inheritdoc cref="INumberBase{TSelf}.TryParse(ReadOnlySpan{byte}, NumberStyles, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, NumberStyles style, IFormatProvider? provider, out NFloat result)
        {
            Unsafe.SkipInit(out result);
            return NativeTypeIsDouble ? double.TryParse(utf8Text, style, provider, out Unsafe.As<NFloat, double>(ref result)) : float.TryParse(utf8Text, style, provider, out Unsafe.As<NFloat, float>(ref result));
        }

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.Parse(ReadOnlySpan{byte}, IFormatProvider?)" />
        public static NFloat Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) => Parse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider);

        /// <inheritdoc cref="IUtf8SpanParsable{TSelf}.TryParse(ReadOnlySpan{byte}, IFormatProvider?, out TSelf)" />
        public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, out NFloat result) => TryParse(utf8Text, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
    }
}
