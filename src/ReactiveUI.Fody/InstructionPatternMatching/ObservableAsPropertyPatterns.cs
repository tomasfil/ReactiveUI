// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ReactiveUI.Fody
{
    internal static class ObservableAsPropertyPatterns
    {
        private static readonly OpCode[] _callOpCodes = new[] { OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt };
        private static readonly OpCode[] _loadFieldOpCodes = new[] { OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda };
        private static readonly OpCode[] _loadOpCodes = _loadFieldOpCodes.Concat(new[] { OpCodes.Ldloc, OpCodes.Ldloc_S, OpCodes.Ldloca, OpCodes.Ldloca_S, OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 }).ToArray();
        private static readonly OpCode[] _loadLocalObjectOpCodes = new[] { OpCodes.Ldloc, OpCodes.Ldloc_S, OpCodes.Ldloca, OpCodes.Ldloca_S, OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 };
        private static readonly OpCode[] _loadArgumentObjectOpCodes = new[] { OpCodes.Ldarg, OpCodes.Ldarg_S, OpCodes.Ldarga, OpCodes.Ldarga_S, OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 };
        private static readonly OpCode[] _loadObjectOpCodes = _loadLocalObjectOpCodes.Concat(_loadArgumentObjectOpCodes).ToArray();

        static ObservableAsPropertyPatterns()
        {
            ToFodyProperty = LambdaPropertyFunc.Concat(PassBooleanInstructions).ToArray();

            LambdaPropertyFunc = new[]
            {
                new PatternInstruction(OpCodes.Ldarg_0),
                new PatternInstruction(_callOpCodes, (i, _) => ((MethodDefinition)i.Operand).Name.Substring(4), (i, _) => ((MethodDefinition)i.Operand).IsGetter),
                new OptionalPatternInstruction(OpCodes.Box),
                new OptionalPatternInstruction(OpCodes.Stloc_0),
                new OptionalPatternInstruction(OpCodes.Br_S),
                new OptionalPatternInstruction(OpCodes.Ldloc_0),
                new PatternInstruction(OpCodes.Ret),
            };
        }

        public static PatternInstruction[] ToFodyProperty { get; }

        /// <summary>
        /// Gets the instructions that are generated in a anonymous class when a Func is generated.
        /// </summary>
        public static PatternInstruction[] LambdaPropertyFunc { get; }

        /// <summary>
        /// Gets the instructions for passing a boolean value to a method.
        /// </summary>
        public static PatternInstruction[] PassBooleanInstructions { get; } = new[]
        {
            new PatternInstruction(new[] { OpCodes.Ldc_I4, OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1 })
        };

        public static PatternInstruction[][] PropertyGetterLambdaFuncPatterns { get; } =
        {
            new[]
            {
                new PatternInstruction(OpCodes.Call, (i, p) => ((MethodDefinition)i.Operand).Name.Substring(4), (i, p) => ((MethodDefinition)i.Operand).IsGetter),
                new OptionalPatternInstruction(OpCodes.Box),
            },
            new[]
            {
                new PatternInstruction(_loadObjectOpCodes),
                new PatternInstruction(_callOpCodes, (i, p) => ((MethodDefinition)i.Operand).Name.Substring(4), (i, p) => ((MethodDefinition)i.Operand).IsGetter),
                new OptionalPatternInstruction(OpCodes.Box),
            }
        };
    }
}
