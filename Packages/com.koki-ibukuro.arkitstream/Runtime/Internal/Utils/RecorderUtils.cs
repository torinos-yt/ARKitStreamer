using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ARKitStream
{
    internal class RecorderUtils
    {

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
        const string _dll = "__Internal";
#else
        const string _dll = "write_jpeg";
#endif

        [DllImport(_dll, EntryPoint = "write_jpeg_data")]
        static extern internal void WriteJPEGData(byte[] data, int size, int width, int height, string path);
    }

    internal static class NativeCollectionUnsafeExtentions
    {
        internal static unsafe IntPtr GetPtr(this NativeArray<byte> list)
            => (IntPtr)list.GetUnsafeReadOnlyPtr();
    }
}
