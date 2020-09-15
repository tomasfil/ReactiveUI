// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil.Cil;

namespace ReactiveUI.Fody
{
    internal class OptionalPatternInstruction : PatternInstruction
    {
        public OptionalPatternInstruction(IReadOnlyList<OpCode> eligibleOpCodes, Func<Instruction, ILProcessor, bool>? predicate = null, bool captureInstruction = false, Func<Instruction, ILProcessor, string?>? getNameFunc = null)
            : base(eligibleOpCodes, predicate, captureInstruction, getNameFunc)
        {
        }

        public OptionalPatternInstruction(OpCode opCode, bool capture = false)
            : base(opCode, null, false)
        {
        }
    }
}
