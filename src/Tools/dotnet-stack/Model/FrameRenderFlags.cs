using System;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

[Flags]
public enum FrameRenderFlags
{
    RenderAll = 0,
    HideAssembly = 1,
    HideNamespace = 2,
    HideType = 4,
    HideMethod = 8,
    HideParameters = 16
}