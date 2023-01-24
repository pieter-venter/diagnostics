using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

public static class MermaidClassDiagramRenderer
{
    public static void Render(ParallelStack stack, IRenderer renderer)
    {
        int id = 1;
        WriteLine(renderer, "classDiagram");
        RenderGraph(renderer, stack, null, ref id);    
    }

    static void WriteLine(IRenderer renderer, string value)
    {
        renderer.Write($"{value}{Environment.NewLine}");
    }
    
     static void RenderGraph(IRenderer renderer, ParallelStack currentFrame, Stack<IStackFrame> block, ref int id)
        {
            if (currentFrame.Stacks.Count == 0)
            {
                // Last item on frame
                block.Push(currentFrame.Frame);
                RenderBlock(renderer, currentFrame.ThreadIds.Count, block, id);
                
            } else if (currentFrame.Stacks.Count == 1)
            {
                //console.Out.WriteLine(currentFrame.Frame.Text);
                block.Push(currentFrame.Frame);
                
                // Move one up the stack, using the same block and ID
                RenderGraph(renderer, currentFrame.Stacks[0], block, ref id);
            }
            else
            {
                // Root does not have a frame value, only child nodes
                if (currentFrame.Frame is not null)
                {
                    // We're at a fork (more than item in Stack property). Push the current frame and render.
                    block.Push(currentFrame.Frame);
                    RenderBlock(renderer, currentFrame.ThreadIds.Count, block, id);
                }

                // Remember my ID (the parent) so I can link my children once they return from recursive call.
                int parentId = id;
                foreach (var stack in currentFrame.Stacks)
                {
                    // This is a byref ID, so we get a global unique number.
                    id++;
                    // Remember child ID, since it may change (by ref) when walking the tree.
                    int childId = id;
                    // Since this is a new block, start a new block.
                    block = new Stack<IStackFrame>();
                    RenderGraph(renderer, stack, block, ref id);
                    WriteLine(renderer, $"{childId} <|-- {parentId}");
                }   
            }
        }

        static void RenderBlock(IRenderer renderer, int threads, Stack<IStackFrame> block, int id)
        {
            if (block is not null)
            {
                WriteLine(renderer, $"class {id} {{");
                WriteLine(renderer, $"Number of Threads: {threads}");
                while (block.Count > 0)
                {
                    IStackFrame top = block.Pop();
                    renderer.Write("    ");
                    renderer.WriteFrame(top);
                    WriteLine(renderer, "");
                }
                WriteLine(renderer,"}");
            }
        }
        
        static string WithMaxLength(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
}