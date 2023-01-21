using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

public interface IStackFrame
{
    string AssemblyName { get; }
    string TypeName { get; }
    string MethodName { get; }
    string[] Signature { get; }
    string Text { get; }
}