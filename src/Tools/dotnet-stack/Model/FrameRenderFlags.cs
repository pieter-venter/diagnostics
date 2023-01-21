using System;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

[Flags]
public enum FrameRenderFlags
{
    RenderAll,
    HideAssembly,
    HideNamespace,
    HideType,
    HideMethod,
    HideParameters
}