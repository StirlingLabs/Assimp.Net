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
using System.Threading;
using System.Collections.Generic;
using Assimp.Configs;
using Assimp.Unmanaged;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class AssimpContextTestFixture
    {

        [OneTimeSetUp]
        public void Setup()
        {
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            IEnumerable<String> filePaths = Directory.GetFiles(outputPath);

            foreach(var filePath in filePaths)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportBadFormatId()
        {
            var context = new AssimpContext();
            var config = new NormalSmoothingAngleConfig(66.0f);
            context.SetConfig(config);

            // This is how you would use the log stream if writing to Sentry or some other logging service
            var logStream = new LogStream(delegate (string msg, string userData)
            {
                Console.Write(msg); // Note that the newline is already included in the message
            }, "TestExportBadFormatId");
            logStream.Attach();

            var collada = context.ImportFile(Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae"));

            var success = context.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "dae");
            Assert.IsFalse(success);

            success = context.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "collada");
            Assert.IsTrue(success);

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportToBlob()
        {
            var colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            var context = new AssimpContext();
            var ducky = context.ImportFile(colladaPath);
            Assert.IsNotNull(ducky);
            
            var blob = context.ExportToBlob(ducky, "obj");
            Assert.IsTrue(blob.HasData);
            Assert.IsTrue(blob.NextBlob != null);
            Assert.IsTrue(blob.NextBlob.Name.Equals("mtl"));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportExportFile()
        {
            var colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.ply");

            var context = new AssimpContext();
            var ducky = context.ImportFile(colladaPath);
            var success = context.ExportFile(ducky, plyPath, "ply");
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(plyPath));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportExportImportFile()
        {
            var colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck2.dae");

            var context = new AssimpContext();
            var ducky = context.ImportFile(colladaPath);
            var success = context.ExportFile(ducky, plyPath, "collada");
            Assert.IsTrue(success);
            
            var ducky2 = context.ImportFile(plyPath);
            Assert.IsNotNull(ducky2);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportToFile()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/ExportedTriangle.obj");
            var logStream = new TestContextLogStream();
            logStream.Attach();

            //Create a very simple scene a single node with a mesh that has a single face, a triangle and a default material
            var scene = new Scene();
            scene.RootNode = new Node("Root");

            var mesh = new Mesh("", PrimitiveType.Triangle);
            mesh.Vertices.Add(new Vector3D(1, 0, 0));
            mesh.Vertices.Add(new Vector3D(5, 5, 0));
            mesh.Vertices.Add(new Vector3D(10, 0, 0));
            mesh.Faces.Add(new Face(new int[] { 0, 1, 2 }));
            mesh.MaterialIndex = 0;

            scene.Meshes.Add(mesh);
            scene.RootNode.MeshIndices.Add(0);

            var mat = new Material();
            mat.Name = "MyMaterial";
            scene.Materials.Add(mat);

            //Export the scene then read it in and compare!

            var context = new AssimpContext();
            var success = context.ExportFile(scene, path, "obj");
            Assert.IsTrue(success);
            
            var importedScene = context.ImportFile(path);
            Assert.IsTrue(importedScene.MeshCount == scene.MeshCount);
            Assert.IsTrue(importedScene.MaterialCount == 2); //Always has the default material, should also have our material

            //Compare the meshes
            var importedMesh = importedScene.Meshes[0];
            Assert.IsTrue(importedMesh.VertexCount == mesh.VertexCount);
            for(var i = 0; i < importedMesh.VertexCount; i++)
            {
                Assert.IsTrue(importedMesh.Vertices[i].Equals(mesh.Vertices[i]));
            }

            Assert.IsTrue(importedMesh.FaceCount == mesh.FaceCount);
            for(int i = 0; i < importedMesh.FaceCount; i++)
            {
                Face importedFace = importedMesh.Faces[i];
                Face face = mesh.Faces[i];

                for(int j = 0; j < importedFace.IndexCount; j++)
                {
                    Assert.IsTrue(importedFace.Indices[j] == face.Indices[j]);
                }
            }
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestFreeLogStreams()
        {
            Assert.Zero(LogStream.AttachedLogStreamCount);
            var console1 = new ConsoleLogStream();
            var console2 = new ConsoleLogStream();
            var console3 = new ConsoleLogStream();

            console1.Attach();
            console2.Attach();
            console3.Attach();
            
            console1.Log("Test1");
            console2.Log("Test2");
            console3.Log("Test3");

            AssimpLibrary.Instance.FreeLibrary();

            var logs = LogStream.GetAttachedLogStreams();

            Assert.IsEmpty(logs);
            Assert.IsFalse(console1.IsAttached);
            Assert.IsFalse(console2.IsAttached);
            Assert.IsFalse(console3.IsAttached);

            Assert.Zero(LogStream.AttachedLogStreamCount);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromFile()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/sphere.obj");

            var context = new AssimpContext();
            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            context.Scale = .5f;
            context.XAxisRotation = 25.0f;
            context.YAxisRotation = 50.0f;
            LogStream.IsVerboseLoggingEnabled = true;

            var success = context.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);
            Assert.IsTrue(success);

            context.RemoveConfigs();
            success = context.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);
            Assert.IsFalse(success);

            context.SetConfig(new NormalSmoothingAngleConfig(65.0f));
            context.SetConfig(new NormalSmoothingAngleConfig(22.5f));
            context.RemoveConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);

            success = context.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);
            Assert.IsFalse(success);

            context.SetConfig(new NormalSmoothingAngleConfig(65.0f));

            var scene = context.ImportFile(path, PostProcessPreset.TargetRealTimeMaximumQuality);

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromStream()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            FileStream fs = File.OpenRead(path);

            AssimpContext importer = new AssimpContext();
            LogStream.IsVerboseLoggingEnabled = true;

            LogStream logstream = new LogStream(delegate(String msg, String userData)
            {
                Console.Write(msg);
            });

            logstream.Attach();

            var scene = importer.ImportFileFromStream(fs, ".dae");

            fs.Close();

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);

            logstream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromStreamNoFormatHint()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            
            var context = new AssimpContext();
            var logStream = new TestContextLogStream();
            logStream.Attach();
            LogStream.IsVerboseLoggingEnabled = true;

            var fs = File.OpenRead(path);
            var scene = context.ImportFileFromStream(fs, String.Empty); //null also seems to work well
            fs.Close();

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImporterDescriptions()
        {
            var context = new AssimpContext();
            var descriptions = context.GetImporterDescriptions();

            Assert.IsNotNull(descriptions);
            Assert.IsTrue(descriptions.Length > 0);

            var descriptionForObj = context.GetImporterDescriptionFor("obj");
            var descriptionForDotObj = context.GetImporterDescriptionFor(".obj");

            Assert.IsNotNull(descriptionForObj);
            Assert.IsNotNull(descriptionForDotObj);
            Assert.IsTrue(descriptionForObj.Name == descriptionForDotObj.Name);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestSupportedFormats()
        {
            var context = new AssimpContext();
            var exportFormatDescriptions = context.GetSupportedExportFormats();
            var importFormats = context.GetSupportedImportFormats();

            Assert.IsNotNull(exportFormatDescriptions);
            Assert.IsNotNull(importFormats);
            Assert.IsTrue(exportFormatDescriptions.Length >= 1);
            Assert.IsTrue(importFormats.Length >= 1);

            Assert.IsTrue(context.IsExportFormatSupported(exportFormatDescriptions[0].FileExtension));
            Assert.IsTrue(context.IsImportFormatSupported(importFormats[0]));

            Assert.IsTrue(context.IsExportFormatSupported("obj"));
            Assert.IsTrue(context.IsExportFormatSupported(".obj"));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromFile()
        {
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/Bob.dae");

            var context = new AssimpContext();
            context.ConvertFromFileToFile(inputPath, outputPath, "collada");

            var blob = context.ConvertFromFileToBlob(inputPath, "collada");
            Assert.IsTrue(blob.HasData);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromStreamNoFormatHint()
        {
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duckNoHint.obj");

            if (File.Exists(outputPath))
                File.Delete(outputPath);
            
            var context = new AssimpContext();
            
            var fs = File.OpenRead(inputPath);
            var success = context.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            fs.Close();
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(outputPath));

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromStream()
        {
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            var inputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.obj");
            var outputPath2 = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck-fromBlob.obj");
            
            var context = new AssimpContext();

            var fs = File.OpenRead(inputPath);
            bool success = context.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(outputPath));
            
            fs.Position = 0;
            var blob = context.ConvertFromStreamToBlob(fs, ".dae", "collada");
            fs.Close();
            Assert.IsTrue(blob.HasData);

            //Take ExportDataBlob's data, write it to a memory stream and export that back to an obj and write it
            var memStream = new MemoryStream();
            memStream.Write(blob.Data, 0, blob.Data.Length);
            memStream.Position = 0;
            success = context.ConvertFromStreamToFile(memStream, ".dae", outputPath2, "obj");
            memStream.Close();
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(outputPath2));

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestMultipleFileImportersMultipleThreads([Range(0,128, 16)]int threadCount)
        {
            if (threadCount == 0)
                threadCount = Environment.ProcessorCount;
            
            var rng = new Random(threadCount);

            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);
            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 4) switch {
                    0 => new Thread(LoadSceneA),
                    1 => new Thread(LoadSceneB),
                    2 => new Thread(ConvertSceneC),
                    3 => new Thread(ConvertSceneD)
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestMultipleStreamingImportersMultipleThreads([Range(0,128, 16)]int threadCount) {
            if (threadCount == 0)
                threadCount = Environment.ProcessorCount;
            
            var rng = new Random(threadCount);
            
            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);
            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 4) switch {
                    0 => new Thread(StreamSceneE),
                    1 => new Thread(StreamSceneF),
                    2 => new Thread(ConvertStreamSceneG),
                    3 => new Thread(ConvertStreamSceneH)
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        [Test, Parallelizable(ParallelScope.None)]
        public void TestMultipleImportersMultipleThreads([Range(0,128, 16)]int threadCount) {
            if (threadCount == 0)
                threadCount = Environment.ProcessorCount;
            
            var rng = new Random(threadCount);
            
            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);

            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 8) switch {
                    0 => new Thread(LoadSceneA),
                    1 => new Thread(LoadSceneB),
                    2 => new Thread(ConvertSceneC),
                    3 => new Thread(ConvertSceneD),
                    4 => new Thread(StreamSceneE),
                    5 => new Thread(StreamSceneF),
                    6 => new Thread(ConvertStreamSceneG),
                    7 => new Thread(ConvertStreamSceneH)
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        private void LoadSceneA(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread A";
            logStream.Attach();
            
            logStream.Log("Establishing Context for import");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            logStream.Log("Importing");
            var scene = context.ImportFile(path);
            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
            logStream.Log("Done importing");
            
            logStream.Detach();
        }

        private void LoadSceneB(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread B";
            logStream.Attach();

            logStream.Log("Establishing Context for import");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            logStream.Log("Importing");
            var scene = context.ImportFile(path);
            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
            logStream.Log("Done importing");

            logStream.Detach();
        }

        private void ConvertSceneC(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread C";
            logStream.Attach();
            
            logStream.Log("Establishing Context for conversionEstablishing Context for conversion");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck2.obj");

            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            context.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            var blob = context.ConvertFromFileToBlob(path, "obj");
            Assert.IsTrue(blob.HasData);
            logStream.Log("Done converting");
            
            logStream.Detach();
        }

        private void ConvertSceneD(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread D";
            logStream.Attach();
            
            logStream.Log("Establishing Context for conversion");
            var context = new AssimpContext();
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            context.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            context.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            var blob = context.ConvertFromFileToBlob(path, "obj");
            Assert.IsTrue(blob.HasData);
            logStream.Log("Done converting");
            
            logStream.Detach();
        }
        
        private void StreamSceneE(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread E";
            logStream.Attach();

            logStream.Log("Establishing Context for stream import");
            var context = new AssimpContext();
            
            logStream.Log("Importing");
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            using var sr = new StreamReader(path);
            var streamScene = context.ImportFileFromStream(sr.BaseStream);
            sr.Close();
            Assert.IsNotNull(streamScene);
            Assert.IsTrue((streamScene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
            logStream.Log("Done importing");
            
            logStream.Detach();
        }

        private void StreamSceneF(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread F";
            logStream.Attach();
            
            logStream.Log("Establishing Context for Stream import");
            var context = new AssimpContext();
            
            logStream.Log("Importing");
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            using var sr = new StreamReader(path);
            var streamScene = context.ImportFileFromStream(sr.BaseStream);
            sr.Close();
            Assert.IsNotNull(streamScene);
            Assert.IsTrue((streamScene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);
            logStream.Log("Done importing");
            
            logStream.Detach();
        }

        private void ConvertStreamSceneG(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread G";
            logStream.Attach();
            
            logStream.Log("Establishing Context for Stream conversion");
            var importer = new AssimpContext();
            var inputFilename = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var inputHint = Path.GetExtension(inputFilename).TrimStart('.');
            var outputFilename = Path.GetTempFileName();
            const string outputHint = "obj";
            
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            using var sr = new StreamReader(inputFilename);
            var success = importer.ConvertFromStreamToFile(sr.BaseStream, inputHint, outputFilename, outputHint);
            sr.Close();
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(outputFilename));
            logStream.Log("Done converting");
            
            logStream.Detach();
        }

        private void ConvertStreamSceneH(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread H";
            logStream.Attach();
            
            logStream.Log("Establishing Context for Stream conversion");
            var importer = new AssimpContext();
            var inputFilename = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            var inputHint = Path.GetExtension(inputFilename).TrimStart('.');
            var outputFilename = Path.GetTempFileName();
            const string outputHint = "obj";
            
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            logStream.Log("Converting");
            using var sr = new StreamReader(inputFilename);
            var success = importer.ConvertFromStreamToFile(sr.BaseStream, inputHint, outputFilename, outputHint);
            sr.Close();
            Assert.IsTrue(success);
            Assert.IsTrue(File.Exists(outputFilename));
            logStream.Log("Done converting");
            
            logStream.Detach();
        }

    }
}
