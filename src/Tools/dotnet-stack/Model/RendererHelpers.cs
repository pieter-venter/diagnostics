// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.Tools.Stack.Model
{
    public static class RendererHelpers
    {
        public static void Render(this ParallelStack stacks, IRenderer visitor)
        {
            RenderStack(stacks, visitor);
        }

        private const int Padding = 5;
        private static void RenderStack(ParallelStack stack, IRenderer visitor, int increment = 0)
        {
            var alignment = new string(' ', Padding * increment);
            if (stack.Stacks.Count == 0)
            {
                var lastFrame = stack.Frame;
                visitor.Write($"{Environment.NewLine}{alignment}");
                visitor.WriteFrameSeparator($" ~~~~ {FormatThreadIdList(visitor, stack.ThreadIds)}");
                visitor.WriteCount($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,Padding} ");
                visitor.WriteFrame(lastFrame);
                return;
            }

            foreach (var nextStackFrame in stack.Stacks.OrderBy(s => s.ThreadIds.Count))
            {
                RenderStack(nextStackFrame, visitor,
                    (nextStackFrame.ThreadIds.Count == stack.ThreadIds.Count) ? increment : increment + 1);
            }

            var currentFrame = stack.Frame;
            visitor.WriteCount($"{Environment.NewLine}{alignment}{stack.ThreadIds.Count,Padding} ");
            visitor.WriteFrame(currentFrame);
        }

        private static string FormatThreadIdList(IRenderer visitor, List<uint> threadIds)
        {
            var count = threadIds.Count;
            var limit = visitor.DisplayThreadIDsCountLimit;
            limit = Math.Min(count, limit);
            if (limit < 0)
                return string.Join(",", threadIds.Select(tid => visitor.FormatTheadId(tid)));
            else
            {
                var result = string.Join(",", threadIds.GetRange(0, limit).Select(tid => visitor.FormatTheadId(tid)));
                if (count > limit)
                    result += "...";
                return result;
            }
        }
    }
}
