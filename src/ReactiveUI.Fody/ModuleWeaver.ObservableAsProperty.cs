// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

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
        private static readonly PatternInstruction[] _propertyFuncPatterns = new[]
        {
            new PatternInstruction(new[] { OpCodes.Ldarg, OpCodes.Ldarg_1 }),
            new PatternInstruction(PatternHelper.CallOpCodes, predicate: (inst, _) => inst.Operand is MethodDefinition method && method.IsGetter, getNameFunc: (inst, _) => ((MethodDefinition)inst.Operand).Name.Substring(4)),
            new PatternInstruction(OpCodes.Ret),
        }.Reverse().ToArray();

        private static readonly PatternInstruction[] _toFodyPropPatterns = new[]
        {
            new PatternInstruction(new[] { OpCodes.Ldarg_0, OpCodes.Ldarg }), // This argument.
            new PatternInstruction(OpCodes.Ldsfld, (instruction, _) => instruction.Operand is FieldReference funcField &&
                        funcField.FieldType.Name == "Func`2" && funcField.FieldType.Namespace == "System"), // Func<TObj, TProp> field.
            new PatternInstruction(OpCodes.Dup), // Duplicate the property to test for true/false
            new PatternInstruction(new[] { OpCodes.Brtrue_S, OpCodes.Brtrue }), // Test to make sure its true.
            new PatternInstruction(OpCodes.Pop), // Pop the result of the branch test.
            new PatternInstruction(OpCodes.Ldsfld), // Load static field
            new PatternInstruction(OpCodes.Ldftn, null, true), // Loading native int.
            new PatternInstruction(OpCodes.Newobj),
            new PatternInstruction(OpCodes.Dup),
            new PatternInstruction(OpCodes.Stsfld),
            new OptionalPatternInstruction(OpCodes.Ldarg_0),
            new PatternInstruction(PatternHelper.BooleanOpCodes.Concat(PatternHelper.CallOpCodes).Concat(PatternHelper.LoadFieldOpCodes).ToArray(), captureInstruction: true),
            new PatternInstruction(new[] { OpCodes.Newobj, OpCodes.Ldnull }.Concat(PatternHelper.CallOpCodes).Concat(PatternHelper.LoadFieldOpCodes).ToArray(), captureInstruction: true),
            new PatternInstruction(new[] { OpCodes.Call }, (inst, _) => IsToFodyPropertyInstruction(inst))
        }.Reverse().ToArray();

        internal void ProcessObservableAsPropertyHelper(TypeNode typeNode)
        {
            if (ObservableAsPropertyHelperValueGetMethod is null)
            {
                throw new InvalidOperationException("ObservableAsPropertyHelper.Value method instance is null.");
            }

            if (ObservableAsPropertyHelperType is null)
            {
                throw new InvalidOperationException("ObservableAsPropertyHelper type is null.");
            }

            if (OAPHCreationHelperMixinToPropertyMethod is null)
            {
                throw new InvalidOperationException("OAPHCreationHelper.ToProperty method instance is null.");
            }

            var typeDefinition = typeNode.TypeDefinition;

            var constructors = typeDefinition.Methods.Where(x => x.IsConstructor).ToList();

            foreach (var method in typeDefinition.Methods.Where(x => x.HasBody && !x.IsStatic))
            {
                ProcessMethod(typeNode, method, constructors);
            }
        }

        private static bool IsToFodyPropertyInstruction(Instruction instruction)
        {
            if (instruction.OpCode != OpCodes.Call)
            {
                return false;
            }

            if (!(instruction.Operand is MethodReference methodReference))
            {
                return false;
            }

            return methodReference.DeclaringType.FullName == "ReactiveUI.Fody.Helpers.ObservableAsPropertyExtensions"
                && methodReference.Name == "ToFodyProperty";
        }

        private static List<Instruction> GetValidInstructions(PatternInstruction[] pattern, ILProcessor ilProcessor, Instruction iterator, int i, out bool patternIsNotMatched, out List<string> namesCaptured, out List<IndexMetadata> indexes)
        {
            var captureInstructions = new List<Instruction>();
            indexes = new List<IndexMetadata>();
            namesCaptured = new List<string>();

            int current = i;
            foreach (var patternInstruction in pattern)
            {
                if (patternInstruction is OptionalPatternInstruction && !patternInstruction.IsValid(iterator, ilProcessor))
                {
                    continue;
                }

                if (!patternInstruction.IsValid(iterator, ilProcessor))
                {
                    patternIsNotMatched = true;
                    break;
                }

                if (patternInstruction.CaptureInstruction)
                {
                    captureInstructions.Add(iterator);
                }

                var name = patternInstruction.GetName(iterator, ilProcessor);

                if (name != null && !string.IsNullOrWhiteSpace(name))
                {
                    namesCaptured.Add(name);
                }

                indexes.Add(new IndexMetadata(current, 1));
                iterator = iterator.Previous;
                current--;
            }

            patternIsNotMatched = false;

            return captureInstructions;
        }

        private static IEnumerable<Instruction> FindInitializer(FieldDefinition oldFieldDefinition, List<MethodDefinition> constructors)
        {
            // See if there exists an initializer for the auto-property
            foreach (var constructor in constructors)
            {
                var fieldAssignment = constructor.Body.Instructions.SingleOrDefault(x => Equals(x.Operand, oldFieldDefinition));
                if (fieldAssignment != null)
                {
                    yield return fieldAssignment;
                }
            }
        }

        private void CreateObservable(TypeDefinition typeDefinition, MethodDefinition method, List<IndexMetadata> indexes, PropertyData propertyData, Instruction delaySubscriptionInstruction, Instruction schedulerInstruction, List<MethodDefinition> constructors)
        {
            if (propertyData.BackingFieldReference == null)
            {
                return;
            }

            var instructions = method.Body.Instructions;
            foreach (var index in indexes)
            {
                instructions.RemoveAt(index.Index);
            }

            var oldBackingField = propertyData.BackingFieldReference.Resolve();

            var oaphType = ObservableAsPropertyHelperType.MakeGenericInstanceType(oldBackingField.FieldType);

            var test = FindInitializer(oldBackingField, constructors);

            // Declare a field to store the property value
            var field = new FieldDefinition("$" + propertyData.PropertyDefinition.Name, FieldAttributes.Private, oaphType);
            typeDefinition.Fields.Add(field);

            instructions.Insert(
                indexes.Last().Index,
                Instruction.Create(OpCodes.Ldstr, propertyData.PropertyDefinition.Name), // Property Name
                Instruction.Create(OpCodes.Ldarg_0), // source = this
                Instruction.Create(OpCodes.Ldfld, oldBackingField), // Get old backing field value.
                delaySubscriptionInstruction, // Copy the delay subscription instruction
                schedulerInstruction, // Copy the scheduler subscription
                Instruction.Create(OpCodes.Call, OAPHCreationHelperMixinToPropertyMethod),  // Invoke our OAPH create method.
                Instruction.Create(OpCodes.Stfld, field));

            MakePropertyObservable(propertyData, field, oldBackingField.FieldType);
        }

        private void MakePropertyObservable(PropertyData propertyData, FieldDefinition field, TypeReference oldFieldType)
        {
            propertyData.PropertyDefinition.SetMethod = null;

            var instructions = propertyData.PropertyDefinition.GetMethod.Body.Instructions;

            instructions.Clear();

            instructions.Add(
                Instruction.Create(OpCodes.Ldarg_0), // this pointer.
                Instruction.Create(OpCodes.Ldfld, field), // Load field
                Instruction.Create(OpCodes.Callvirt, ObservableAsPropertyHelperValueGetMethod!.MakeGeneric(oldFieldType)), // Call the .Value
                Instruction.Create(OpCodes.Ret)); // Return the value.
        }

        private void ProcessMethod(TypeNode typeNode, MethodDefinition method, List<MethodDefinition> constructors)
        {
            method.Body.SimplifyMacros();

            var ilProcessor = method.Body.GetILProcessor();

            for (int i = 0; i < method.Body.Instructions.Count; ++i)
            {
                var instruction = method.Body.Instructions[i];
                if (!IsToFodyPropertyInstruction(instruction))
                {
                    continue;
                }

                var captureInstructions = GetValidInstructions(_toFodyPropPatterns, ilProcessor, instruction, i, out var patternIsNotMatched, out var _, out var indexes);

                if (patternIsNotMatched)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but the property couldn't be matched. Therefore it is ineligible for ToFodyProperty weaving.");
                    return;
                }

                if (captureInstructions.Count != 3)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but property couldn't be matched. Therefore it is ineligible for ToFodyProperty weaving.");
                    return;
                }

                var schedulerInstruction = captureInstructions[0];
                var isDelayedInstruction = captureInstructions[1];
                var fieldLoadInstruction = captureInstructions[2];

                if (schedulerInstruction == null || isDelayedInstruction == null || fieldLoadInstruction == null)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but property couldn't be matched. Therefore it is ineligible for ToFodyProperty weaving.");
                    return;
                }

                var name = GetNameFromExpressionMethod(fieldLoadInstruction);

                var propertyData = typeNode.PropertyDatas.FirstOrDefault(x => x.PropertyDefinition.Name == name);

                if (propertyData == null)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but a property couldn't be matched. Therefore it is ineligible ToFodyProperty for weaving.");
                    return;
                }

                if (propertyData.PropertyDefinition.GetMethod == null)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty on property {propertyData.PropertyDefinition.FullName} has no getter and therefore is not suitable for ToFodyProperty weaving.");
                    return;
                }

                if (propertyData.PropertyDefinition.GetMethod.IsStatic)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty on property {propertyData.PropertyDefinition.FullName} which getter is static and therefore is not suitable for ToFodyProperty weaving.");
                    return;
                }

                CreateObservable(typeNode.TypeDefinition, method, indexes, propertyData, isDelayedInstruction, schedulerInstruction, constructors);
            }

            method.Body.OptimizeMacros();
        }

        private string? GetNameFromExpressionMethod(Instruction anonymousMethodCallInstruction)
        {
            var method = anonymousMethodCallInstruction.Operand as MethodDefinition;

            if (method == null)
            {
                return null;
            }

            var index = method.Body.Instructions.Count - 1;

            if (index < 0)
            {
                return null;
            }

            var instruction = method.Body.Instructions[method.Body.Instructions.Count - 1];

            var ilProcessor = method.Body.GetILProcessor();

            GetValidInstructions(_propertyFuncPatterns, ilProcessor, instruction, index, out var patternIsNotMatched, out var namesCaptured, out _);

            if (patternIsNotMatched)
            {
                WriteError($"Method {method.FullName} calls into ToFodyProperty but does not have a valid expression to a Property.");
                return null;
            }

            if (namesCaptured == null || namesCaptured.Count == 0)
            {
                WriteError($"Method {method.FullName} calls into ToFodyProperty but cannot find a valid property name.");
                return null;
            }

            return namesCaptured.Last();
        }
    }
}
