// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ReactiveUI.Fody
{
    internal static class CecilExtensions
    {
        public static string GetName(this PropertyDefinition propertyDefinition)
        {
            return $"{propertyDefinition.DeclaringType.FullName}.{propertyDefinition.Name}";
        }

        public static MethodReference MakeGeneric(this MethodReference self, params TypeReference[] arguments)
        {
            var reference = new MethodReference(self.Name, self.ReturnType)
            {
                HasThis = self.HasThis,
                ExplicitThis = self.ExplicitThis,
                DeclaringType = self.DeclaringType.MakeGenericInstanceType(arguments),
                CallingConvention = self.CallingConvention,
            };

            foreach (var parameter in self.Parameters)
            {
                reference.Parameters.Add(new ParameterDefinition(parameter.ParameterType));
            }

            foreach (var genericParameter in self.GenericParameters)
            {
                reference.GenericParameters.Add(new GenericParameter(genericParameter.Name, reference));
            }

            return reference;
        }

        public static bool ContainsAttribute(this IEnumerable<CustomAttribute> attributes, string attributeName)
        {
            return attributes.Any(attribute => attribute.Constructor.DeclaringType.FullName == attributeName);
        }

        public static IEnumerable<TypeReference> GetAllInterfaces(this TypeDefinition type)
        {
            TypeDefinition? current = type;
            while (current != null)
            {
                if (current.HasInterfaces)
                {
                    foreach (var iface in current.Interfaces)
                    {
                        yield return iface.InterfaceType;
                    }
                }

                current = current.BaseType?.Resolve();
            }
        }
    }
}
