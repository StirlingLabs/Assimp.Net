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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Assimp.Unmanaged;

namespace Assimp
{
  /// <summary>
  /// Callback delegate for Assimp's LogStream.
  /// </summary>
  /// <param name="msg">Log message</param>
  /// <param name="userData">Supplied user data</param>
  public delegate void LoggingCallback(String msg, String userData);

  /// <summary>
  /// Represents a log stream, which receives all log messages and streams them somewhere.
  /// </summary>
  [DebuggerDisplay("IsAttached = {IsAttached}")]
  public class LogStream : IDisposable
  {
    private static readonly AiLogStreamCallback s_dlgStaticOnLogStreamCallback = StaticOnAiLogStreamCallback;
    
    private static ImmutableList<LogStream> s_attachedLogStreams = ImmutableList<LogStream>.Empty;
    
    private static ThreadLocal<HashSet<LogStream>> s_tlsLogStreams = new(() => new());

    private static HashSet<LogStream> s_logStreams => s_tlsLogStreams.Value;
    
    private static unsafe AiLogStream* s_logstreamPtr;

    private static int s_instanceCounter = 0;

    private LoggingCallback m_logCallback;

    protected String m_userData;

    private int m_isDisposedValue;

    private int m_isAttachedValue;

    public static int AttachedLogStreamCount => s_attachedLogStreams.Count;
    
    /// <summary>
    /// Gets or sets, if verbose logging is enabled globally.
    /// </summary>
    public static bool IsVerboseLoggingEnabled
    {
      get
      {
        return AssimpLibrary.Instance.GetVerboseLoggingEnabled();
      }
      set
      {
        AssimpLibrary.Instance.EnableVerboseLogging(value);
      }
    }

    /// <summary>
    /// Gets or sets the user data to be passed to the callback.
    /// </summary>
    public String UserData
    {
      get
      {
        return m_userData;
      }
      set
      {
        m_userData = value;
      }
    }

    /// <summary>
    /// Gets whether the logstream has been disposed or not.
    /// </summary>
    public bool IsDisposed
    {
      get
      {
        return Interlocked.CompareExchange(ref m_isDisposedValue, 0, 0) != 0;
      }
    }

    /// <summary>
    /// Gets whether or not the logstream is currently attached to the library.
    /// </summary>
    public bool IsAttached
    {
      get
      {
        return Interlocked.CompareExchange(ref m_isAttachedValue, 0, 0) != 0;
      }
    }


    public bool IsAttachedOnThread {
      get
      {
        return s_logStreams.Contains(this);
      }
    }

    /// <summary>
    /// Static constructor.
    /// </summary>
    static unsafe LogStream()
    {
      AssimpLibrary.Instance.LibraryFreed += AssimpLibraryFreed;
      s_logstreamPtr = (AiLogStream*)MemoryHelper.AllocateMemory(MemoryHelper.SizeOf<AiLogStream>());
      s_logstreamPtr->UserData = default;
      s_logstreamPtr->Callback = Marshal.GetFunctionPointerForDelegate(s_dlgStaticOnLogStreamCallback);
    }

    /// <summary>
    /// Constructs a new LogStream.
    /// </summary>
    /// <param name="initialize">
    /// Whether to immediately initialize the system by setting up native pointers. Set this to
    /// false if you want to manually initialize and use custom function pointers for advanced use cases.
    /// </param>
    protected LogStream(bool initialize = true) : this("", initialize) { }

    /// <summary>
    /// Constructs a new LogStream.
    /// </summary>
    /// <param name="userData">User-supplied data</param>
    /// <param name="initialize">True if initialize should be immediately called with the default callbacks. Set this to false
    /// if your subclass requires a different way to setup the function pointers.</param>
    protected LogStream(String userData, bool initialize = true)
    {
      if (initialize)
        Initialize(null, userData);
    }

    /// <summary>
    /// Constructs a new LogStream.
    /// </summary>
    /// <param name="callback">Logging callback that is called when messages are received by the log stream.</param>
    public LogStream(LoggingCallback callback)
    {
      Initialize(callback);
    }

    /// <summary>
    /// Constructs a new LogStream.
    /// </summary>
    /// <param name="callback">Logging callback that is called when messages are received by the log stream.</param>
    /// <param name="userData">User-supplied data</param>
    public LogStream(LoggingCallback callback, String userData)
    {
      Initialize(callback, userData);
    }

    /// <summary>
    /// Finalizes an instance of the <see cref="LogStream"/> class.
    /// </summary>
    ~LogStream()
    {
      Dispose(false);
    }

    /// <summary>
    /// Detaches all active logstreams from the library.
    /// </summary>
    public static void DetachAllLogstreams() {
      var attachedLogStreams
        = Interlocked.Exchange(ref s_attachedLogStreams,
          s_attachedLogStreams.Clear());

      foreach (var logStream in attachedLogStreams)
        logStream.Detach(); 
    }

    /// <summary>
    /// Gets all active logstreams that are currently attached to the library.
    /// </summary>
    /// <returns>Collection of active logstreams attached to the library.</returns>
    public static IEnumerable<LogStream> GetAttachedLogStreams()
      => s_attachedLogStreams;

    //Ensure we cleanup our logstreams if any are around when the unmanaged library is freed.
    private static void AssimpLibraryFreed(object sender, EventArgs e)
    {
      DetachAllLogstreams();
    }

    private static unsafe void StaticAttach() {
      if (Interlocked.Increment(ref s_instanceCounter) != 1)
        return;

      AssimpLibrary.Instance.AttachLogStream((nint)s_logstreamPtr);
    }

    private static unsafe void StaticDetach() {
      if (Interlocked.Decrement(ref s_instanceCounter) != 0)
        return;
      
      AssimpLibrary.Instance.DetachLogStream((nint)s_logstreamPtr);
    }
    

    /// <summary>
    /// Attaches the logstream to the library.
    /// </summary>
    public bool Attach()
    {
      var wasAttached = Interlocked.Increment(ref m_isAttachedValue) > 1;
      
      var added = s_logStreams.Add(this);

      if (!added) {
        Interlocked.Decrement(ref m_isAttachedValue);
        return false;
      }
      
      
      if (!wasAttached) {
        ImmutableInterlocked.Update(ref s_attachedLogStreams,
          static (attachedLogStreams, item) => attachedLogStreams.Add(item),
          this);
        OnAttach();
      }

      StaticAttach();
      
      return true;
    }

    /// <summary>
    /// Detaches the logstream from the library.
    /// </summary>
    public bool Detach()
    {
      var shouldDetach = Interlocked.Decrement(ref m_isAttachedValue) == 0;
      
      var removed = s_logStreams.Remove(this);

      if (!removed)
        return false;

      if (shouldDetach) {
        ImmutableInterlocked.Update(ref s_attachedLogStreams,
          static (attachedLogStreams, item) => attachedLogStreams.Remove(item),
          this);
        OnDetach();
      }
        
      StaticDetach();

      return true;
    }

    /// <summary>
    /// Logs a message.
    /// </summary>
    /// <param name="msg">Message contents</param>
    public void Log(String msg)
    {
      if (String.IsNullOrEmpty(msg))
        return;

      OnAiLogStreamCallback(msg);
    }

    /// <summary>
    /// Releases unmanaged resources held by the LogStream. This should not be called by the user if the logstream is currently attached to an assimp importer.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and - optionally - managed resources.
    /// </summary>
    /// <param name="disposing">True to release both managed and unmanaged resources; False to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (Interlocked.CompareExchange(ref m_isDisposedValue, 1, 0) != 0)
        return;

      while (Detach())
      {
        // call it again
      }
    }

    /// <summary>
    /// Override this method to log a message for a subclass of Logstream, if no callback
    /// was set.
    /// </summary>
    /// <param name="msg">Message</param>
    /// <param name="userData">User data</param>
    protected virtual void LogMessage(String msg, String userData) { }

    /// <summary>
    /// Called when the log stream has been attached to the assimp importer. At this point it may start receiving messages.
    /// </summary>
    protected virtual void OnAttach() { }

    /// <summary>
    /// Called when the log stream has been detatched from the assimp importer. After this point it will stop receiving
    /// messages until it is re-attached.
    /// </summary>
    protected virtual void OnDetach() { }

    /// <summary>
    /// Callback for Assimp that handles a message being logged.
    /// </summary>
    /// <param name="msg"></param>
    /// <param name="userData"></param>
    protected void OnAiLogStreamCallback(String msg)
    {
      if (m_logCallback != null)
      {
        m_logCallback(msg, m_userData);
      }
      else
      {
        LogMessage(msg, m_userData);
      }
    }
    protected static void StaticOnAiLogStreamCallback(String msg, IntPtr userData)
    {
      foreach (var logStream in s_logStreams)
      {
        logStream.OnAiLogStreamCallback(msg);
      }
    }

    /// <summary>
    /// Initializes the stream by setting up native pointers for Assimp to the specified functions.
    /// </summary>
    /// <param name="aiLogStreamCallback">Callback that is marshaled to native code, a reference is held on to avoid it being GC'ed.</param>
    /// <param name="callback">User callback, if any. Defaults to console if null.</param>
    /// <param name="userData">User data, or empty.</param>
    /// <param name="assimpUserData">Additional assimp user data, if any.</param>
    protected void Initialize(LoggingCallback callback, String userData = "")
    {
      if (userData == null)
        userData = String.Empty;

      m_logCallback = callback;
      m_userData = userData;

    }
  }

  /// <summary>
  /// Log stream that writes messages to the Console.
  /// </summary>
  public sealed class ConsoleLogStream : LogStream
  {
    /// <summary>
    /// Constructs a new console logstream.
    /// </summary>
    public ConsoleLogStream() : base() { }

    /// <summary>
    /// Constructs a new console logstream.
    /// </summary>
    /// <param name="userData">User supplied data</param>
    public ConsoleLogStream(String userData) : base(userData) { }

    /// <summary>
    /// Log a message to the console.
    /// </summary>
    /// <param name="msg">Message</param>
    /// <param name="userData">Userdata</param>
    protected override void LogMessage(String msg, String userData)
    {
      if (String.IsNullOrEmpty(userData))
      {
        Console.Write(msg);
      }
      else
      {
        Console.Write(String.Format("{0}: {1}", userData, msg));
      }
    }
  }
}
