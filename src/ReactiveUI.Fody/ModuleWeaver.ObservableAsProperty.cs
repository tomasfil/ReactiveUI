// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly List<FieldDefinition> PotentiallyUnusedFieldDefinitions = new List<FieldDefinition>();

        private readonly PatternInstruction[] _toFodyPropPatterns = new PatternInstruction[]
        {
            new PatternInstruction(OpCodes.Ldarg_0), // this pointer.
            new PatternInstruction(OpCodes.Ldsfld, (instruction, _) => instruction.Operand is FieldReference funcField &&
                        funcField.FieldType.Name == "Func`2" && funcField.FieldType.Namespace == "System"),
            new PatternInstruction(OpCodes.Dup),
            new PatternInstruction(OpCodes.Brtrue_S),
            new PatternInstruction(OpCodes.Pop),
            new PatternInstruction(OpCodes.Ldsfld, instruction => PotentiallyUnusedFieldDefinitions.Add((FieldDefinition)instruction.Operand)),
            new PatternInstruction(OpCodes.Ldftn, null, GetNameFromAnonymousMethod),
            new PatternInstruction(OpCodes.Newobj),
            new PatternInstruction(OpCodes.Dup),
            new PatternInstruction(OpCodes.Stsfld),
            new OptionalPatternInstruction(OpCodes.Ldarg_0),
            new PatternInstruction(PatternHelper.BooleanOpCodes.Concat(PatternHelper.CallOpCodes).ToArray())
        };

        internal void ProcessObservableAsPropertyHelper(TypeNode typeNode)
        {
            var typeDefinition = typeNode.TypeDefinition;

            foreach (var method in typeDefinition.Methods.Where(x => x.HasBody))
            {
                ProcessMethod(typeNode, method);
            }
        }

        private static string? GetNameFromAnonymousMethod(Instruction instruction, ILProcessor processor)
        {
            throw new NotImplementedException();
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

                var instructions = instruction.AsReverseEnumerable().ToArray();

                var patternInstructions = new List<string>();

                ////FieldReference? foundFuncField = null;
                ////foreach (var patternInstruction in instructions)
                ////{
                ////    patternInstructions.Add(patternInstruction.ToString());

                ////    if (patternInstruction.OpCode == OpCodes.Ldsfld && )
                ////    {
                ////        Console.WriteLine(funcField);

                ////        foundFuncField = funcField;
                ////        break;
                ////    }
                ////}

                ////if (foundFuncField == null)
                ////{
                ////    WriteError($"Method {method.FullName} does not contain a valid Func call to a property and is not therefore eligible for ToFodyProperty().");
                ////    continue;
                ////}

                ////patternInstructions.Reverse();
            }

            method.Body.OptimizeMacros();
        }

        ////private string? GetNameFromExpressionMethod(Instruction anonymousMethodCallInstruction, ILProcessor ilProcessor)
        ////{
        ////    var instruction = ((MethodDefinition)anonymousMethodCallInstruction.Operand).Body.Instructions.Last();
        ////    Instruction? terminalInstruction = null;
        ////    var pattern = PatternHelper.LambdaPropertyFunc;
        ////    var iterator = instruction;
        ////    bool patternIsNotMatched = false;
        ////    Terminal? terminal = null;
        ////    foreach (var patternInstruction in pattern.Reverse())
        ////    {
        ////        if (patternInstruction is OptionalPatternInstruction && !patternInstruction.EligibleOpCodes.Contains(iterator.OpCode))
        ////        {
        ////            continue;
        ////        }

        ////        if (!patternInstruction.EligibleOpCodes.Contains(iterator.OpCode) || !patternInstruction.IsPredicated(iterator, ilProcessor))
        ////        {
        ////            patternIsNotMatched = true;
        ////            break;
        ////        }

        ////        if (patternInstruction.Terminal != null)
        ////        {
        ////            terminalInstruction = iterator;
        ////            terminal = patternInstruction.Terminal;
        ////        }

        ////        iterator = iterator.Previous;
        ////    }

        ////    if (patternIsNotMatched)
        ////    {
        ////        WriteError($"Method {method.FullName} calls into ToFodyProperty but does not have a valid expression to a Property.");
        ////        return null;
        ////    }

        ////    if (terminal == null || terminalInstruction == null)
        ////    {
        ////        WriteError($"Method {method.FullName} calls into ToFodyProperty but does not terminate at a valid Property.");
        ////        return null;
        ////    }

        ////    var propertyName = terminal(terminalInstruction, ilProcessor);
        ////    if (propertyName == null)
        ////    {
        ////        WriteError($"Method {method.FullName} calls into ToFodyProperty but could not find a valid method call.");
        ////        return null;
        ////    }

        ////    return propertyName;
        ////}
    }
}
