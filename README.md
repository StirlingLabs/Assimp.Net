![Assimp.Net from Stirling Labs](https://raw.githubusercontent.com/StirlingLabs/Assimp.Net/main/Assimp.Net.jpg)

[![CD](https://github.com/StirlingLabs/Assimp.Net/actions/workflows/deploy.yaml/badge.svg)](https://github.com/StirlingLabs/Assimp.Net/actions/workflows/deploy.yaml) [![NuGet version (StirlingLabs.Assimp.Net)](https://img.shields.io/nuget/v/StirlingLabs.Assimp.Net.svg?style=flat-square)](https://www.nuget.org/packages/StirlingLabs.Assimp.Net/)

## Introduction ##
This is a fork of [assimpnet by Starnick](https://bitbucket.org/Starnick/assimpnet), a cross-platform .NET wrapper for the Open Asset Import Library (otherwise known as [Assimp](https://github.com/StirlingLabs/assimp)), which is a 3D model import-export library. The primary motivation is for this library to power content pipelines to import and process 3D models. Please see the Assimp website for a full list of supported formats and features. Each version of the managed wrapper tries to maintain parity with the features of the native version.

We intend to keep this fork close to the original, with the main differences being a focus on supporting modern .Net, Apple Silicon and a open, publicly-verifiable build chain.

The managed assembly is compiled as **AnyCpu** and the native 64-bit multi-threaded binaries are loaded dynamically (including support for Apple Silicon).

The library is split between two parts, a low level and a high level. The intent is to give as much freedom as possible to the developer to work with the native library from managed code.

### Low level ###

* Native methods are exposed via the AssimpLibrary singleton.
* Structures corresponding to unmanaged structures are prefixed with the name **Ai** and generally contain IntPtrs to the unmanaged data.
* Located in the *Assimp.Unmanaged* namespace.

### High level ###

* Replicates the native library's C++ API, but in a way that is more familiar to C# developers.
* Marshaling to and from managed memory handled automatically, all you need to worry about is processing your data.
* Located in the *Assimp* namespace.

## Supported Frameworks ##

This version of the library is focussed on modern .NET, targeting specifically:

* **.NET Standard 2.1**
* **.NET 6**
* **.NET 7**

The native binaries are resolved by the NuGet dependency graph automatically and are built & packaged in a closely-following fork of [assimp](https://github.com/StirlingLabs/assimp).

## Supported Platforms ##

The NuGet package supports the following Operating Systems and Architectures:

* Windows x64
* Linux x64
* MacOS x64 & Apple Silicon (M1)

## Unity Users ##

A Unity plugin is inherited from Starnick but this is not our focus so we recommend that you use the original.

## Licensing ##

The library is licensed under the [MIT](https://opensource.org/licenses/MIT) license. This means you're free to modify the source and use the library in whatever way you want, as long as you attribute the original authors. The native library is licensed under the [3-Clause BSD](https://opensource.org/licenses/BSD-3-Clause) license. Please be kind enough to include the licensing text file (it contains both licenses).
