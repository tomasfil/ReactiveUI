// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Generates the ObservableAsPropertyHelper property helper.
    /// </summary>
    public partial class ModuleWeaver
    {
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsToFodyPropertyInstruction(Instruction instruction) =>
            instruction.OpCode == OpCodes.Call &&
            instruction.Operand is MethodReference methodReference &&
            methodReference.DeclaringType.FullName == "ReactiveUI.Fody.Helpers.ObservableAsPropertyExtensions" &&
            methodReference.Name == "ToFodyProperty";

        private static IEnumerable<InstructionBlock> FindInitializer(FieldDefinition oldFieldDefinition, List<MethodDefinition> constructors)
        {
            // See if there exists an initializer for the auto-property
            foreach (var constructor in constructors)
            {
                var fieldAssignment = constructor.Body.Instructions.SingleOrDefault(x => Equals(x.Operand, oldFieldDefinition));
                if (fieldAssignment == null)
                {
                    continue;
                }

                var value = GetDependentInstructions(constructor, fieldAssignment);

                if (value != null)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Will get all the instructions for the passed in "methodInstruction" to get its required parameters.
        /// It will use the instructions pop/push deltas to determine the instructions.
        /// </summary>
        /// <example>
        /// An example input output would be:
        /// ldc.i4 1
        /// ldc.i4 2
        /// add
        /// 'add' requires two parameters on the evaluation stack. So in this case 'add' would be the methodInstruction
        /// and the two ldc.i4 would be the parameter instructions.
        /// It needs to handle more complex cases like loading field or calling methods to get the instructions.
        /// </example>
        /// <param name="parentMethodDefinition">The parent where the instruction is located.</param>
        /// <param name="methodInstruction">The instruction which we want the parameter instructions for.</param>
        /// <returns>A instruction block which contains the method instruction, and the instructions used for generating its parameters.</returns>
        private static InstructionBlock? GetDependentInstructions(MethodDefinition parentMethodDefinition, Instruction methodInstruction)
        {
            var bodyInstructions = parentMethodDefinition.Body.Instructions;

            if (bodyInstructions == null)
            {
                return null;
            }

            // This method has no parameters from the evaluation stack, return early.
            if (methodInstruction.GetPopDelta() == 0)
            {
                return new InstructionBlock(methodInstruction, parentMethodDefinition, bodyInstructions.IndexOf(methodInstruction));
            }

            // Get the first instruction from the parent method that holds the method instruction.
            var iterator = bodyInstructions[0];

            if (iterator == null)
            {
                return null;
            }

            var evaluationStack = new Stack<InstructionBlock>();
            InstructionBlock? methodBlock = null;
            int index = 0;
            while (iterator != null)
            {
                var currentBlock = new InstructionBlock(iterator, parentMethodDefinition, index);
                var iteratorPopDelta = iterator.GetPopDelta();
                var iteratorPushDelta = iterator.GetPushDelta();

                if (iteratorPopDelta != 0)
                {
                    for (int i = 0; i < iteratorPopDelta; ++i)
                    {
                        if (evaluationStack.Count != 0)
                        {
                            currentBlock.NeededInstructions.Add(evaluationStack.Pop());
                        }
                    }

                    ////currentBlock.NeededInstructions.Reverse();
                }

                if (iteratorPushDelta != 0)
                {
                    evaluationStack.Push(currentBlock);
                }

                if (iterator == methodInstruction)
                {
                    methodBlock = currentBlock;
                    break;
                }

                iterator = iterator.Next;
                index++;
            }

            return methodBlock;
        }

        private void CreateObservable(TypeDefinition typeDefinition, MethodDefinition method, PropertyData propertyData, InstructionBlock propertySetInstructionBlock, List<MethodDefinition> constructors)
        {
            if (propertyData.BackingFieldReference == null)
            {
                return;
            }

            var methodInstructions = method.Body.Instructions;

            int index = 0;
            foreach (var indexMetadata in propertySetInstructionBlock.OrderByDescending(x => x.Index))
            {
                index = indexMetadata.Index;
                methodInstructions.RemoveAt(index);
            }

            var oldBackingField = propertyData.BackingFieldReference.Resolve();

            var oaphType = ObservableAsPropertyHelperType.MakeGenericInstanceType(oldBackingField.FieldType);

            var toPropertyMethodCall = OAPHCreationHelperMixinToPropertyMethod!.MakeGenericInstance(typeDefinition, oldBackingField.FieldType);

            // Declare a field to store the property value
            var field = new FieldDefinition("$" + propertyData.PropertyDefinition.Name, FieldAttributes.Private, oaphType);
            typeDefinition.Fields.Add(field);

            var existingProperties = propertySetInstructionBlock.TakeWhile(x => !IsToFodyPropertyInstruction(x.Instruction)).ToList();

            ////index = methodInstructions.Insert(index, );

            //////index = methodInstructions.Insert(index, setFodyInstructionBlock.GetNeededInstructionEnumerator().Select(x => x.Instruction));

            ////var index = methodInstructions.Insert(
            ////    indexMetadatas.Last().Index,
            ////    Instruction.Create(OpCodes.Ldstr, propertyData.PropertyDefinition.Name), // Property Name
            ////    Instruction.Create(OpCodes.Ldarg_0), // source = this
            ////    Instruction.Create(OpCodes.Ldfld, oldBackingField));

            ////index = methodInstructions.Insert(index, capturedInstructions);

            methodInstructions.Insert(
                index,
                Instruction.Create(OpCodes.Call, toPropertyMethodCall),  // Invoke our ObservableAsPropertyHelpe create method.
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

            var instructions = method.Body.Instructions;

            for (int i = 0; i < instructions.Count; ++i)
            {
                var instruction = instructions[i];
                if (!IsToFodyPropertyInstruction(instruction))
                {
                    continue;
                }

                var propertySetInstruction = instruction.Next;
                if (propertySetInstruction == null || !(propertySetInstruction.Operand is MethodDefinition propertyMethod && propertyMethod.IsSetter))
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but does not set the result to a property. It is therefore ineligible for ToFodyProperty weaving.");
                    return;
                }

                var name = propertyMethod.Name.Substring(4);

                if (string.IsNullOrWhiteSpace(name))
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but could not find property name. It is therefore ineligible for ToFodyProperty weaving.");
                    return;
                }

                var propertyData = typeNode.PropertyDatas.Find(x => x.PropertyDefinition.Name == name);

                if (propertyData == null)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() but property with name {name} couldn't be matched. Make sure that you set ToFodyProperty() to a property. It is ineligible for ToFodyProperty weaving.");
                    return;
                }

                if (propertyData.PropertyDefinition.GetMethod == null)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty on property {name} has no getter and therefore is not suitable for ToFodyProperty weaving.");
                    return;
                }

                if (propertyData.PropertyDefinition.GetMethod.IsStatic)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty on property {name} which getter is static and therefore is not suitable for ToFodyProperty weaving.");
                    return;
                }

                // Get the instructions for the set property.
                var instructionBlock = GetDependentInstructions(method, propertySetInstruction);

                if (instructionBlock == null)
                {
                    WriteError($"Method {method.FullName} calls ToFodyProperty() on property {name} but could not find correct instructions. It is therefore ineligible for ToFodyProperty weaving.");
                    return;
                }

                CreateObservable(typeNode.TypeDefinition, method, propertyData, instructionBlock, constructors);
            }

            method.Body.OptimizeMacros();
        }

        private class InstructionBlock : IEnumerable<InstructionBlock>
        {
            public InstructionBlock(Instruction instruction, MethodDefinition parentMethod, int index)
            {
                Instruction = instruction;
                NeededInstructions = new List<InstructionBlock>(instruction.GetPopDelta());
                Index = index;
                ParentMethod = parentMethod;
            }

            public List<InstructionBlock> NeededInstructions { get; }

            public Instruction Instruction { get; }

            public MethodDefinition ParentMethod { get; }

            public int Index { get; }

            public IEnumerator<InstructionBlock> GetEnumerator()
            {
                foreach (var neededInstruction in NeededInstructions.OrderBy(x => x.Index))
                {
                    foreach (var child in neededInstruction.OrderBy(x => x.Index))
                    {
                        yield return child;
                    }
                }

                yield return this;
            }

            public override string ToString() => $"{Instruction} - ({string.Join(", ", NeededInstructions.Select(x => x.ToString()))})";

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
