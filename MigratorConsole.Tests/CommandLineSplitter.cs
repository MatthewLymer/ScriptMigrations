﻿using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MigratorConsole.Tests
{
    internal static class CommandLineSplitter
    {
        [DllImport("shell32.dll", SetLastError = true)]
        static private extern IntPtr CommandLineToArgvW([MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);

            if (argv == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            try
            {
                var args = new string[argc];

                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }
    }
}
