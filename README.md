![alt text](https://bitbucket.org/Starnick/assimpnet/raw/64485416c27d84b2928ba375d7ae51c8ab24bdb7/logo.png "AssimpNet Logo")

**The latest release can be downloaded via [NuGet](https://www.nuget.org/packages/AssimpNet/).**

## Introduction ##
This is the official repository for **AssimpNet**, the cross-platform .NET wrapper for the Open Asset Import Library (otherwise known as [Assimp](https://github.com/assimp/assimp)), which is a 3D model import-export library. The primary motivation is for this library to power content pipelines to import and process 3D models into your game engine's internal format, although the wrapper can be used at runtime to enable your users to import custom content.

P/Invoke is used to communicate with the C-API of the native library. The managed assembly is compiled as **AnyCpu** and the native DLLs are loaded dynamically for either 32 or 64 bit applications.

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

AssimpNet officially targets the **.NET Standard 1.3** and supplies binaries for **32/64 bit Windows** and **64 bit Linux (tested on ubuntu)**. The library is able to support **MacOS** but native binaries are not yet bundled with the official NuGet package. To use the library on your
preferred platform, you may have to build and supply the native binaries yourself.

Additionally, the NuGet package has targets for **.NET Framework 4.x** and **.NET Framework 3.5** should you need them. It was compiled with Visual Studio 2017, but it has been compiled on Ubuntu using the DotNet CLI. There is one **build-time only** dependency, an IL Patcher also distributed as a cross-platform NuGet package. As long as you're
able to build with Visual Studio or the DotNet CLI, the library *should* compile without issue on any platform.

## Licensing ##

The library is licensed under the [MIT](https://opensource.org/licenses/MIT) license. This means you're free to modify the source and use the library in whatever way you want, as long as you attribute the original authors. The native library is licensed under the [3-Clause BSD](https://opensource.org/licenses/BSD-3-Clause) license. Please be kind enough to include the licensing text file (it contains both licenses).

## Contact ##

Follow project updates and more on [Twitter](https://twitter.com/Tesla3D/).

In addition, check out these other projects from the same author:

[TeximpNet](https://bitbucket.org/Starnick/teximpnet) - A wrapper for the Nvidia Texture Tools and FreeImage libraries, which is a sister library to this one.

[MemoryInterop.ILPatcher](https://bitbucket.org/Starnick/memoryinterop.ilpatcher) - This is the ILPatcher that is required at build time, it uses Mono.Cecil to inject IL code to improve native interop. The ILPatcher is cross-platform, which enables building of AssimpNet on non-windows platforms.

[Tesla Graphics Engine](https://bitbucket.org/Starnick/tesla3d) - A 3D rendering engine written in C# and the primary driver for developing AssimpNet.