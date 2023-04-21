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
using ImageMagick;
using NUnit.Framework;

namespace Assimp.Test
{
    [TestFixture]
    public class MaterialTestFixture
    {

        [TestCase]
        public void TestBaseMaterialProperty()
        {
            var material = new Material();
            var prop = new MaterialProperty(null, false);

            var success = material.AddProperty(prop);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.False);
                Assert.That(string.IsNullOrEmpty(prop.FullyQualifiedName), Is.True);
            });
            
            prop = new MaterialProperty("test", "test value");
            success = material.AddProperty(prop);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(material.PropertyCount, Is.EqualTo(1));
                Assert.That(material.HasProperty("test,0,0"), Is.True);
                var mp = material.GetProperty("test,0,0");
                Assert.That(mp.Name, Is.EqualTo("test"));
                Assert.That(mp.PropertyType, Is.EqualTo(PropertyType.String));
                Assert.That(mp.GetStringValue(), Is.EqualTo("test value"));
            });
            
            prop = new MaterialProperty("test2", 1.0f);
            success = material.AddProperty(prop);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(material.PropertyCount, Is.EqualTo(2));
                Assert.That(material.HasProperty("test2,0,0"), Is.True);
                var mp = material.GetProperty("test2,0,0");
                Assert.That(mp.Name, Is.EqualTo("test2"));
                Assert.That(mp.PropertyType, Is.EqualTo(PropertyType.Float));
                Assert.That(mp.GetFloatValue(), Is.EqualTo(1.0f));
            });
            
            prop = new MaterialProperty("test3", true);
            success = material.AddProperty(prop);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(material.PropertyCount, Is.EqualTo(3));
                Assert.That(material.HasProperty("test3,0,0"), Is.True);
                var mp = material.GetProperty("test3,0,0");
                Assert.That(mp.Name, Is.EqualTo("test3"));
                Assert.That(mp.GetBooleanValue(), Is.True);
            });
            
            var color = new Color4D(1.0f, 0.0f, 0.0f, 1.0f);
            prop = new MaterialProperty("test4", color);
            success = material.AddProperty(prop);
            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(material.PropertyCount, Is.EqualTo(4));
                var color = new Color4D(1.0f, 0.0f, 0.0f, 1.0f);
                Assert.That(material.HasProperty("test4,0,0"), Is.True);
                var mp = material.GetProperty("test4,0,0");
                Assert.That(mp.Name, Is.EqualTo("test4"));
                Assert.That(mp.GetColor4DValue(), Is.EqualTo(color));
            });
            foreach (var property in material.GetAllProperties())
            {
                Console.WriteLine($"{property.Name} aka '{property.FullyQualifiedName}' of type {property.PropertyType}.");
            }
        }

        [TestCase]
        public void TestEmbeddedTexture()
        {
            var inputFile = Path.Combine(TestHelper.RootPath, "TestFiles/apples.fbx");

            var context = new AssimpContext();
            var log = new TestContextLogStream();
            log.Attach();

            var scene = context.ImportFile(inputFile);
            Assert.That(scene, Is.Not.Null);
            Assert.That(scene.Textures, Is.Not.Empty);
            foreach (var text in scene.Textures)
            {
                Assert.That(text.HasCompressedData || text.HasNonCompressedData, Is.True);
                if (text.HasNonCompressedData)
                {
                    Assert.That(text.NonCompressedDataSize, Is.GreaterThan(0));
                    // var pixels = text.NonCompressedData...;
                    // var img = new MagickImage(pixels, new PixelReadSettings(text.Width, text.Height, StorageType.Float, PixelMapping.ABGR));
                }
                if (text.HasCompressedData)
                {
                    Assert.That(text.CompressedDataSize, Is.GreaterThan(0));
                    Assert.That(text.CompressedFormatHint, Is.Not.Empty);
                    var img = new MagickImage(text.CompressedData);
                    log.Log($"image: {img.Format} {img.Width}x{img.Height}: ");
                    Assert.That(img, Is.Not.Null);
                }
                log.Log($"{text.Filename} {text.Width}x{text.Height} {text.HasCompressedData}:{text.CompressedDataSize}({text.CompressedFormatHint}) {text.HasNonCompressedData}:{text.NonCompressedDataSize}\n");
            }
            log.Detach();
        }
        
        [TestCase]
        public void TestCreatingEmbeddedTexture()
        {
            var scene = new Scene();
            const string fileName = "TestFiles/duckCM.bmp";
            var filePath = Path.Combine(TestHelper.RootPath, fileName);
            
            var imageBytes = new MagickImage(filePath).ToByteArray(MagickFormat.Png32);
            var texture = new EmbeddedTexture("Png32", imageBytes, fileName);
            scene.Textures.Add(texture);
            
            var texQuery = scene.GetEmbeddedTexture("*0");
            Assert.That(texQuery.HasCompressedData || texQuery.HasNonCompressedData, Is.True);
            Assert.That(texQuery, Is.EqualTo(texture));
        }
    }
}
