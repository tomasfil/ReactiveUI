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
using Mono.Collections.Generic;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Processes property changed events.
    /// </summary>
    public partial class ModuleWeaver
    {
        internal void ProcessPropertyChanged(TypeNode typeNode, PropertyData propertyData)
        {
            var customAttributes = new List<CustomAttribute>(propertyData.PropertyDefinition.CustomAttributes.Count);
            foreach (var attribute in propertyData.PropertyDefinition.CustomAttributes)
            {
                if (!attribute.AttributeType.FullName.Equals("ReactiveUI.Fody.Helpers.ReactiveAttribute", StringComparison.InvariantCulture))
                {
                    continue;
                }

                customAttributes.Add(attribute);
            }

            if (customAttributes.Count == 0)
            {
                return;
            }

            if (customAttributes.Count != 1)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} has multiple ReactiveAttribute's and therefore is not suitable for Reactive property changed weaving.");
                return;
            }

            if (propertyData.PropertyDefinition.SetMethod == null)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} has no setter and therefore is not suitable for ReactiveAttribute weaving.");
                return;
            }

            if (propertyData.PropertyDefinition.SetMethod.IsStatic)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} is static and therefore is not suitable for ReactiveAttribute weaving.");
                return;
            }

            if (propertyData.BackingFieldReference == null)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} has no valid backing field and therefore is not suitable for ReactiveAttribute weaving.");
            }

            ExecutePropertyChanged(propertyData, typeNode.TypeDefinition);
        }

        private static int AddSimpleInvokerCall(IndexMetadata indexMetadata, Collection<Instruction> instructions, FieldReference backingField, PropertyReference property, MethodReference method, TypeDefinition typeDefinition)
        {
            // Remove the current from the set location.
            for (var i = 0; i < indexMetadata.Count; ++i)
            {
                instructions.RemoveAt(indexMetadata.Index);
            }

            var genericMethod = new GenericInstanceMethod(method);

            // Set our generic args to the RaiseAndSetIfChanged<TClass, TFieldType>
            genericMethod.GenericArguments.Add(typeDefinition);
            genericMethod.GenericArguments.Add(property.PropertyType);
            method = genericMethod;

            // Generates this.RaiseAndSetIfChanged(this, this.Field, value, "propertyName");
            return instructions.Insert(
                indexMetadata.Index,
                Instruction.Create(OpCodes.Ldarg_0), // this -- for the extension method.
                Instruction.Create(OpCodes.Ldarg_0), // this -- for the field reference
                Instruction.Create(OpCodes.Ldflda, backingField), // the field -- uses the previous this instance.
                Instruction.Create(OpCodes.Ldarg_1), // value -- value passed to the set.
                Instruction.Create(OpCodes.Ldstr, property.Name), // The property name.
                Instruction.Create(OpCodes.Call, method), // Call the RaiseAndSetIfChanged with the previous parameters set.
                Instruction.Create(OpCodes.Pop)); // Return the value from RaiseAndSetIfChanged
        }

        private bool ExecutePropertyChanged(PropertyData propertyData, TypeDefinition typeDefinition)
        {
            var property = propertyData.PropertyDefinition;

            var backingField = propertyData.BackingFieldReference;

            var instructions = propertyData.PropertyDefinition.SetMethod.Body.Instructions;

            var method = RaiseAndSetIfChangedMethod;

            if (backingField == null)
            {
                return false;
            }

            if (method == null)
            {
                throw new InvalidOperationException("Invalid RaiseAndSetIfChanged method reference.");
            }

            var indexes = instructions.FindSetFieldInstructions(backingField);
            indexes.Reverse();

            foreach (var index in indexes)
            {
                AddSimpleInvokerCall(index, instructions, backingField, property, method, typeDefinition);
            }

            return true;
        }
    }
}
