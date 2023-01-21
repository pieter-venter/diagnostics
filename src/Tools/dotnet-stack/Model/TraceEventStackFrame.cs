using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Diagnostics.Tools.Stack.Model;

public class TraceEventStackFrame : IStackFrame
{
    static Regex regex = new Regex( @"^(.*)!(.*)\((.*)\)$", RegexOptions.Compiled | RegexOptions.NonBacktracking);
    /// <summary>
    /// Constructs a new TraceEventStackFrame. This is an instance of IStackFrame where the data originated
    /// from the TraceEvent library.
    ///
    /// The purpose of this class is to deconstruct a given string that represents a frame back into
    /// semantic parts. This is primarily done so we can render the frame in different ways.
    /// </summary>
    /// <param name="frame">A string representation from the TraceEvent library representing a frame.</param>
    public TraceEventStackFrame(string frame)
    {
        Text = frame;
        ParseFrame(frame);
    }

    void ParseFrame(string frame)
    {
        // TODO: Regex is not great - works, but is slow. We could do better by specifically parsing
        // TODO: Review if there is a better way to get the parts of a frame directly from the TraceEvent library
        Match match = regex.Match(frame);
        if (match.Success)
        {
            AssemblyName = match.Groups[1].Value;
            Signature = match.Groups[3].Value.Trim() == "" ? Array.Empty<string>() : match.Groups[3].Value.Split(",");
            string[] parts = match.Groups[2].Value.Split(".");
            MethodName = parts.Last();
            TypeName = match.Groups[2].Value.Substring(0, match.Groups[2].Value.Length - MethodName.Length - 1);
        }
        else
        {
            AssemblyName = "Unknown";
            Signature = Array.Empty<string>();
            MethodName = "Unknown";
            TypeName = "";
        }
    }

    public string AssemblyName { get; private set; }
    public string TypeName { get; private set; }
    public string MethodName { get; private set; }
    public string[] Signature { get; private set; }
    public string Text { get; }
}