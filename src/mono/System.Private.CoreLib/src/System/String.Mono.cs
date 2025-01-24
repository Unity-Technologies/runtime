// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System
{
    public partial class String
    {
        public static string Intern(string str)
        {
            ArgumentNullException.ThrowIfNull(str);

            return InternalIntern(str);
        }

        public static string IsInterned(string str)
        {
            ArgumentNullException.ThrowIfNull(str);

            return InternalIsInterned(str);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern string InternalIsInterned(string str);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern string InternalIntern(string str);

        internal int OFFSET_TO_STRING => RuntimeHelpers.TargetIs64Bit ? 20 : 12;

        // TODO: Should be pointing to Buffer instead
        #region Runtime method-to-ir dependencies

        private static unsafe void memset(byte* dest, int val, int len)
        {
            if (len < 8)
            {
                while (len != 0)
                {
                    *dest = (byte)val;
                    ++dest;
                    --len;
                }
                return;
            }

            int word_size = sizeof(void*);

            long word_val_long;
            int word_val_int;
            Unsafe.SkipInit(out word_val_long);
            Unsafe.SkipInit(out word_val_int);

            if (RuntimeHelpers.TargetIs64Bit)
            {
                word_val_long = val;
                if (word_val_long != 0)
                {
                    word_val_long |= (word_val_long << 32);
                }
            }
            else
            {
                word_val_int = val;
                if (word_val_int != 0)
                {
                    word_val_int |= (word_val_int << 8);
                    word_val_int |= (word_val_int << 16);
                }
            }

            // align to word_size
            int rest = (int)dest & (word_size - 1);
            if (rest != 0)
            {
                rest = word_size - rest;
                len -= rest;
                do
                {
                    *dest = (byte)val;
                    ++dest;
                    --rest;
                } while (rest != 0);
            }

            while (len >= 16)
            {
                if (RuntimeHelpers.TargetIs64Bit)
                {
                    ((long*)dest)[0] = word_val_long;
                    ((long*)dest)[1] = word_val_long; ;
                }
                else
                {
                    ((int*)dest)[0] = word_val_int;
                    ((int*)dest)[1] = word_val_int;
                    ((int*)dest)[2] = word_val_int;
                    ((int*)dest)[3] = word_val_int;
                }
                dest += 16;
                len -= 16;
            }
            while (len >= word_size)
            {
                if (RuntimeHelpers.TargetIs64Bit)
                    ((long*)dest)[0] = word_val_long;
                else
                    ((int*)dest)[0] = word_val_int;
                dest += word_size;
                len -= word_size;
            }
            // tail bytes
            while (len > 0)
            {
                *dest = (byte)val;
                dest++;
                len--;
            }
        }

        private static unsafe void memcpy(byte* dest, byte* src, int size)
        {
            Buffer.Memmove(ref *dest, ref *src, (nuint)size);
        }

        /* Used by the runtime */
        internal static unsafe void bzero(byte* dest, int len)
        {
            memset(dest, 0, len);
        }

        internal static unsafe void bzero_aligned_1(byte* dest, int len)
        {
            ((byte*)dest)[0] = 0;
        }

        internal static unsafe void bzero_aligned_2(byte* dest, int len)
        {
            ((short*)dest)[0] = 0;
        }

        internal static unsafe void bzero_aligned_4(byte* dest, int len)
        {
            ((int*)dest)[0] = 0;
        }

        internal static unsafe void bzero_aligned_8(byte* dest, int len)
        {
            ((long*)dest)[0] = 0;
        }

        internal static unsafe void memcpy_aligned_1(byte* dest, byte* src, int size)
        {
            ((byte*)dest)[0] = ((byte*)src)[0];
        }

        internal static unsafe void memcpy_aligned_2(byte* dest, byte* src, int size)
        {
            ((short*)dest)[0] = ((short*)src)[0];
        }

        internal static unsafe void memcpy_aligned_4(byte* dest, byte* src, int size)
        {
            ((int*)dest)[0] = ((int*)src)[0];
        }

        internal static unsafe void memcpy_aligned_8(byte* dest, byte* src, int size)
        {
            ((long*)dest)[0] = ((long*)src)[0];
        }

        #endregion
    }
}
