using System;
using System.CommandLine;
using System.Collections.Generic;
using System.CommandLine.IO;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

public static class MermaidClassDiagramRenderer
{
    //TODO: Review if outputting to Console is a good idea - or should we output to stream?
    //TODO: Pass an output formatter, so we can manipulate how stacks should be displayed
    public static void Render(ParallelStack stack, IConsole console)
    {
        int id = 1;
        console.Out.WriteLine("classDiagram");
        RenderGraph(console, stack, null, ref id);    
    }
    
     static void RenderGraph(IConsole console, ParallelStack currentFrame, Stack<string> block, ref int id)
        {
            if (currentFrame.Stacks.Count == 0)
            {
                // Last item on frame
                block.Push(currentFrame.Frame.Text);
                RenderBlock(console, currentFrame.ThreadIds.Count, block, id);
                
            } else if (currentFrame.Stacks.Count == 1)
            {
                //console.Out.WriteLine(currentFrame.Frame.Text);
                block.Push(currentFrame.Frame.Text);
                
                // Move one up the stack, using the same block and ID
                RenderGraph(console, currentFrame.Stacks[0], block, ref id);
            }
            else
            {
                // Root does not have a frame value, only child nodes
                if (currentFrame.Frame is not null)
                {
                    // We're at a fork (more than item in Stack property). Push the current frame and render.
                    block.Push(currentFrame.Frame.Text);
                    RenderBlock(console, currentFrame.ThreadIds.Count, block, id);
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
                    block = new Stack<string>();
                    RenderGraph(console, stack, block, ref id);
                    console.Out.WriteLine($"{childId} <|-- {parentId}");
                }   
            }
        }

        static void RenderBlock(IConsole console, int threads, Stack<string> block, int id)
        {
            if (block is not null)
            {
                console.Out.WriteLine($"class {id} {{");
                console.Out.WriteLine($"Number of Threads: {threads}");
                while (block.Count > 0)
                {
                    string top = block.Pop();
                    
                    // To ensure mermaid puts our method in the method block, it must have brackets
                    if (!top.EndsWith(")"))
                    {
                        top += "()";
                    }
                    console.Out.Write("    ");
                    console.Out.WriteLine(top);
                }
                console.Out.WriteLine("}");
            }
        }
        
        static string WithMaxLength(this string value, int maxLength)
        {
            return value?.Substring(0, Math.Min(value.Length, maxLength));
        }
}