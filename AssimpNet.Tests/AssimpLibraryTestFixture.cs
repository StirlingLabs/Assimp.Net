/*
* Copyright (c) 2023 AssimpNet - Stirling Labs Pty Ltd
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
using System.IO;
using System.Threading;
using System.Collections.Generic;
using Assimp.Configs;
using Assimp.Unmanaged;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class AssimpLibraryTestFixture
    {

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestMultiThreaded()
        {
            Assert.That(AssimpLibrary.Instance.IsMultithreadingSupported, Is.True);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void DisplayCompileFlags()
        {
            var assimpInstance = AssimpLibrary.Instance;
            var flags = assimpInstance.GetCompileFlags();
            if (flags.HasFlag(CompileFlags.SingleThreaded))
            {
                Console.WriteLine("Built as SingleThreaded");
                Assert.That(assimpInstance.IsMultithreadingSupported, Is.False);
            }
            if (flags.HasFlag(CompileFlags.Shared))
            {
                Console.WriteLine("Built as Shared");
            }
            if (flags.HasFlag(CompileFlags.Debug))
            {
                Console.WriteLine("Built as Debug");
            }
            if (flags.HasFlag(CompileFlags.NoBoost))
            {
                Console.WriteLine("Built without Boost");
                Assert.That(assimpInstance.IsMultithreadingSupported, Is.False);
            }
            if (flags.HasFlag(CompileFlags.STLport))
            {
                Console.WriteLine("Built with STLport");
            }
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void DisplayVersion()
        {
            var assimpInstance = AssimpLibrary.Instance;
            Console.WriteLine($"version: {assimpInstance.GetVersion()}");
            Console.WriteLine($"branch: {assimpInstance.GetBranchName()}\n");
            Console.WriteLine(assimpInstance.GetLegalString());
        }

    }
}
