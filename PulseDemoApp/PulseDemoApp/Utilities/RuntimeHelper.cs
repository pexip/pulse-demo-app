// <copyright file="RuntimeHelper.cs" company="Pexip">
// Copyright (c) Pexip. All rights reserved.
// </copyright>

namespace PulseDemoApp.Utilities;

using System.Runtime.InteropServices;

public static class RuntimeHelper
{
    public static bool IsMSIX
    {
        get
        {
            int length = 0;
            return NativeMethods.GetCurrentPackageFullName(ref length, null) != 15700L;
        }
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
        internal static extern int GetCurrentPackageFullName(ref int packageFullNameLength, [Out] char[]? packageFullName);
    }
}
