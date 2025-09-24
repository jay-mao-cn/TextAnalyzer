using System.Collections.Generic;
using System;
using System.Runtime.InteropServices;

namespace TextAnalyzer.Mac
{
    static class MacInterop
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void OpenFilesCallback(int count, IntPtr files);

        [DllImport("libMacOpenFileDelegate.dylib", EntryPoint = "RegisterOpenFilesCallback")]
        public static extern void RegisterOpenFilesCallback(OpenFilesCallback cb);
    }

    class MacOpenFileDelegate
    {
        // Need a member variable to keep the callback alive
        private MacInterop.OpenFilesCallback _callback;

        public MacOpenFileDelegate(Action<string> onFileOpen)
        {
            _callback = (count, filesPtr) =>
            {
                // Marshal native char** -> managed strings
                var fileList = new List<string>();
                var ptrs = new IntPtr[count];
                Marshal.Copy(filesPtr, ptrs, 0, count);

                for (int i = 0; i < count; i++)
                {
                    var file = Marshal.PtrToStringUTF8(ptrs[i]);
                    if (file != null && !file.EndsWith("TextAnalyzer.dll"))
                    {
                        fileList.Add(file);
                    }
                }

                foreach (var f in fileList)
                {
                    Console.WriteLine($"[OpenWith] {f}");
                    onFileOpen?.Invoke(f!);
                }
            };

            MacInterop.RegisterOpenFilesCallback(_callback);
        }
    }
}