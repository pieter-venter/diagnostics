// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Diagnostics.Tools.Stack.Model
{
    public abstract class RendererBase : IRenderer
    {
        private readonly int _limit;
        private readonly FrameRenderFlags _frameRenderFlags;

        protected RendererBase(int limit, FrameRenderFlags frameRenderFlags = FrameRenderFlags.RenderAll)
        {
            _limit = limit;
            _frameRenderFlags = frameRenderFlags;
        }

        public int DisplayThreadIDsCountLimit => _limit;

        public abstract void Write(string text);
        public abstract void WriteCount(string count);
        public abstract void WriteNamespace(string ns);
        public abstract void WriteType(string type);
        public abstract void WriteSeparator(string separator);
        public abstract void WriteDark(string separator);
        public abstract void WriteMethod(string method);
        public abstract void WriteMethodType(string type);
        public abstract void WriteFrameSeparator(string text);
        public abstract string FormatTheadId(uint threadID);

        public void WriteFrame(IStackFrame frame)
        {
            if (!string.IsNullOrEmpty(frame.TypeName))
            {
                var namespaces = frame.TypeName.Split('.');

                if (_frameRenderFlags != FrameRenderFlags.HideNamespace)
                {
                    for (int i = 0; i < namespaces.Length - 1; i++)
                    {
                        WriteNamespace(namespaces[i]);
                        WriteSeparator(".");
                    }
                }

                if (_frameRenderFlags != FrameRenderFlags.HideType)
                {
                    WriteMethodType(namespaces[namespaces.Length - 1]);
                    WriteSeparator(".");
                }
            }

            if (_frameRenderFlags != FrameRenderFlags.HideMethod)
            {
                WriteMethod(frame.MethodName);
                WriteSeparator("(");

                if (_frameRenderFlags != FrameRenderFlags.HideParameters)
                {
                    var parameters = frame.Signature;
                    for (int current = 0; current < parameters.Length; current++)
                    {
                        var parameter = parameters[current];
                        // handle byref case
                        var pos = parameter.LastIndexOf(" ByRef");
                        if (pos != -1)
                        {
                            WriteType(parameter.Substring(0, pos));
                            WriteDark(" ByRef");
                        }
                        else
                        {
                            WriteType(parameter);
                        }
                        if (current < parameters.Length - 1) WriteSeparator(", ");
                    }
                }
                WriteSeparator(")");
            }
        }
    }
}
