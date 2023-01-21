using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

public class ParallelStack
{
    public ParallelStack() : this(null)
    {
    }
    private ParallelStack(IStackFrame frame = null)
    {
        Stacks = new List<ParallelStack>();
        ThreadIds = new List<uint>();
        Frame = frame;
    }
    public List<ParallelStack> Stacks { get; }

    public IStackFrame Frame { get; }

    public List<uint> ThreadIds { get; set; }

    public void AddStack(uint threadId, List<IStackFrame> stack)
    {
        AddStack(threadId, stack, 0);
    }
    
    void AddStack(uint threadId, List<IStackFrame> stack, int index)
    {
        ThreadIds.Add(threadId);
        var firstFrame = stack[index].Text;
        var callstack = Stacks.FirstOrDefault(s => s.Frame.Text == firstFrame);
        if (callstack == null)
        {
            callstack = new ParallelStack(stack[index]);
            Stacks.Add(callstack);
        }

        if (index == stack.Count - 1)
        {
            callstack.ThreadIds.Add(threadId);
            return;
        }

        callstack.AddStack(threadId, stack, index + 1);
    }
}