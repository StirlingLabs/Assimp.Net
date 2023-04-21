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
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Assimp.Test; 


/// <summary>
/// Log stream that writes messages to the test context it was constructed in.
/// </summary>
public class TestContextLogStream : LogStream
{
  private readonly TextWriter m_writer;

  /// <summary>
  /// Constructs a new test context log stream.
  /// </summary>
  public TestContextLogStream() : base()
  {
    m_writer = TestExecutionContext.CurrentContext.OutWriter;
  }

  /// <summary>
  /// Constructs a new test context log stream.
  /// </summary>
  /// <param name="userData">User supplied data</param>
  public TestContextLogStream(String userData) : base(userData)
  {
    m_writer = TestExecutionContext.CurrentContext.OutWriter;
  }

  /// <summary>
  /// Log a message to the test context.
  /// </summary>
  /// <param name="msg">Message</param>
  /// <param name="userData">Userdata</param>
  protected override void LogMessage(String msg, String userData)
  {
    m_writer.Write(
      String.IsNullOrEmpty(userData)
        ? msg
        : $"{userData}: {msg}"
    );
  }

}
