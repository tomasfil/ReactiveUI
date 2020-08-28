// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil.Cil;

namespace ReactiveUI.Fody.InstructionPatternMatching
{
    internal class PatternInstruction
    {
        private readonly Predicate _predicate;

        public PatternInstruction(OpCode[] eligibleOpCodes, Terminal? terminal = null, Predicate? predicate = null)
        {
            if (eligibleOpCodes == null)
            {
                throw new ArgumentNullException(nameof(eligibleOpCodes));
            }

            if (eligibleOpCodes.Length == 0)
            {
                throw new ArgumentException("Array length must be greater than zero", nameof(eligibleOpCodes));
            }

            EligibleOpCodes = eligibleOpCodes;
            Terminal = terminal;
            _predicate = predicate ?? PredicateDummy;
        }

        public PatternInstruction(OpCode opCode, Terminal? terminal = null, Predicate? predicate = null)
            : this(new[] { opCode }, terminal, predicate)
        {
        }

        public PatternInstruction(OpCode opCode, Action<Instruction> action)
            : this(opCode, null, null)
        {
            Action = action;
        }

        public OpCode[] EligibleOpCodes { get; }

        public Action<Instruction> Action { get; }

        public Terminal Terminal { get; }

        public bool IsPredicated(Instruction instruction, ILProcessor ilProcessor)
        {
            try
            {
                return _predicate(instruction, ilProcessor);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static bool PredicateDummy(Instruction instruction, ILProcessor ilProcessor) => true;
    }
}
