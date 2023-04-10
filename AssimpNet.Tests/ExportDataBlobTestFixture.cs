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

using System.IO;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class ExportDataBlobTestFixture
    {
        [Test]
        public void TestToStream()
        {
            var path = Path.Combine(TestHelper.RootPath, "TestFiles/duck.dae");

            var context = new AssimpContext();
            var logStream = new TestContextLogStream();
            logStream.Attach();
            
            var blob = context.ConvertFromFileToBlob(path, "obj");

            Assert.That(blob, Is.Not.Null);
            Assert.That(blob.HasData, Is.True);

            var stream = new MemoryStream();
            blob.ToStream(stream);

            Assert.That(stream.Length, Is.Not.Zero);
            stream.Position = 0;

            var blob2 = ExportDataBlob.FromStream(stream);
            Assert.Multiple(() =>
            {
                Assert.That(blob2, Is.Not.Null);
                Assert.That(blob2.Data, Has.Length.EqualTo(blob.Data.Length));
            });

            if (blob.NextBlob != null)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(blob2.NextBlob, Is.Not.Null);
                    Assert.That(blob2.NextBlob.Name, Is.EqualTo(blob.NextBlob.Name));
                    Assert.That(blob2.NextBlob.Data, Has.Length.EqualTo(blob.NextBlob.Data.Length));
                });
            }
            else
            {
                logStream.Log($"blob.NextBlob is null");
            }

            logStream.Detach();
        }
    }
}
