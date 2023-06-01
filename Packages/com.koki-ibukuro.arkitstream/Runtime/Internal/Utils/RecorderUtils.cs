using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ARKitStream
{
    internal static class RecorderUtils
    {

#if !UNITY_EDITOR && (UNITY_IOS || UNITY_WEBGL)
        const string _dll = "__Internal";
#else
        const string _dll = "write_jpeg";
#endif

        [DllImport(_dll, EntryPoint = "write_jpeg_data")]
        static extern internal void WriteJPEGData(byte[] data, int size, int width, int height, string path);

#if UNITY_EDITOR
        [DllImport(_dll, EntryPoint = "decode_jpeg")]
        static extern IntPtr DecodeJPEG(byte[] data, int size, out int width, out int height, out int dstSize, IntPtr buffer);
#endif

        static IntPtr _ptr;

        static internal Texture2D LoadJPEGData(Texture2D texture, byte[] data)
        {
#if UNITY_EDITOR
            IntPtr ptr = RecorderUtils.DecodeJPEG(data, data.Length, out var w, out var h, out var size, _ptr);

            if(size != -1)
            {
                if(texture.width != w || texture.height != h)
                    texture = new Texture2D(w, h, TextureFormat.RGB24, false);

                texture.LoadRawTextureData(ptr, size);
                texture.Apply();
            }
#endif
            return texture;
        }

        static RecorderUtils()
        {
            _ptr = Marshal.AllocCoTaskMem(1440*1920*3);
        }
    }

    internal static class NativeCollectionUnsafeExtentions
    {
        internal static unsafe IntPtr GetPtr(this NativeArray<byte> list)
            => (IntPtr)list.GetUnsafeReadOnlyPtr();
    }
}
