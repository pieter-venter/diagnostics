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

//TODO: Flags that could be useful
// CollapseSameFrame - if two subsequent frames are exactly the same, show one (for example, if filtering out signature)
// Remove the "middle" of a very deep stack. Only show top and bottom
// Limit the length (probably different parameter)