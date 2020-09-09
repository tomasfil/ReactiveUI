// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mono.Cecil.Cil;

namespace ReactiveUI.Fody
{
    internal class PatternInstruction
    {
        private readonly Func<Instruction, ILProcessor, bool> _predicate;

        public PatternInstruction(IReadOnlyList<OpCode> eligibleOpCodes, Func<Instruction, ILProcessor, bool>? predicate = null, Func<Instruction, ILProcessor, string?>? nameFunc = null)
        {
            if (eligibleOpCodes == null)
            {
                throw new ArgumentNullException(nameof(eligibleOpCodes));
            }

            if (eligibleOpCodes.Count == 0)
            {
                throw new ArgumentException("Array length must be greater than zero", nameof(eligibleOpCodes));
            }

            EligibleOpCodes = eligibleOpCodes;
            Name = nameFunc;
            _predicate = predicate ?? PredicateDummy;
        }

        public PatternInstruction(OpCode opCode, Func<Instruction, ILProcessor, bool>? predicate = null, Func<Instruction, ILProcessor, string?>? nameFunc = null)
            : this(new[] { opCode }, predicate, nameFunc)
        {
        }

        public PatternInstruction(OpCode opCode, Action<Instruction> action)
            : this(opCode, null, null)
        {
            Action = action;
        }

        public IReadOnlyList<OpCode> EligibleOpCodes { get; }

        public Action<Instruction>? Action { get; }

        public Func<Instruction, ILProcessor, string?>? Name { get; }

        public bool IsValid(Instruction instruction, ILProcessor ilProcessor)
        {
            if (!EligibleOpCodes.Contains(instruction.OpCode))
            {
                return false;
            }

            try
            {
                return _predicate(instruction, ilProcessor);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool PredicateDummy(Instruction instruction, ILProcessor ilProcessor) => true;
    }
}
