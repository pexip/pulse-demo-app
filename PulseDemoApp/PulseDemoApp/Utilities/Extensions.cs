// <copyright file="Extensions.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

using System.Runtime.InteropServices;
using System.Text;

namespace PulseDemoApp.Utilities;

public static class Extensions
{
    internal static string ToString(this nint value, Encoding encoding)
    {
        if (encoding == Encoding.ASCII)
        {
            return Marshal.PtrToStringAnsi(value)!;
        }

        if (encoding == Encoding.UTF8)
        {
            return Marshal.PtrToStringUTF8(value)!;
        }

        if (encoding == Encoding.Unicode)
        {
            return Marshal.PtrToStringUni(value)!;
        }

        return Marshal.PtrToStringAuto(value)!;
    }

    internal static T[] ToStructs<T>(this nint value, int size, bool byRef = true)
        where T : struct
    {
        // T*[] case (array of pointers to structs: T* items[] = { &item1, &item2, &item3 };)
        if (byRef)
        {
            var structPtrs = new nint[size];
            if (value != nint.Zero)
            {
                Marshal.Copy(value, structPtrs, 0, size);
            }

            return structPtrs
                .Select(x => x.ToStruct<T>())
                .ToArray();
        }

        // T[] case (contiguous block of T structs: T items[3] = { item1, item2, item3 };)
        else
        {
            var structs = new T[size];
            nint currentStruct = value;
            for (int i = 0; i < size; i++)
            {
                structs[i] = currentStruct.ToStruct<T>();
                currentStruct += Marshal.SizeOf<T>();
            }

            return structs;
        }
    }

    internal static T ToStruct<T>(this nint value)
        where T : struct
    {
        return Marshal.PtrToStructure<T>(value)!;
    }
}
