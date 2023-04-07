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

  public string UserData
  {
    get { return m_userData; }
    set { m_userData = value; }
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
