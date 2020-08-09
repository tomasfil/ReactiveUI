// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using ReactiveUI.Fody.PropertyChanged;

namespace ReactiveUI.Fody
{
    internal static class PropertyWeaver
    {
        public static bool Execute(ModuleWeaver moduleWeaver, PropertyData propertyData, TypeNode typeNode)
        {
            if (propertyData is null)
            {
                throw new ArgumentNullException(nameof(propertyData));
            }

            var property = propertyData.PropertyDefinition;

            var backingField = propertyData.BackingFieldReference;

            var instructions = propertyData.PropertyDefinition.SetMethod.Body.Instructions;

            var method = moduleWeaver.RaiseAndSetIfChangedMethod;

            moduleWeaver.WriteWarning("\t\t" + property.Name);

            if (backingField == null)
            {
                return false;
            }

            if (method == null)
            {
                throw new InvalidOperationException("Invalid RaiseAndSetIfChanged method reference.");
            }

            var indexes = GetIndexes(instructions, backingField);
            indexes.Reverse();

            foreach (var index in indexes)
            {
                AddSimpleInvokerCall(index, instructions, backingField, property, method, typeNode.TypeDefinition);
            }

            GeneratedCodeHelper.MarkAsGeneratedCode(moduleWeaver, propertyData.PropertyDefinition.SetMethod.CustomAttributes);

            return true;
        }

        private static List<(int Index, int Count)> GetIndexes(Collection<Instruction> instructions, FieldReference backingField)
        {
            var setFieldInstructions = FindSetFieldInstructions(instructions, backingField).ToList();
            if (setFieldInstructions.Count == 0)
            {
                throw new Exception("test");
                return new List<(int Index, int Count)> { (instructions.Count - 1, 0) };
            }

            return setFieldInstructions;
        }

        private static IEnumerable<(int Index, int Count)> FindSetFieldInstructions(Collection<Instruction> instructions, FieldReference backingField)
        {
            for (var index = 0; index < instructions.Count; index++)
            {
                var instruction = instructions[index];
                if (instruction.OpCode == OpCodes.Stfld)
                {
                    if (!(instruction.Operand is FieldReference fieldReference1))
                    {
                        continue;
                    }

                    if (fieldReference1.Name == backingField?.Name)
                    {
                        yield return (index, 1);
                    }

                    continue;
                }

                if (instruction.OpCode != OpCodes.Ldflda)
                {
                    continue;
                }

                if (instruction.Next == null)
                {
                    continue;
                }

                if (instruction.Next.OpCode != OpCodes.Initobj)
                {
                    continue;
                }

                if (!(instruction.Operand is FieldReference fieldReference2))
                {
                    continue;
                }

                if (fieldReference2.Name == backingField?.Name)
                {
                    yield return (index, 2);
                }
            }
        }

        private static int AddSimpleInvokerCall((int Index, int Count) indexInfo, Collection<Instruction> instructions, FieldReference backingField, PropertyReference property, MethodReference method, TypeDefinition typeDefinition)
        {
            // Remove the current from the set location.
            for (var i = 0; i < indexInfo.Count; ++i)
            {
                instructions.RemoveAt(indexInfo.Index);
            }

            var genericMethod = new GenericInstanceMethod(method);

            // Set our generic args to the RaiseAndSetIfChanged<TClass, TFieldType>
            genericMethod.GenericArguments.Add(typeDefinition);
            genericMethod.GenericArguments.Add(property.PropertyType);
            method = genericMethod;

            // Generates this.RaiseAndSetIfChanged(this, this.Field, value, "propertyName");
            return instructions.Insert(
                indexInfo.Index,
                Instruction.Create(OpCodes.Ldarg_0), // this -- for the extension method.
                Instruction.Create(OpCodes.Ldarg_0), // this -- for the field reference
                Instruction.Create(OpCodes.Ldflda, backingField), // the field -- uses the previous this instance.
                Instruction.Create(OpCodes.Ldarg_1), // value -- value passed to the set.
                Instruction.Create(OpCodes.Ldstr, property.Name), // The property name.
                Instruction.Create(OpCodes.Call, method), // Call the RaiseAndSetIfChanged with the previous parameters set.
                Instruction.Create(OpCodes.Pop)); // Return the value from RaiseAndSetIfChanged
        }
    }
}
