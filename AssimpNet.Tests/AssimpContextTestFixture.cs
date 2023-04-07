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
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output");

            if (!Directory.Exists(outputPath))
                Directory.CreateDirectory(outputPath);

            IEnumerable<String> filePaths = Directory.GetFiles(outputPath);

            foreach(String filePath in filePaths)
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportBadFormatId()
        {
            AssimpContext importer = new AssimpContext();
            NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
            importer.SetConfig(config);

            LogStream logStream = new LogStream(delegate (string msg, string userData)
            {
                Console.WriteLine(msg);
            });

            logStream.Attach();

            Scene collada = importer.ImportFile(Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae"));

            bool success = importer.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "dae");

            Assert.IsFalse(success);

            success = importer.ExportFile(collada, Path.Combine(TestHelper.RootPath, "TestFiles/output/exportedCollada.dae"), "collada");

            Assert.IsTrue(success);

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportToBlob()
        {
            String colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            AssimpContext context = new AssimpContext();
            Scene ducky = context.ImportFile(colladaPath);
            ExportDataBlob blob = context.ExportToBlob(ducky, "obj");

            Assert.IsTrue(blob.HasData);
            Assert.IsTrue(blob.NextBlob != null);
            Assert.IsTrue(blob.NextBlob.Name.Equals("mtl"));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportExportFile()
        {
            String colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.ply");

            AssimpContext context = new AssimpContext();
            Scene ducky = context.ImportFile(colladaPath);
            context.ExportFile(ducky, plyPath, "ply");
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportExportImportFile()
        {
            String colladaPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String plyPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck2.dae");

            AssimpContext context = new AssimpContext();
            Scene ducky = context.ImportFile(colladaPath);
            context.ExportFile(ducky, plyPath, "collada");

            Scene ducky2 = context.ImportFile(plyPath);
            Assert.IsNotNull(ducky2);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestExportToFile()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/ExportedTriangle.obj");

            //Create a very simple scene a single node with a mesh that has a single face, a triangle and a default material
            Scene scene = new Scene();
            scene.RootNode = new Node("Root");

            Mesh triangle = new Mesh("", PrimitiveType.Triangle);
            triangle.Vertices.Add(new Vector3D(1, 0, 0));
            triangle.Vertices.Add(new Vector3D(5, 5, 0));
            triangle.Vertices.Add(new Vector3D(10, 0, 0));
            triangle.Faces.Add(new Face(new int[] { 0, 1, 2 }));
            triangle.MaterialIndex = 0;

            scene.Meshes.Add(triangle);
            scene.RootNode.MeshIndices.Add(0);

            Material mat = new Material();
            mat.Name = "MyMaterial";
            scene.Materials.Add(mat);

            //Export the scene then read it in and compare!

            AssimpContext context = new AssimpContext();
            Assert.IsTrue(context.ExportFile(scene, path, "obj"));

            Scene importedScene = context.ImportFile(path);
            Assert.IsTrue(importedScene.MeshCount == scene.MeshCount);
            Assert.IsTrue(importedScene.MaterialCount == 2); //Always has the default material, should also have our material

            //Compare the meshes
            Mesh importedTriangle = importedScene.Meshes[0];

            Assert.IsTrue(importedTriangle.VertexCount == triangle.VertexCount);
            for(int i = 0; i < importedTriangle.VertexCount; i++)
            {
                Assert.IsTrue(importedTriangle.Vertices[i].Equals(triangle.Vertices[i]));
            }

            Assert.IsTrue(importedTriangle.FaceCount == triangle.FaceCount);
            for(int i = 0; i < importedTriangle.FaceCount; i++)
            {
                Face importedFace = importedTriangle.Faces[i];
                Face face = triangle.Faces[i];

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
            ConsoleLogStream console1 = new ConsoleLogStream();
            ConsoleLogStream console2 = new ConsoleLogStream();
            ConsoleLogStream console3 = new ConsoleLogStream();

            console1.Attach();
            console2.Attach();
            console3.Attach();

            AssimpLibrary.Instance.FreeLibrary();

            IEnumerable<LogStream> logs = LogStream.GetAttachedLogStreams();

            Assert.IsEmpty(logs);
            Assert.IsFalse(console1.IsAttached);
            Assert.IsFalse(console2.IsAttached);
            Assert.IsFalse(console3.IsAttached);

            Assert.Zero(LogStream.AttachedLogStreamCount);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromFile()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/sphere.obj");

            AssimpContext importer = new AssimpContext();

            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.Scale = .5f;
            importer.XAxisRotation = 25.0f;
            importer.YAxisRotation = 50.0f;
            LogStream.IsVerboseLoggingEnabled = true;

            Assert.IsTrue(importer.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName));

            importer.RemoveConfigs();

            Assert.IsFalse(importer.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName));

            importer.SetConfig(new NormalSmoothingAngleConfig(65.0f));
            importer.SetConfig(new NormalSmoothingAngleConfig(22.5f));
            importer.RemoveConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName);

            Assert.IsFalse(importer.ContainsConfig(NormalSmoothingAngleConfig.NormalSmoothingAngleConfigName));

            importer.SetConfig(new NormalSmoothingAngleConfig(65.0f));

            Scene scene = importer.ImportFile(path, PostProcessPreset.TargetRealTimeMaximumQuality);

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
                Console.WriteLine(msg);
            });

            logstream.Attach();

            Scene scene = importer.ImportFileFromStream(fs, ".dae");

            fs.Close();

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);

            logstream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImportFromStreamNoFormatHint()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            FileStream fs = File.OpenRead(path);

            AssimpContext importer = new AssimpContext();
            LogStream.IsVerboseLoggingEnabled = true;

            LogStream logstream = new LogStream(delegate (String msg, String userData)
            {
                Console.WriteLine(msg);
            });

            logstream.Attach();

            Scene scene = importer.ImportFileFromStream(fs, String.Empty); //null also seems to work well

            fs.Close();

            Assert.IsNotNull(scene);
            Assert.IsTrue((scene.SceneFlags & SceneFlags.Incomplete) != SceneFlags.Incomplete);

            logstream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestImporterDescriptions()
        {
            AssimpContext importer = new AssimpContext();
            ImporterDescription[] importerDescrs = importer.GetImporterDescriptions();

            Assert.IsNotNull(importerDescrs);
            Assert.IsTrue(importerDescrs.Length > 0);

            ImporterDescription descr = importer.GetImporterDescriptionFor("obj");
            ImporterDescription descr2 = importer.GetImporterDescriptionFor(".obj");

            Assert.IsNotNull(descr);
            Assert.IsNotNull(descr2);
            Assert.IsTrue(descr.Name == descr2.Name);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestSupportedFormats()
        {
            AssimpContext importer = new AssimpContext();
            ExportFormatDescription[] exportDescs = importer.GetSupportedExportFormats();

            String[] importFormats = importer.GetSupportedImportFormats();

            Assert.IsNotNull(exportDescs);
            Assert.IsNotNull(importFormats);
            Assert.IsTrue(exportDescs.Length >= 1);
            Assert.IsTrue(importFormats.Length >= 1);

            Assert.IsTrue(importer.IsExportFormatSupported(exportDescs[0].FileExtension));
            Assert.IsTrue(importer.IsImportFormatSupported(importFormats[0]));

            Assert.IsTrue(importer.IsExportFormatSupported("obj"));
            Assert.IsTrue(importer.IsExportFormatSupported(".obj"));
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromFile()
        {
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/Bob.dae");

            AssimpContext importer = new AssimpContext();
            importer.ConvertFromFileToFile(path, outputPath, "collada");

            ExportDataBlob blob = importer.ConvertFromFileToBlob(path, "collada");
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromStreamNoFormatHint()
        {
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duckNoHint.obj");

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            FileStream fs = File.OpenRead(path);

            AssimpContext importer = new AssimpContext();
            bool success = importer.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            Assert.IsTrue(success);

            Assert.IsTrue(File.Exists(outputPath));

            logStream.Detach();
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestConvertFromStream()
        {
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck.obj");
            String outputPath2 = Path.Combine(TestHelper.RootPath, "TestFiles/output/duck-fromBlob.obj");

            FileStream fs = File.OpenRead(path);

            AssimpContext importer = new AssimpContext();
            bool success = importer.ConvertFromStreamToFile(fs, ".dae", outputPath, "obj");
            Assert.IsTrue(success);

            fs.Position = 0;

            ExportDataBlob blob = importer.ConvertFromStreamToBlob(fs, ".dae", "collada");
            Assert.IsNotNull(blob);

            fs.Close();

            //Take ExportDataBlob's data, write it to a memory stream and export that back to an obj and write it

            MemoryStream memStream = new MemoryStream();
            memStream.Write(blob.Data, 0, blob.Data.Length);

            memStream.Position = 0;

            success = importer.ConvertFromStreamToFile(memStream, ".dae", outputPath2, "obj");

            memStream.Close();

            logStream.Detach();

            Assert.IsTrue(success);
        }

        [Test, Parallelizable(ParallelScope.Self)]
        public void TestMultipleImportersMultipleThreads()
        {
            LogStream.IsVerboseLoggingEnabled = true;

            Thread threadA = new Thread(LoadSceneB);
            Thread threadB = new Thread(LoadSceneB);
            Thread threadC = new Thread(ConvertSceneC);

            threadB.Start(new TestContextLogStream());
            threadA.Start(new TestContextLogStream());
            threadC.Start(new TestContextLogStream());

            threadC.Join();
            threadA.Join();
            threadB.Join();
        }

        [Test, Parallelizable(ParallelScope.All)]
        [Ignore("Ignore impossible test, this is for debugging purposes only")]
        public void TestMultipleImportersMultipleThreadsHardcore([Range(1,128)]int threadCount) {
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
                threads[i].Start();
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        [Test, Parallelizable(ParallelScope.All)]
        public void TestMultipleStreamingImportersMultipleThreads([Range(1,4)]int threadCount) {
            var rng = new Random(threadCount);
            
            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);

            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 4) switch {
                    0 => new Thread(StreamSceneA),
                    1 => new Thread(StreamSceneB),
                    2 => new Thread(ConvertStreamSceneA),
                    3 => new Thread(ConvertStreamSceneB)
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start(new TestContextLogStream());
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        [Test, Parallelizable(ParallelScope.All)]
        [Ignore("Ignore impossible test, this is for debugging purposes only")]
        public void TestMultipleStreamingImportersMultipleThreadsHardcore([Range(1,128)]int threadCount) {
            var rng = new Random(threadCount);
            
            LogStream.IsVerboseLoggingEnabled = true;

            var threads = new List<Thread>(threadCount);

            for (var i = 0; i < threadCount; ++i) {
                threads.Add((i % 4) switch {
                    0 => new Thread(StreamSceneA),
                    1 => new Thread(StreamSceneB),
                    2 => new Thread(ConvertStreamSceneA),
                    3 => new Thread(ConvertStreamSceneB)
                });
            }

            threads.Shuffle(rng);
            
            for (var i = 0; i < threadCount; ++i)
                threads[i].Start();
            
            threads.Shuffle(rng);

            for (var i = 0; i < threadCount; ++i)
                threads[i].Join();
        }

        private void LoadSceneA(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread A:";
            logStream.Attach();
            
            Console.WriteLine("Thread A: Starting import.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            Console.WriteLine("Thread A: Importing");
            Scene scene = importer.ImportFile(path);
            Console.WriteLine("Thread A: Done importing");
            
            logStream.Detach();
        }

        private void LoadSceneB(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread B:";
            logStream.Attach();

            Console.WriteLine("Thread B: Starting import.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            Console.WriteLine("Thread B: Importing");
            Scene scene = importer.ImportFile(path);
            Console.WriteLine("Thread B: Done importing");

            logStream.Detach();
        }

        private void ConvertSceneC(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread C:";
            logStream.Attach();
            
            Console.WriteLine("Thread C: Starting convert.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/duck2.obj");

            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            Console.WriteLine("Thread C: Converting");
            ExportDataBlob blob = importer.ConvertFromFileToBlob(path, "obj");

            Console.WriteLine("Thread C: Done converting");
            
            logStream.Detach();
        }

        private void ConvertSceneD(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread D:";
            logStream.Attach();
            
            Console.WriteLine("Thread D: Starting convert.");
            AssimpContext importer = new AssimpContext();
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            String outputPath = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.obj");

            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            Console.WriteLine("Thread D: Converting");
            ExportDataBlob blob = importer.ConvertFromFileToBlob(path, "obj");

            Console.WriteLine("Thread D: Done converting");
            
            logStream.Detach();
        }
        
        private void StreamSceneA(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread A:";
            logStream.Attach();
            
            Console.WriteLine("Thread A: Starting import.");
            AssimpContext importer = new AssimpContext();
            
            Console.WriteLine("Thread A: Importing");
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");

            using var sr = new StreamReader(path);
            var scene = importer.ImportFileFromStream(sr.BaseStream);
            sr.Close();

            // Scene scene = importer.ImportFile(path);
            Console.WriteLine("Thread A: Done importing");
            
            logStream.Detach();
        }

        private void StreamSceneB(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread A:";
            logStream.Attach();
            
            Console.WriteLine("Thread A: Starting import.");
            AssimpContext importer = new AssimpContext();
            
            Console.WriteLine("Thread A: Importing");
            String path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            using var sr = new StreamReader(path);
            var scene = importer.ImportFileFromStream(sr.BaseStream);
            sr.Close();

            // Scene scene = importer.ImportFile(path);
            Console.WriteLine("Thread A: Done importing");
            
            logStream.Detach();
        }

        private void ConvertStreamSceneA(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread C:";
            logStream.Attach();
            
            Console.WriteLine("Thread C: Starting convert.");
            var importer = new AssimpContext();
            var inputFilename = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");
            var inputHint = Path.GetExtension(inputFilename).TrimStart('.');
            var outputFilename = Path.GetTempFileName();
            const string outputHint = "obj";
            
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            Console.WriteLine("Thread C: Converting");
            using var sr = new StreamReader(inputFilename);
            // var blob = importer.ConvertFromStreamToBlob(sr.BaseStream, inputHint, outputHint);
            var s = importer.ConvertFromStreamToFile(sr.BaseStream, inputHint, outputFilename, outputHint);
            sr.Close();
            Console.WriteLine("Thread C: Done converting");
            
            logStream.Detach();
        }

        private void ConvertStreamSceneB(object logStreamObj)
        {
            var logStream = (TestContextLogStream)logStreamObj;
            logStream.UserData = "Thread B:";
            logStream.Attach();
            
            Console.WriteLine("Thread B: Starting convert.");
            var importer = new AssimpContext();
            var inputFilename = Path.Combine(TestHelper.RootPath, "TestFiles/Bob.md5mesh");
            var inputHint = Path.GetExtension(inputFilename).TrimStart('.');
            var outputFilename = Path.GetTempFileName();
            const string outputHint = "obj";
            
            importer.SetConfig(new NormalSmoothingAngleConfig(55.0f));
            importer.SetConfig(new FavorSpeedConfig(true));

            Console.WriteLine("Thread B: Converting");
            using var sr = new StreamReader(inputFilename);
            // var blob = importer.ConvertFromStreamToBlob(sr.BaseStream, inputHint, outputHint);
            var s = importer.ConvertFromStreamToFile(sr.BaseStream, inputHint, outputFilename, outputHint);
            sr.Close();
            Console.WriteLine("Thread B: Done converting");
            
            logStream.Detach();
        }

    }
}
