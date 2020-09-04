// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Generates the OAPH property helper.
    /// </summary>
    public partial class ModuleWeaver
    {
        internal void ProcessObservableAsPropertyHelper(TypeNode typeNode)
        {
            var typeDefinition = typeNode.TypeDefinition;

            foreach (var method in typeDefinition.Methods.Where(x => x.HasBody))
            {
                ProcessMethod(typeNode, method);
            }
        }

        private void ProcessMethod(TypeNode typeNode, MethodDefinition method)
        {
            method.Body.SimplifyMacros();

            for (int i = 0; i < method.Body.Instructions.Count; ++i)
            {
                var instruction = method.Body.Instructions[i];
                if (instruction.OpCode != OpCodes.Call)
                {
                    continue;
                }

                if (!(instruction.Operand is MethodReference methodReference))
                {
                    continue;
                }

                if (methodReference.DeclaringType.FullName != "ReactiveUI.Fody.Helpers.ObservableAsPropertyExtensions" && methodReference.Name != "ToFodyProperty")
                {
                    continue;
                }

                var instructions = instruction.AsReverseEnumerable();

                List<Instruction> patternInstructions = new List<Instruction>();
                foreach (var patternInstruction in instructions)
                {
                    if (patternInstruction.OpCode == OpCodes.Newobj && patternInstruction.Operand is MethodReference patternMethod)
                    {
                        break;
                    }

                    patternInstructions.Add(patternInstruction);
                }
            }

            method.Body.OptimizeMacros();
        }

        private string? GetNameFromExpressionMethod(MethodDefinition method, Instruction anonymousMethodCallInstruction, ILProcessor ilProcessor)
        {
            var instruction = ((MethodDefinition)anonymousMethodCallInstruction.Operand).Body.Instructions.Last();
            Instruction? terminalInstruction = null;
            var pattern = ObservableAsPropertyPatterns.LambdaPropertyFunc;
            var iterator = instruction;
            bool patternIsNotMatched = false;
            Terminal? terminal = null;
            foreach (var patternInstruction in pattern.Reverse())
            {
                if (patternInstruction is OptionalPatternInstruction && !patternInstruction.EligibleOpCodes.Contains(iterator.OpCode))
                {
                    continue;
                }

                if (!patternInstruction.EligibleOpCodes.Contains(iterator.OpCode) || !patternInstruction.IsPredicated(iterator, ilProcessor))
                {
                    patternIsNotMatched = true;
                    break;
                }

                if (patternInstruction.Terminal != null)
                {
                    terminalInstruction = iterator;
                    terminal = patternInstruction.Terminal;
                }

                iterator = iterator.Previous;
            }

            if (patternIsNotMatched)
            {
                WriteError($"Method {method.FullName} calls into ToFodyProperty but does not have a valid expression to a Property.");
                return null;
            }

            if (terminal == null || terminalInstruction == null)
            {
                WriteError($"Method {method.FullName} calls into ToFodyProperty but does not terminate at a valid Property.");
                return null;
            }

            var propertyName = terminal(terminalInstruction, ilProcessor);
            if (propertyName == null)
            {
                WriteError($"Method {method.FullName} calls into ToFodyProperty but could not find a valid method call.");
                return null;
            }

            return propertyName;
        }
    }
}
