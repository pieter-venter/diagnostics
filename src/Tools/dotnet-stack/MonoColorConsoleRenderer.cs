// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.CommandLine;
using Microsoft.Diagnostics.Tools.Stack.Model;

namespace Microsoft.Diagnostics.Tools.Stack
{
    public class MonoColorConsoleRenderer : RendererBase
    {
        private readonly IConsole _console;

        public MonoColorConsoleRenderer(IConsole console, int limit = -1, FrameRenderFlags frameRenderFlags = FrameRenderFlags.RenderAll) : base(limit, frameRenderFlags)
        {
            _console = console;
        }

        public override void Write(string text)
        {
            Output(text);
        }

        public override void WriteCount(string count)
        {
            Output(count);
        }

        public override void WriteNamespace(string ns)
        {
            Output(ns);
        }

        public override void WriteType(string type)
        {
            Output(type);
        }

        public override void WriteSeparator(string separator)
        {
            Output(separator);
        }

        public override void WriteDark(string separator)
        {
            Output(separator);
        }

        public override void WriteMethod(string method)
        {
            Output(method);
        }

        public override void WriteMethodType(string type)
        {
            Output(type);
        }

        public override void WriteFrameSeparator(string text)
        {
            Output(text);
        }

        public override string FormatTheadId(uint threadID)
        {
            var idInHex = threadID.ToString("x");
            return idInHex;
        }

        private void Output(string text)
        {
            _console.Out.Write(text);
        }
    }
}
