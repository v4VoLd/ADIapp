using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Platform;

namespace ADIapp.Helpers;

public static class MacDockHelper
{
    private const string ObjCLib = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    private static extern IntPtr msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    private static extern IntPtr msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    private static extern IntPtr msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLib)]
    private static extern IntPtr sel_registerName(string name);

    public static void SetDockIcon()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return;

        try
        {
            byte[] bytes;
            using (var stream = AssetLoader.Open(new Uri("avares://ADIapp/Assets/app_dock_logo.png")))
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                bytes = ms.ToArray();
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                IntPtr bytesPtr = handle.AddrOfPinnedObject();
                IntPtr lengthPtr = new IntPtr(bytes.Length);

                IntPtr clsNSData = objc_getClass("NSData");
                IntPtr selDataWithBytesLength = sel_registerName("dataWithBytes:length:");
                IntPtr nsData = msgSend(clsNSData, selDataWithBytesLength, bytesPtr, lengthPtr);

                if (nsData == IntPtr.Zero) return;

                IntPtr clsNSImage = objc_getClass("NSImage");
                IntPtr selAlloc = sel_registerName("alloc");
                IntPtr selInitWithData = sel_registerName("initWithData:");

                IntPtr allocatedImage = msgSend(clsNSImage, selAlloc);
                IntPtr nsImage = msgSend(allocatedImage, selInitWithData, nsData);

                if (nsImage == IntPtr.Zero) return;

                IntPtr clsNSApplication = objc_getClass("NSApplication");
                IntPtr selSharedApplication = sel_registerName("sharedApplication");
                IntPtr nsApp = msgSend(clsNSApplication, selSharedApplication);

                if (nsApp == IntPtr.Zero) return;

                IntPtr selSetApplicationIconImage = sel_registerName("setApplicationIconImage:");
                msgSend(nsApp, selSetApplicationIconImage, nsImage);
            }
            finally
            {
                handle.Free();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set macOS Dock Icon: {ex.Message}");
        }
    }
}
