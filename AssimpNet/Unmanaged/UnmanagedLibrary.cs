/*
* Copyright (c) 2012-2018 AssimpNet - Nicholas Woodfield
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in
* all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
* THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Assimp.Unmanaged
{
    /// <summary>
    /// Enumerates supported platforms.
    /// </summary>
    public enum Platform
    {
        /// <summary>
        /// Windows platform.
        /// </summary>
        Windows,

        /// <summary>
        /// Unix platform.
        /// </summary>
        Unix,

        /// <summary>
        /// Mac platform.
        /// </summary>
        Mac
    }

    /// <summary>
    /// An attribute that represents the name of an unmanaged function to import.
    /// </summary>
    [AttributeUsage(AttributeTargets.Delegate)]
    public class UnmanagedFunctionNameAttribute : Attribute
    {
        private String m_unmanagedFunctionName;

        /// <summary>
        /// Name of the unmanaged function.
        /// </summary>
        public String UnmanagedFunctionName
        {
            get
            {
                return m_unmanagedFunctionName;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="UnmanagedFunctionName"/>.
        /// </summary>
        /// <param name="unmanagedFunctionName">Name of the function.</param>
        public UnmanagedFunctionNameAttribute(String unmanagedFunctionName)
        {
            m_unmanagedFunctionName = unmanagedFunctionName;
        }
    }

    /// <summary>
    /// Represents management and access to an unmanaged library. An unmanaged library can be loaded and unloaded dynamically. The library then searches for a list
    /// of exported functions to create managed delegates for, allowing callers to access the library. Each OS platform has its own implementation to determine how to load
    /// unmanaged libraries.
    /// </summary>
    public abstract class UnmanagedLibrary
    {
        private static Object s_defaultLoadSync = new Object();

        private UnmanagedLibraryImplementation m_impl;
        private String m_libraryPath = String.Empty;
        private volatile bool m_checkNeedsLoading = true;

        /// <summary>
        /// Occurs when the unmanaged library is loaded.
        /// </summary>
        public event EventHandler LibraryLoaded;

        /// <summary>
        /// Occurs when the unmanaged library is freed.
        /// </summary>
        public event EventHandler LibraryFreed;

        /// <summary>
        /// Queries if the unmanaged library has been loaded or not.
        /// </summary>
        public bool IsLibraryLoaded
        {
            get
            {
                return m_impl.IsLibraryLoaded;
            }
        }

        /// <summary>
        /// Queries the default path to the 32-bit unmanaged library DLL.
        /// </summary>
        public String DefaultLibraryPath32Bit
        {
            get
            {
                return m_impl.DefaultLibraryPath32Bit;
            }
        }

        /// <summary>
        /// Queries the default path to the 64-bit unmanaged library DLL.
        /// </summary>
        public String DefaultLibraryPath64bit
        {
            get
            {
                return m_impl.DefaultLibraryPath64Bit;
            }
        }

        /// <summary>
        /// Queries the path to the unmanaged library DLL that is currently loaded.
        /// </summary>
        public String LibraryPath
        {
            get
            {
                return m_libraryPath;
            }
        }

        /// <summary>
        /// Queries if the OS is 64-bit, if false then it is 32-bit.
        /// </summary>
        public static bool Is64Bit
        {
            get
            {
                return IntPtr.Size == 8;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="UnmanagedLibrary"/>.
        /// </summary>
        /// <param name="default32BitName">Default name (NOT path) of the 32-bit unmanaged library.</param>
        /// <param name="default64BitName">Default name (NOT path) of the 64-bit unmanaged library.</param>
        /// <param name="unmanagedFunctionDelegateTypes">Delegate types to instantiate and load.</param>
        protected UnmanagedLibrary(String default32BitName, String default64BitName, Type[] unmanagedFunctionDelegateTypes)
        {
            CreateRuntimeImplementation(default32BitName, default64BitName, unmanagedFunctionDelegateTypes);
        }

        /// <summary>
        /// Gets an enum representing the current OS that is application is executing on.
        /// </summary>
        /// <returns>Platform enumeration.</returns>
        public static Platform GetPlatform()
        {
            if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return Platform.Windows;

            if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return Platform.Unix;

            if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Platform.Mac;

            throw new InvalidOperationException("Cannot determine OS-specific implementation.");
        }

        /// <summary>
        /// Loads the unmanaged library using the default library paths based on the OS bitness.
        /// </summary>
        /// <returns>True if the library was found and successfully loaded.</returns>
        public bool LoadLibrary()
        {
            return LoadLibrary((Is64Bit) ? DefaultLibraryPath64bit : DefaultLibraryPath32Bit);
        }

        /// <summary>
        /// Loads the unmanaged library using the supplied 32 and 64 bit paths, the one chosen is based on the OS bitness.
        /// </summary>
        /// <param name="lib32Path">Path to the 32-bit DLL</param>
        /// <param name="lib64Path">Path to the 64-bit DLL</param>
        /// <returns>True if the library was found and successfully loaded.</returns>
        public bool LoadLibrary(String lib32Path, String lib64Path)
        {
            return LoadLibrary((Is64Bit) ? lib64Path : lib32Path);
        }

        /// <summary>
        /// Loads the unmanaged library using the supplied path.
        /// </summary>
        /// <param name="libPath">Path to the unmanaged DLL.</param>
        /// <returns>True if the library was found and successfully loaded.</returns>
        public bool LoadLibrary(String libPath)
        {
            if(IsLibraryLoaded)
            {
                //Ignore repeated calls...but do assert
                System.Diagnostics.Debug.Assert(false, "Library already loaded");
                return true;
            }

            //Automatically append extension if necessary
            if(!Path.HasExtension(libPath))
                libPath = Path.ChangeExtension(libPath, m_impl.DllExtension);

            if(m_impl.LoadLibrary(libPath))
            {
                m_libraryPath = libPath;

                OnLibraryLoaded();

                return true;
            }

            return false;
        }

        /// <summary>
        /// Frees the unmanaged library that is currently loaded.
        /// </summary>
        /// <returns></returns>
        public bool FreeLibrary()
        {
            if(IsLibraryLoaded)
            {
                OnLibraryFreed();

                m_impl.FreeLibrary();
                m_libraryPath = String.Empty;
                m_checkNeedsLoading = true;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a delegate based on the unmanaged function name.
        /// </summary>
        /// <typeparam name="T">Type of delegate.</typeparam>
        /// <param name="funcName">Name of unmanaged function that is exported by the library.</param>
        /// <returns>The delegate, or null if not found.</returns>
        public T GetFunction<T>(String funcName) where T : class
        {
            return m_impl.GetFunction<T>(funcName);
        }

        /// <summary>
        /// If library is not explicitly loaded by user, call this when trying to call an unmanaged function to load the unmanaged library
        /// from the default path. This function is thread safe.
        /// </summary>
        protected void LoadIfNotLoaded()
        {
            //Check the loading flag so we don't have to lock every time we want to talk to the native library...
            if(!m_checkNeedsLoading)
                return;

            lock(s_defaultLoadSync)
            {
                if(!IsLibraryLoaded)
                    LoadLibrary();

                m_checkNeedsLoading = false;
            }
        }

        /// <summary>
        /// Called when the library is loaded.
        /// </summary>
        protected virtual void OnLibraryLoaded()
        {
            EventHandler evt = LibraryLoaded;

            if(evt != null)
                evt(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when the library is freed.
        /// </summary>
        protected virtual void OnLibraryFreed()
        {
            EventHandler evt = LibraryFreed;

            if(evt != null)
                evt(this, EventArgs.Empty);
        }

        private void CreateRuntimeImplementation(String default32BitName, String default64BitName, Type[] unmanagedFunctionDelegateTypes)
        {
            switch(GetPlatform())
            {
                case Platform.Windows:
                    m_impl = new UnmanagedWindowsLibraryImplementation(default32BitName, default64BitName, unmanagedFunctionDelegateTypes);
                    break;
                case Platform.Unix:
                    m_impl = new UnmanagedLinuxLibraryImplementation(default32BitName, default64BitName, unmanagedFunctionDelegateTypes);
                    break;
                case Platform.Mac:
                    m_impl = new UnmanagedMacLibraryImplementation(default32BitName, default64BitName, unmanagedFunctionDelegateTypes);
                    break;
                default:
                    throw new PlatformNotSupportedException();
            }
        }

        #region Base Implementation

        internal abstract class UnmanagedLibraryImplementation : IDisposable
        {
            private String m_default32Path;
            private String m_default64Path;
            private Type[] m_unmanagedFunctionDelegateTypes;
            private Dictionary<String, Delegate> m_nameToUnmanagedFunction;
            private IntPtr m_libraryHandle;
            private bool m_isDisposed;

            public bool IsLibraryLoaded
            {
                get
                {
                    return m_libraryHandle != IntPtr.Zero;
                }
            }

            public bool IsDisposed
            {
                get
                {
                    return m_isDisposed;
                }
            }

            public String DefaultLibraryPath32Bit
            {
                get
                {
                    return m_default32Path;
                }
            }

            public String DefaultLibraryPath64Bit
            {
                get
                {
                    return m_default64Path;
                }
            }

            public abstract String DllExtension { get; }

            public virtual String DllPrefix { get { return String.Empty; } }

            public UnmanagedLibraryImplementation(String default32BitName, String default64BitName, Type[] unmanagedFunctionDelegateTypes)
            {
                default32BitName = DllPrefix + Path.ChangeExtension(default32BitName, DllExtension);
                default64BitName = DllPrefix + Path.ChangeExtension(default64BitName, DllExtension);

                //Resolve paths, find TeximpNet.dll. Default path is in the same directory
                String managedAssemblyPath = PlatformHelper.GetAppBaseDirectory();

                m_default32Path = Path.Combine(managedAssemblyPath, default32BitName);
                m_default64Path = Path.Combine(managedAssemblyPath, default64BitName);

                m_unmanagedFunctionDelegateTypes = unmanagedFunctionDelegateTypes;

                m_nameToUnmanagedFunction = new Dictionary<String, Delegate>();
                m_isDisposed = false;
                m_libraryHandle = IntPtr.Zero;
            }

            ~UnmanagedLibraryImplementation()
            {
                Dispose(false);
            }

            public T GetFunction<T>(String functionName) where T : class
            {
                if(String.IsNullOrEmpty(functionName))
                    return null;

                Delegate function;
                if(!m_nameToUnmanagedFunction.TryGetValue(functionName, out function))
                    return null;

                Object obj = (Object) function;

                return (T) obj;
            }

            public bool LoadLibrary(String path)
            {
                FreeLibrary(true);

                m_libraryHandle = NativeLoadLibrary(path);

                if(m_libraryHandle != IntPtr.Zero)
                    LoadFunctions();

                return m_libraryHandle != IntPtr.Zero;
            }

            public bool FreeLibrary()
            {
                return FreeLibrary(true);
            }

            private bool FreeLibrary(bool clearFunctions)
            {
                if(m_libraryHandle != IntPtr.Zero)
                {
                    NativeFreeLibrary(m_libraryHandle);
                    m_libraryHandle = IntPtr.Zero;

                    if(clearFunctions)
                        m_nameToUnmanagedFunction.Clear();

                    return true;
                }

                return false;
            }

            private void LoadFunctions()
            {
                foreach(Type funcType in m_unmanagedFunctionDelegateTypes)
                {
                    String funcName = GetUnmanagedName(funcType);
                    if(String.IsNullOrEmpty(funcName))
                    {
                        System.Diagnostics.Debug.Assert(false, String.Format("No UnmanagedFunctionNameAttribute on {0} type.", funcType.AssemblyQualifiedName));
                        continue;
                    }

                    IntPtr procAddr = NativeGetProcAddress(m_libraryHandle, funcName);
                    if(procAddr == IntPtr.Zero)
                    {
                        System.Diagnostics.Debug.Assert(false, String.Format("No unmanaged function found for {0} type.", funcType.AssemblyQualifiedName));
                        continue;
                    }

                    Delegate function;
                    if(!m_nameToUnmanagedFunction.TryGetValue(funcName, out function))
                    {
                        function = PlatformHelper.GetDelegateForFunctionPointer(procAddr, funcType);
                        m_nameToUnmanagedFunction.Add(funcName, function);
                    }
                }
            }

            private String GetUnmanagedName(Type funcType)
            {
                object[] attributes = PlatformHelper.GetCustomAttributes(funcType, typeof(UnmanagedFunctionNameAttribute), false);
                foreach(object attr in attributes)
                {
                    if(attr is UnmanagedFunctionNameAttribute)
                        return (attr as UnmanagedFunctionNameAttribute).UnmanagedFunctionName;
                }

                return null;
            }

            protected abstract IntPtr NativeLoadLibrary(String path);
            protected abstract void NativeFreeLibrary(IntPtr handle);
            protected abstract IntPtr NativeGetProcAddress(IntPtr handle, String functionName);

            public void Dispose()
            {
                Dispose(true);

                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool isDisposing)
            {
                if(!m_isDisposed)
                {
                    FreeLibrary(isDisposing);

                    m_isDisposed = true;
                }
            }
        }

        #endregion

        #region Windows Implementation

        internal sealed class UnmanagedWindowsLibraryImplementation : UnmanagedLibraryImplementation
        {
            public override String DllExtension
            {
                get
                {
                    return ".dll";
                }
            }

            public UnmanagedWindowsLibraryImplementation(String default32BitName, String default64BitName, Type[] unmanagedFunctionDelegateTypes)
                : base(default32BitName, default64BitName, unmanagedFunctionDelegateTypes)
            {
            }

            protected override IntPtr NativeLoadLibrary(String path)
            {
                IntPtr libraryHandle = WinLoadLibrary(path);

                if(libraryHandle == IntPtr.Zero)
                {
                    int hr = Marshal.GetHRForLastWin32Error();
                    Exception innerException = Marshal.GetExceptionForHR(hr);

                    if(innerException != null)
                        throw new AssimpException(String.Format("Error loading unmanaged library from path: {0}\n\n{1}", path, innerException.Message), innerException);
                    else
                        throw new AssimpException(String.Format("Error loading unmanaged library from path: {0}", path));
                }

                return libraryHandle;
            }

            protected override IntPtr NativeGetProcAddress(IntPtr handle, String functionName)
            {
                return GetProcAddress(handle, functionName);
            }

            protected override void NativeFreeLibrary(IntPtr handle)
            {
                FreeLibrary(handle);
            }

            #region Native Methods

            [DllImport("kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, SetLastError = true, EntryPoint = "LoadLibrary")]
            private static extern IntPtr WinLoadLibrary(String fileName);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool FreeLibrary(IntPtr hModule);

            [DllImport("kernel32.dll")]
            private static extern IntPtr GetProcAddress(IntPtr hModule, String procName);

            #endregion
        }

        #endregion

        #region Linux Implementation

        internal sealed class UnmanagedLinuxLibraryImplementation : UnmanagedLibraryImplementation
        {
            public override String DllExtension
            {
                get
                {
                    return ".so";
                }
            }

            public override String DllPrefix
            {
                get
                {
                    return "lib";
                }
            }

            public UnmanagedLinuxLibraryImplementation(String default32BitName, String default64BitName, Type[] unmanagedFunctionDelegateTypes)
                : base(default32BitName, default64BitName, unmanagedFunctionDelegateTypes)
            {
            }

            protected override IntPtr NativeLoadLibrary(String path)
            {
                IntPtr libraryHandle = dlopen(path, RTLD_NOW);

                if(libraryHandle == IntPtr.Zero)
                {
                    IntPtr errPtr = dlerror();
                    String msg = Marshal.PtrToStringAnsi(errPtr);
                    if(!String.IsNullOrEmpty(msg))
                        throw new AssimpException(String.Format("Error loading unmanaged library from path: {0}\n\n{1}", path, msg));
                    else
                        throw new AssimpException(String.Format("Error loading unmanaged library from path: {0}", path));
                }

                return libraryHandle;
            }

            protected override IntPtr NativeGetProcAddress(IntPtr handle, String functionName)
            {
                return dlsym(handle, functionName);
            }

            protected override void NativeFreeLibrary(IntPtr handle)
            {
                dlclose(handle);
            }

            #region Native Methods

            [DllImport("libdl.so")]
            private static extern IntPtr dlopen(String fileName, int flags);

            [DllImport("libdl.so")]
            private static extern IntPtr dlsym(IntPtr handle, String functionName);

            [DllImport("libdl.so")]
            private static extern int dlclose(IntPtr handle);

            [DllImport("libdl.so")]
            private static extern IntPtr dlerror();

            private const int RTLD_NOW = 2;

            #endregion
        }

        #endregion

        #region Mac Implementation

        internal sealed class UnmanagedMacLibraryImplementation : UnmanagedLibraryImplementation
        {
            public override String DllExtension
            {
                get
                {
                    return ".dylib";
                }
            }

            public override String DllPrefix
            {
                get
                {
                    return "lib";
                }
            }

            public UnmanagedMacLibraryImplementation(String default32BitName, String default64BitName, Type[] unmanagedFunctionDelegateTypes)
                : base(default32BitName, default64BitName, unmanagedFunctionDelegateTypes)
            {
            }

            protected override IntPtr NativeLoadLibrary(String path)
            {
                IntPtr libraryHandle = dlopen(path, RTLD_NOW);

                if(libraryHandle == IntPtr.Zero)
                {
                    IntPtr errPtr = dlerror();
                    String msg = Marshal.PtrToStringAnsi(errPtr);
                    if(!String.IsNullOrEmpty(msg))
                        throw new AssimpException(String.Format("Error loading unmanaged library from path: {0}\n\n{1}", path, msg));
                    else
                        throw new AssimpException(String.Format("Error loading unmanaged library from path: {0}", path));
                }

                return libraryHandle;
            }

            protected override IntPtr NativeGetProcAddress(IntPtr handle, String functionName)
            {
                return dlsym(handle, functionName);
            }

            protected override void NativeFreeLibrary(IntPtr handle)
            {
                dlclose(handle);
            }

            #region Native Methods

            [DllImport("libSystem.B.dylib")]
            private static extern IntPtr dlopen(String fileName, int flags);

            [DllImport("libSystem.B.dylib")]
            private static extern IntPtr dlsym(IntPtr handle, String functionName);

            [DllImport("libSystem.B.dylib")]
            private static extern int dlclose(IntPtr handle);

            [DllImport("libSystem.B.dylib")]
            private static extern IntPtr dlerror();

            private const int RTLD_NOW = 2;

            #endregion
        }

        #endregion
    }
}
