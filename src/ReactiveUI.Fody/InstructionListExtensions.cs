// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace ReactiveUI.Fody
{
    internal static class InstructionListExtensions
    {
        public static int Insert(this IList<Instruction> collection, int index, params Instruction[] instructions)
        {
            foreach (var instruction in instructions.Reverse())
            {
                collection.Insert(index, instruction);
            }

            return index + instructions.Length;
        }

        public static int Insert(this IList<Instruction> collection, int index, IEnumerable<Instruction> instructions)
        {
            foreach (var instruction in instructions.Reverse())
            {
                collection.Insert(index, instruction);
                index++;
            }

            return index;
        }

        public static void Add(this IList<Instruction> collection, params Instruction[] instructions)
        {
            foreach (var instruction in instructions)
            {
                collection.Add(instruction);
            }
        }
    }
}
