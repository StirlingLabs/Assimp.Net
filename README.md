**The latest release can be downloaded from the [Downloads](https://bitbucket.org/Starnick/assimpnet/downloads) section or via [NuGet](https://www.nuget.org/packages/AssimpNet/).**

## Introduction ##
This is the official repository for **AssimpNet**, the cross-platform .NET wrapper for the Open Asset Import Library (otherwise known as [Assimp](https://github.com/assimp/assimp) - it's German). This wrapper leverages P/Invoke to communicate with the native library's C-API. Since the managed assembly is compiled as **AnyCpu** and the native DLLs are loaded dynamically, the library fully supports usage with 32 and 64 bit applications without needing to be recompiled.

The library is split between two parts, a low level and a high level. The intent is to give as much freedom as possible to the developer to work with the native library from managed code.

### Low level ###

* Native methods are exposed via the AssimpLibrary singleton.
* Structures corresponding to unmanaged structures are prefixed with the name **Ai** and generally contain IntPtrs to the unmanaged data.
* Located in the *Assimp.Unmanaged* namespace.

### High level ###

* Replicates the native library's C++ API, but in a way that is more familiar to C# developers.
* Marshaling to and from managed memory handled automatically, all you need to worry about is processing your data.
* Located in the *Assimp* namespace.

## Supported Platforms ##

The NuGet package is the only official release of the binaries and currently it only supports **Windows**. Both 32 and 64 bit are supported with a managed DLL compiled for .NET Framework 2.0 and 4.5. The current release (3.3.2) targets the native **Assimp 3.1.1** release. The library supports other platforms unofficially. It has a **Linux** and **Mac** implementation to load and communicate with the native library for those platforms, but you have to provide the native binary yourself. Users have also successfully used the library with Unity3D and Android, but your mileage may vary.

The library is compiled using Visual Studio 2015 and at runtime has no other external dependencies other than the native library. However, there is a compile time dependency using [Mono.Cecil](https://github.com/jbevain/cecil/). If you compile without using the VS projects/MSBuild environment, the **only** special instruction is that you need to ensure that the interop generator patches the AssimpNet.dll in a post-build process, otherwise the library won't function correctly. This is because Mono.Cecil is used to inject IL into the assembly to make interoping with the native library more efficient.

## Licensing ##

The library is licensed under the [MIT](https://opensource.org/licenses/MIT) license. This means you're free to modify the source and use the library in whatever way you want, as long as you attribute the original authors. The native library is licensed under the [3-Clause BSD](https://opensource.org/licenses/BSD-3-Clause) license. Please be kind enough to include the licensing text file (it contains both licenses).

## Contact ##

Follow project updates and more on [Twitter](https://twitter.com/Tesla3D/).

In addition, check out these other projects from the same author:

[TeximpNet](https://bitbucket.org/Starnick/teximpnet) - A wrapper for the Nvidia Texture Tools and FreeImage libraries, which is a sister library to this one.

[Tesla Graphics Engine](https://bitbucket.org/Starnick/tesla3d) - A 3D rendering engine written in C# and the primary driver for developing AssimpNet.