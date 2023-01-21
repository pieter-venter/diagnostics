using Microsoft.Diagnostics.Tools.Stack.Model;
using Xunit;

namespace Microsoft.Diagnostics.Tools.Stack;

public class ModelTests
{
    [Fact]
    public void TestNoParameters()
    {
        var frame = new TraceEventStackFrame("System.Private.CoreLib!System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()"); 
        Assert.Equal("System.Private.CoreLib!System.Threading.PortableThreadPool+WorkerThread.WorkerThreadStart()", frame.Text);
        Assert.Equal("System.Private.CoreLib", frame.AssemblyName);
        Assert.Equal("System.Threading.PortableThreadPool+WorkerThread", frame.TypeName);
        Assert.Empty(frame.Signature);
    }
    
    [Fact]
    public void TestParameters()
    {
        var frame = new TraceEventStackFrame("System.Private.CoreLib!System.Threading.Tasks.Task.ExecuteWithThreadLocal(class System.Threading.Tasks.Task&,class System.Threading.Thread)"); 
        Assert.Equal("System.Private.CoreLib!System.Threading.Tasks.Task.ExecuteWithThreadLocal(class System.Threading.Tasks.Task&,class System.Threading.Thread)", frame.Text);
        Assert.Equal("System.Private.CoreLib", frame.AssemblyName);
        Assert.Equal("System.Threading.Tasks.Task", frame.TypeName);
        Assert.Equal(2, frame.Signature.Length);
        Assert.Equal("class System.Threading.Tasks.Task&", frame.Signature[0]);
        Assert.Equal("class System.Threading.Thread", frame.Signature[1]);
    }
}