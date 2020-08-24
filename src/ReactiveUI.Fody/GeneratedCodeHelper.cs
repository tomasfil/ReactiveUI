// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Collections.Generic;

namespace ReactiveUI.Fody
{
    internal static class GeneratedCodeHelper
    {
        private static readonly string AssemblyVersion = typeof(ModuleWeaver).Assembly.GetName().Version.ToString();
        private static readonly string AssemblyName = typeof(ModuleWeaver).Assembly.GetName().Name;

        public static void MarkAsGeneratedCode(ModuleWeaver moduleWeaver, Collection<CustomAttribute> customAttributes)
        {
            if (moduleWeaver.DebuggerNonUserCodeAttributeConstructor == null || moduleWeaver.GeneratedCodeAttributeConstructor == null)
            {
                return;
            }

            AddGeneratedCodeAttribute(customAttributes, moduleWeaver.GeneratedCodeAttributeConstructor, moduleWeaver.TypeSystem.StringReference);
            AddDebuggerNonUserCodeAttribute(customAttributes, moduleWeaver.DebuggerNonUserCodeAttributeConstructor);
        }

        private static void AddDebuggerNonUserCodeAttribute(Collection<CustomAttribute> customAttributes, MethodReference debuggerNonUserCodeAttributeConstructor)
        {
            var debuggerAttribute = new CustomAttribute(debuggerNonUserCodeAttributeConstructor);
            customAttributes.Add(debuggerAttribute);
        }

        private static void AddGeneratedCodeAttribute(Collection<CustomAttribute> customAttributes, MethodReference generatedCodeAttributeConstructor, TypeReference stringReference)
        {
            var attribute = new CustomAttribute(generatedCodeAttributeConstructor);
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringReference, AssemblyName));
            attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringReference, AssemblyVersion));
            customAttributes.Add(attribute);
        }
    }
}
