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
        private readonly Func<Instruction, ILProcessor, string?>? _getName;

        public PatternInstruction(IReadOnlyList<OpCode> eligibleOpCodes, Func<Instruction, ILProcessor, bool>? predicate = null, bool captureInstruction = false, Func<Instruction, ILProcessor, string?>? getNameFunc = null)
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
            CaptureInstruction = captureInstruction;
            _predicate = predicate ?? PredicateDummy;
            _getName = getNameFunc;
        }

        public PatternInstruction(OpCode opCode, Func<Instruction, ILProcessor, bool>? predicate = null, bool captureInstruction = false)
            : this(new[] { opCode }, predicate, captureInstruction)
        {
        }

        public PatternInstruction(OpCode opCode, Action<Instruction> action)
            : this(opCode, null, false) =>
            Action = action;

        public IReadOnlyList<OpCode> EligibleOpCodes { get; }

        public Action<Instruction>? Action { get; }

        public bool CaptureInstruction { get; }

        public bool HasNameFunc => _getName != null;

        public string? GetName(Instruction instruction, ILProcessor processor) => _getName?.Invoke(instruction, processor);

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

        public override string ToString() => string.Join(", ", EligibleOpCodes.Select(x => x.ToString()));

        private bool PredicateDummy(Instruction instruction, ILProcessor ilProcessor) => true;
    }
}
