// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Handles the cases where there is a ReactiveDependencyAttribute set on a property.
    /// </summary>
    public partial class ModuleWeaver
    {
        internal void ProcessDependency(TypeNode typeNode, PropertyData propertyData)
        {
            var customAttributes = new List<CustomAttribute>(propertyData.PropertyDefinition.CustomAttributes.Count);
            foreach (var attribute in propertyData.PropertyDefinition.CustomAttributes)
            {
                if (!attribute.AttributeType.FullName.Equals("ReactiveUI.Fody.Helpers.ReactiveDependencyAttribute", StringComparison.InvariantCulture))
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
                WriteError($"Property {propertyData.PropertyDefinition.FullName} has multiple ReactiveDependencyAttribute's and therefore is not suitable for ReactiveDependency weaving.");
                return;
            }

            // If the property already has a body then do not weave to prevent loss of instructions
            if (!propertyData.PropertyDefinition.GetMethod.Body.Instructions.Any(x => x.Operand is FieldReference) || propertyData.PropertyDefinition.GetMethod.Body.HasVariables)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} is not an auto property and therefore is not suitable for ReactiveDependency weaving.");
                return;
            }

            if (propertyData.PropertyDefinition.SetMethod == null)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} has no setter and therefore is not suitable for ReactiveDependency weaving.");
                return;
            }

            if (propertyData.PropertyDefinition.SetMethod.IsStatic)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} is static and therefore is not suitable for ReactiveDependency weaving.");
                return;
            }

            ExecuteDependency(typeNode, propertyData, customAttributes[0]);
        }

        private static void RewriteConstructor(TypeDefinition typeDefinition, FieldReference fieldReference, PropertyDefinition facadeProperty)
        {
            foreach (var constructor in typeDefinition.Methods.Where(x => x.IsConstructor))
            {
                var instructions = constructor.Body.Instructions;
                foreach (var indexMetadata in instructions.FindSetFieldInstructions(fieldReference).ToList())
                {
                    for (int i = 0; i < indexMetadata.Count; ++i)
                    {
                        instructions.RemoveAt(indexMetadata.Index);
                    }

                    // Replace field assignment with a property set (the stack semantics are the same for both,
                    // so happily we don't have to manipulate the bytecode any further.)
                    instructions.Insert(indexMetadata.Index, facadeProperty.SetMethod.GetCall());
                }
            }
        }

        private void WriteGetMethod(PropertyDefinition facadeProperty, PropertyDefinition? objPropertyTarget, FieldDefinition? objFieldTarget, PropertyDefinition destinationProperty)
        {
            if (objPropertyTarget == null && objFieldTarget == null)
            {
                return;
            }

            var targetCall = objPropertyTarget != null ? objPropertyTarget.GetMethod.GetCall() : Instruction.Create(OpCodes.Ldfld, objFieldTarget);
            var destinationCall = destinationProperty.GetMethod.GetCall();

            // Build out the getter which simply returns the value of the generated field
            var instructions = facadeProperty.GetMethod.Body.Instructions;
            instructions.Clear();
            instructions.Add(
                Instruction.Create(OpCodes.Ldarg_0),
                targetCall,
                destinationCall,
                Instruction.Create(OpCodes.Ret));

            GeneratedCodeHelper.MarkAsGeneratedCode(this, facadeProperty.GetMethod.CustomAttributes);
        }

        private void WriteSetMethod(PropertyDefinition facadeProperty, PropertyDefinition? objPropertyTarget, FieldDefinition? objFieldTarget, PropertyDefinition destinationProperty, MethodReference raisePropertyChangedMethod)
        {
            // getter - return this.target
            var targetCall = objPropertyTarget != null ? objPropertyTarget.GetMethod.GetCall() : Instruction.Create(OpCodes.Ldfld, objFieldTarget);

            // setter - this.target.NestedProperty
            var destinationCall = destinationProperty.SetMethod.GetCall();

            // Build out the getter which simply returns the value of the generated field
            var instructions = facadeProperty.SetMethod.Body.Instructions;
            instructions.Clear();

            // Generates
            // this.target.destination = value;
            // this.RaisePropertyChanged("PropertyName");
            instructions.Add(
                Instruction.Create(OpCodes.Ldarg_0),                            // this
                targetCall,                                                     // target
                Instruction.Create(OpCodes.Ldarg_1),                            // value
                destinationCall,                                                // target.destination
                Instruction.Create(OpCodes.Ldarg_0),                            // this
                Instruction.Create(OpCodes.Ldstr, facadeProperty.Name),         // "PropertyName"
                Instruction.Create(OpCodes.Call, raisePropertyChangedMethod),   // "RaisePropertyChanged"
                Instruction.Create(OpCodes.Ret));                               // return

            GeneratedCodeHelper.MarkAsGeneratedCode(this, facadeProperty.SetMethod.CustomAttributes);
        }

        private bool ExecuteDependency(TypeNode typeNode, PropertyData propertyData, CustomAttribute attribute)
        {
            if (RaisePropertyChanged == null)
            {
                throw new InvalidOperationException("RaisePropertyChanged does not exist.");
            }

            if (propertyData.BackingFieldReference == null)
            {
                return false;
            }

            var backingField = propertyData.BackingFieldReference.Resolve();

            var facadeProperty = propertyData.PropertyDefinition;

            var typeDefinition = typeNode.TypeDefinition;

            var targetNamedArgument = attribute.ConstructorArguments.FirstOrDefault();
            var targetValue = targetNamedArgument.Value?.ToString();
            if (string.IsNullOrEmpty(targetValue))
            {
                WriteError($"ReactiveDependencyAttribute on {facadeProperty.FullName} has no target property defined on the object and is therefore unsuitable for ReactiveDependency weaving.");
                return false;
            }

            var objPropertyTarget = typeDefinition.Properties.FirstOrDefault(x => x.Name == targetValue);
            var objFieldTarget = typeDefinition.Fields.FirstOrDefault(x => x.Name == targetValue);

            if (objPropertyTarget == null && objFieldTarget == null)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} dependency {targetValue} not found on type {typeDefinition.FullName}.");
                return false;
            }

            var objDependencyTargetType = objPropertyTarget != null
                    ? objPropertyTarget.PropertyType.Resolve()
                    : objFieldTarget?.FieldType.Resolve();

            if (objDependencyTargetType == null)
            {
                WriteError($"Property {propertyData.PropertyDefinition.FullName} dependency {targetValue} could not resolve to a type.");
                return false;
            }

            // Look for the target property on the member obj
            var destinationPropertyNamedArgument = attribute.Properties.FirstOrDefault(x => x.Name == "TargetProperty");
            var destinationPropertyName = destinationPropertyNamedArgument.Argument.Value?.ToString();

            // If no target property was specified use this property's name as the target on the decorated object (ala a decorated property)
            if (string.IsNullOrEmpty(destinationPropertyName))
            {
                destinationPropertyName = facadeProperty.Name;
            }

            var destinationProperty = objDependencyTargetType.Properties.First(x => x.Name == destinationPropertyName);

            if (destinationProperty == null)
            {
                WriteError($"Property {typeDefinition.DeclaringType.FullName}.{typeDefinition.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveDecorator].");
                return false;
            }

            // The property on the dependency should have a setter e.g. Dependency.SomeProperty = value;
            if (destinationProperty.SetMethod == null)
            {
                WriteError($"Dependency object's property {destinationProperty.DeclaringType.FullName}.{destinationProperty.Name} has no setter, therefore it is not possible for the property to change, and thus should not be marked with [ReactiveDecorator]");
                return false;
            }

            if (!typeDefinition.Fields.Remove(backingField))
            {
                WriteError($"Backing Field {backingField.FullName} has not been removed.");
                return false;
            }

            // See if there exists an initializer for the auto-property
            RewriteConstructor(typeDefinition, backingField, facadeProperty);

            WriteGetMethod(facadeProperty, objPropertyTarget, objFieldTarget, destinationProperty);

            WriteSetMethod(facadeProperty, objPropertyTarget, objFieldTarget, destinationProperty, RaisePropertyChanged);

            return true;
        }
    }
}
