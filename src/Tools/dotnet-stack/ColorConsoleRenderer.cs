// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CommandLine;
using System.CommandLine.Rendering;
using Microsoft.Diagnostics.Tools.Stack.Model;

namespace Microsoft.Diagnostics.Tools.Stack
{
    public class ColorConsoleRenderer : RendererBase
    {
        private readonly IConsole _console;

        public ColorConsoleRenderer(IConsole console, int limit = -1) : base(limit)
        {
            _console = console;
        }

        public override void Write(string text)
        {
            Output(text);
        }

        public override void WriteCount(string count)
        {
            OutputWithColor(count, ConsoleColor.Red);
        }

        public override void WriteNamespace(string ns)
        {
            Output(ns);
        }

        public override void WriteType(string type)
        {
            OutputWithColor(type, ConsoleColor.Gray);
        }

        public override void WriteSeparator(string separator)
        {
            //OutputWithColor(separator, ConsoleColor.DarkGray);
            Output(separator);
        }

        public override void WriteDark(string separator)
        {
            Output(separator);
        }

        public override void WriteMethod(string method)
        {
            OutputWithColor(method, ConsoleColor.DarkBlue);
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
        
        private void OutputWithColor(string text, ConsoleColor color)
        {
            var current = Console.ForegroundColor;
            Console.ForegroundColor = color;
            _console.Out.Write(text);
            Console.ForegroundColor = current;
        }
    }
}
