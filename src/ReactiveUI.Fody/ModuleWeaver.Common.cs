// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Common module weaver between the different internal weavers.
    /// </summary>
    public partial class ModuleWeaver
    {
        internal List<TypeNode> ReactiveObjects { get; } = new List<TypeNode>();

        internal MethodReference? RaiseAndSetIfChangedMethod { get; private set; }

        internal MethodReference? DebuggerNonUserCodeAttributeConstructor { get; private set; }

        internal MethodReference? GeneratedCodeAttributeConstructor { get; private set; }

        internal void BuildTypeNodes()
        {
            ReactiveObjects.Clear();

            ReactiveObjects.AddRange(ModuleDefinition
                         .GetTypes()
                         .Where(x => x.IsClass && x.BaseType != null && IsIReactiveObject(x))
                         .Select(x => new TypeNode(x, GetPropertyData(x))));
        }

        internal void GetTypes()
        {
            if (!TryFindTypeDefinition("ReactiveUI.IReactiveObjectExtensions", out var extensionsType))
            {
                throw new InvalidOperationException("Could not find ReactiveUI.IReactiveObjectExtensions");
            }

            var extensionsReference = ModuleDefinition.ImportReference(extensionsType);

            if (extensionsReference == null)
            {
                throw new InvalidOperationException("Could not find ReactiveUI.IReactiveObjectExtensions");
            }

            var raiseAndSetIfChangedMethod = ModuleDefinition.ImportReference(extensionsType.GetMethods().FirstOrDefault(x => x.IsStatic && x.Name == "RaiseAndSetIfChanged"));

            if (raiseAndSetIfChangedMethod == null)
            {
                throw new InvalidOperationException("Could not find RaiseAndSetIfChanged on ReactiveUI.IReactiveObjectExtensions");
            }

            RaiseAndSetIfChangedMethod = raiseAndSetIfChangedMethod;

            var debuggerNonUserCodeType = FindTypeDefinition("System.Diagnostics.DebuggerNonUserCodeAttribute");
            var debuggerNonUserCodeConstructor = debuggerNonUserCodeType.GetConstructors().Single(c => !c.HasParameters);
            DebuggerNonUserCodeAttributeConstructor = ModuleDefinition.ImportReference(debuggerNonUserCodeConstructor);

            var generatedCodeType = FindTypeDefinition("System.CodeDom.Compiler.GeneratedCodeAttribute");
            var generatedCodeAttributeConstructor = generatedCodeType.GetConstructors().Single(c => c.Parameters.Count == 2 && c.Parameters.All(p => p.ParameterType.Name == "String"));
            GeneratedCodeAttributeConstructor = ModuleDefinition.ImportReference(generatedCodeAttributeConstructor);
        }

        private static bool IsIReactiveObject(TypeDefinition typeDefinition)
        {
            return typeDefinition.GetAllInterfaces().Any(x => x.FullName.Equals("ReactiveUI.IReactiveObject", StringComparison.InvariantCulture));
        }

        private static List<PropertyData> GetPropertyData(TypeDefinition typeDefinition)
        {
            var list = new List<PropertyData>(typeDefinition.Properties.Count);

            foreach (var property in typeDefinition.Properties)
            {
                var field = TryGetField(typeDefinition, property);

                var propertyData = new PropertyData(field, property);

                list.Add(propertyData);
            }

            return list;
        }

        private static FieldDefinition? TryGetField(TypeDefinition typeDefinition, PropertyDefinition property)
        {
            var propertyName = property.Name;
            var fieldsWithSameType = typeDefinition.Fields.Where(x => x.DeclaringType == typeDefinition && x.FieldType == property.PropertyType).ToList();
            foreach (var field in fieldsWithSameType)
            {
                // AutoProp
                if (field.Name == $"<{propertyName}>k__BackingField")
                {
                    return field;
                }
            }

            foreach (var field in fieldsWithSameType)
            {
                // diffCase
                var upperPropertyName = propertyName.ToUpper(CultureInfo.InvariantCulture);
                var fieldUpper = field.Name.ToUpper(CultureInfo.InvariantCulture);
                if (fieldUpper == upperPropertyName)
                {
                    return field;
                }

                // underScore
                if (fieldUpper == "_" + upperPropertyName)
                {
                    return field;
                }
            }

            return GetSingleField(property);
        }

        private static FieldDefinition? GetSingleField(PropertyDefinition property)
        {
            var fieldDefinition = GetSingleField(property, Code.Stfld, property.SetMethod);
            if (fieldDefinition != null)
            {
                return fieldDefinition;
            }

            return GetSingleField(property, Code.Ldfld, property.GetMethod);
        }

        private static FieldDefinition? GetSingleField(PropertyDefinition property, Code code, MethodDefinition methodDefinition)
        {
            if (methodDefinition?.Body == null)
            {
                return null;
            }

            FieldReference? fieldReference = null;
            foreach (var instruction in methodDefinition.Body.Instructions)
            {
                if (instruction.OpCode.Code == code)
                {
                    // if fieldReference is not null then we are at the second one
                    if (fieldReference != null)
                    {
                        return null;
                    }

                    if (!(instruction.Operand is FieldReference field))
                    {
                        continue;
                    }

                    if (field.DeclaringType != property.DeclaringType)
                    {
                        continue;
                    }

                    if (field.FieldType != property.PropertyType)
                    {
                        continue;
                    }

                    var fieldDef = instruction.Operand as FieldDefinition;
                    var fieldAttributes = fieldDef?.Attributes & FieldAttributes.InitOnly;
                    if (fieldAttributes == FieldAttributes.InitOnly)
                    {
                        continue;
                    }

                    fieldReference = field;
                }
            }

            return fieldReference?.Resolve();
        }
    }
}
