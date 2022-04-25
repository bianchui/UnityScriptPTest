using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class FFIClass : IDisposable {
#if (UNITY_IPHONE || UNITY_TVOS || UNITY_WEBGL || UNITY_SWITCH) && !UNITY_EDITOR
    const string LUADLL = "__Internal";
#else
    const string LUADLL = "xlua";
#endif

    [StructLayout(LayoutKind.Sequential)]
    unsafe struct STestFFI {
        public Vector3* vectorArray;
        public int vectorCount;
    }

    [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr test_ffi_init();

    [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe Vector3* test_ffi_create(IntPtr handle, int size);

    [DllImport(LUADLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void test_ffi_close(IntPtr ptr);

    private IntPtr _handle;
    public FFIClass() {
        Debug.Log("FFIClass()");
        _handle = test_ffi_init();
    }

    public unsafe Vector3* getArray(int size) {
        return test_ffi_create(_handle, size);
    }

    public IntPtr handle() {
        return _handle;
    }

    ~FFIClass() {
        Debug.Log("~FFIClass()");
    }

    public void Dispose() {
        Destroy();
    }

    private void Destroy() {
        if (_handle != IntPtr.Zero) {
            test_ffi_close(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
