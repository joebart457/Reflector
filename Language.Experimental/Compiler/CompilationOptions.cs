﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Language.Experimental.Compiler;

public enum OutputTarget
{
    Exe,
    Dll
}

public class CompilationOptions
{
    public string InputPath { get; set; } = "";
    public string EntryPoint { get; set; } = "Start";
    public string AssemblyPath { get; set; } = "";
    public string OutputPath { get; set; } = "";
    public OutputTarget OutputTarget { get; set; } = OutputTarget.Exe;
    public bool EnableOptimizations { get; set; } = false;
    public int OptimizationPasses { get; set; } = 3;
    public bool SourceComments { get; set; } = false;
    public AssemblerOptions AssemblerOptions { get; set; } = new();

}

public class AssemblerOptions
{
    public bool EnableInMemoryAssembly { get; set; }
    public int MemorySize { get; set; } = 100000;
    public int PassesLimit { get; set; } = 100;
}