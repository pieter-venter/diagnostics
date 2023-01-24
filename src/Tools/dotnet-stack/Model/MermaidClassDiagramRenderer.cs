using System;
using System.CommandLine;
using System.Collections.Generic;
using System.CommandLine.IO;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

public static class MermaidClassDiagramRenderer
{
    //TODO: Remove IConsole interface and only render to IRenderer
    public static void Render(ParallelStack stack, IConsole console, IRenderer renderer)
    {
        int id = 1;
        console.Out.WriteLine("classDiagram");
        RenderGraph(console, renderer, stack, null, ref id);    
    }
    
     static void RenderGraph(IConsole console, IRenderer renderer, ParallelStack currentFrame, Stack<IStackFrame> block, ref int id)
        {
            if (currentFrame.Stacks.Count == 0)
            {
                // Last item on frame
                block.Push(currentFrame.Frame);
                RenderBlock(console, renderer, currentFrame.ThreadIds.Count, block, id);
                
            } else if (currentFrame.Stacks.Count == 1)
            {
                //console.Out.WriteLine(currentFrame.Frame.Text);
                block.Push(currentFrame.Frame);
                
                // Move one up the stack, using the same block and ID
                RenderGraph(console, renderer, currentFrame.Stacks[0], block, ref id);
            }
            else
            {
                // Root does not have a frame value, only child nodes
                if (currentFrame.Frame is not null)
                {
                    // We're at a fork (more than item in Stack property). Push the current frame and render.
                    block.Push(currentFrame.Frame);
                    RenderBlock(console, renderer, currentFrame.ThreadIds.Count, block, id);
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
                    RenderGraph(console, renderer, stack, block, ref id);
                    console.Out.WriteLine($"{childId} <|-- {parentId}");
                }   
            }
        }

        static void RenderBlock(IConsole console, IRenderer renderer, int threads, Stack<IStackFrame> block, int id)
        {
            if (block is not null)
            {
                console.Out.WriteLine($"class {id} {{");
                console.Out.WriteLine($"Number of Threads: {threads}");
                while (block.Count > 0)
                {
                    IStackFrame top = block.Pop();
                    console.Out.Write("    ");
                    renderer.WriteFrame(top);
                    renderer.Write($"{Environment.NewLine}");
                }
                console.Out.WriteLine("}");
            }
        }
        
        static string WithMaxLength(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
}