/*
* Copyright (c) 2012-2020 AssimpNet - Nicholas Woodfield
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
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class IoSystemTestFixture
    {
        [Test]
        public void TestMultiSearchDirectoryLoad()
        {
            const string fileName = "fenris.lws";
            var searchPaths = new []{ Path.Combine(TestHelper.RootPath, "TestFiles/fenris/scenes"), Path.Combine(TestHelper.RootPath, "TestFiles/fenris/objects") };
            var ioSystem = new FileIOSystem(searchPaths);

            var context = new AssimpContext();
            context.SetIOSystem(ioSystem);

            //None, using the "target high quality flags caused a crash with this model.
            var scene = context.ImportFile(fileName, PostProcessSteps.None);
            Assert.Multiple(() =>
            {
                Assert.That(scene, Is.Not.Null);
                Assert.That(scene.RootNode, Is.Not.Null);
                Assert.That(scene.HasMeshes, Is.True);
            });
        }
        
        [Test]
        public void TestMultiSearchDirectoryLoadWrite()
        {
            const string fileName = "fenris.lws";
            var searchPaths = new []{ Path.Combine(TestHelper.RootPath, "TestFiles/fenris/scenes"), Path.Combine(TestHelper.RootPath, "TestFiles/fenris/objects") };
            var outputFilename = Path.Combine(TestHelper.RootPath, "TestFiles/output/fenris.obj");
            var ioSystem = new FileIOSystem(searchPaths);

            var context = new AssimpContext();
            context.SetIOSystem(ioSystem);

            //None, using the "target high quality flags caused a crash with this model.
            var scene = context.ImportFile(fileName, PostProcessSteps.None);
            Assert.Multiple(() =>
            {
                Assert.That(scene, Is.Not.Null);
                Assert.That(scene.RootNode, Is.Not.Null);
                Assert.That(scene.HasMeshes, Is.True);
            });
            context.ExportFile(scene, outputFilename, "obj", PostProcessSteps.None);
            Assert.That(File.Exists(outputFilename), Is.True);
            var fileInfo = new FileInfo(outputFilename);
            Assert.That(fileInfo.Length, Is.GreaterThan(0));
        }

        [Test]
        public void TestMultiSearchDirectoryConvertToFile()
        {
            var fileName = Path.Combine(TestHelper.RootPath, "TestFiles/fenris/scenes/fenris.lws");
            var searchPath = Path.Combine(TestHelper.RootPath, "TestFiles/fenris/objects");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/fenris.assbin");

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            var ioSystem = new FileIOSystem(new []{searchPath, Path.GetDirectoryName(fileName), Path.GetDirectoryName(outputPath)});

            var context = new AssimpContext();
            var log = new TestContextLogStream();
            log.Attach();
            LogStream.IsVerboseLoggingEnabled = true;
            context.SetIOSystem(ioSystem);
            var exportFormatId = GetExportFormatId(outputPath, context);
            Assert.That(exportFormatId, Is.Not.Null);
            
            context.ConvertFromFileToFile(fileName, outputPath, exportFormatId);
            Assert.That(File.Exists(outputPath), Is.True);
            var fileInfo = new FileInfo(outputPath);
            Assert.That(fileInfo.Length, Is.GreaterThan(0));
            log.Detach();
        }
        
        [Test]
        public void TestMultiSearchDirectoryConvertToBlob()
        {
            var fileName = Path.Combine(TestHelper.RootPath, "TestFiles/fenris/scenes/fenris.lws");
            var searchPath = Path.Combine(TestHelper.RootPath, "TestFiles/fenris/objects");

            var ioSystem = new FileIOSystem(new []{searchPath, Path.GetDirectoryName(fileName)});

            var context = new AssimpContext();
            var log = new TestContextLogStream();
            log.Attach();
            LogStream.IsVerboseLoggingEnabled = true;
            context.SetIOSystem(ioSystem);
            const string formatId = "assbin";
            Assert.That(formatId, Is.Not.Null);
            
            var blob = context.ConvertFromFileToBlob(fileName, formatId);

            var ms = new MemoryStream(blob.Data);
            var scene = context.ImportFileFromStream(ms, PostProcessSteps.None, $".{formatId}");
            ms.Close();
            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.MeshCount, Is.GreaterThan(0));

            log.Detach();
        }
        
        private string GetExportFormatId(string filename, AssimpContext context)
        {
            var extension = Path.GetExtension(filename);
            if (String.IsNullOrEmpty(extension))
                return null;

            extension = extension.Substring(1);
            var formats = context.GetSupportedExportFormats();
            foreach (var format in formats)
            {
                if (format.FileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                    return format.FormatId;
            }

            return null;
        }

        [Test]
        public void TestIoSystemError()
        {
            const string fileName = "duckduck.dae"; //GOOSE!
            var searchPaths = new[]{ Path.Combine(TestHelper.RootPath, "TestFiles") };
            var ioSystem = new FileIOSystem(searchPaths);

            var context = new AssimpContext();
            context.SetIOSystem(ioSystem);
            Assert.Throws<AssimpException>(delegate()
            {
                context.ImportFile(fileName, PostProcessSteps.None);
            });
        }

        [Test]
        public void TestIOSystem_ImportObj()
        {
            var dir = Path.Combine(TestHelper.RootPath, "TestFiles");
            var logStream = new TestContextLogStream();
            logStream.Attach();
            LogStream.IsVerboseLoggingEnabled = true;

            using(var context = new AssimpContext())
            {
                var iOSystem = new FileIOSystem(dir);
                context.SetIOSystem(iOSystem);

                //Using stream does not use the IO system...
                using(Stream fs = File.OpenRead(Path.Combine(dir, "sphere.obj")))
                {
                    var scene = context.ImportFileFromStream(fs, "obj");
                    Assert.That(scene, Is.Not.Null);
                    Assert.That(scene.HasMeshes, Is.True);
                    Assert.That(scene.HasMaterials, Is.True);

                    //No material file, so the mesh will always use the default material
                    Assert.That(scene.Materials[scene.Meshes[0].MaterialIndex].Name, Is.EqualTo("DefaultMaterial"));
                }

                //Using custom IO system requires us to pass in the file name, assimp will ask the io system to get a stream
                var scene2 = context.ImportFile("sphere.obj");
                Assert.Multiple(() =>
                {
                    Assert.That(scene2, Is.Not.Null);
                    Assert.That(scene2.HasMeshes, Is.True);
                    Assert.That(scene2.HasMaterials, Is.True);

                    //Should have found a material with the name "SphereMaterial" in the mtl file
                    Assert.That(scene2.Materials[scene2.Meshes[0].MaterialIndex].Name == "SphereMaterial", Is.True);
                });
            }
        }
    }
}
