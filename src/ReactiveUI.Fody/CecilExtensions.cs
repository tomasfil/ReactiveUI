// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace ReactiveUI.Fody
{
    internal static class CecilExtensions
    {
        public static string GetName(this PropertyDefinition propertyDefinition)
        {
            return $"{propertyDefinition.DeclaringType.FullName}.{propertyDefinition.Name}";
        }

        public static Instruction GetCall(this MethodDefinition method)
        {
            return Instruction.Create(method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method);
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

        public static MethodReference MakeGenericInstance(this MethodReference self, params TypeReference[] arguments)
        {
            var genericInstance = new GenericInstanceMethod(self);

            foreach (var argument in arguments)
            {
                genericInstance.GenericArguments.Add(argument);
            }

            return genericInstance;
        }

        public static void RemoveAttributes(this ICustomAttributeProvider member, params string[] attributeNames)
        {
            if (!member.HasCustomAttributes)
            {
                return;
            }

            var attributes = member.CustomAttributes
                .Where(attribute => attributeNames.Contains(attribute.Constructor.DeclaringType.FullName));

            foreach (var customAttribute in attributes.ToList())
            {
                member.CustomAttributes.Remove(customAttribute);
            }
        }

        /// <summary>
        /// Determines what ammount of entries does <paramref name="instruction"/> pushes onto the evaluation stack.
        /// </summary>
        /// <param name="instruction">The instruction to be analyzed.</param>
        /// <returns>Returns the number of pushed items.</returns>
        public static int GetPushDelta(this Instruction instruction)
        {
            OpCode code = instruction.OpCode;
            switch (code.StackBehaviourPush)
            {
                case StackBehaviour.Push0:
                    return 0;

                case StackBehaviour.Push1:
                case StackBehaviour.Pushi:
                case StackBehaviour.Pushi8:
                case StackBehaviour.Pushr4:
                case StackBehaviour.Pushr8:
                case StackBehaviour.Pushref:
                    return 1;

                case StackBehaviour.Push1_push1:
                    return 2;

                case StackBehaviour.Varpush:
                    if (code.FlowControl == FlowControl.Call)
                    {
                        IMethodSignature method = (IMethodSignature)instruction.Operand;
                        TypeReference @return = method.ReturnType;

                        return IsVoid(@return) ? 0 : 1;
                    }

                    break;
            }

            throw new ArgumentException("Instruction does not have a known stack behavior");
        }

        /// <summary>
        /// Determines what amount of stack entries does <paramref name="instruction"/> pop.
        /// </summary>
        /// <param name="instruction">The instruction.</param>
        /// <returns>Returns the number of popped elements.</returns>
        public static int GetPopDelta(this Instruction instruction)
        {
            OpCode code = instruction.OpCode;
            switch (code.StackBehaviourPop)
            {
                case StackBehaviour.Pop0:
                    return 0;
                case StackBehaviour.Popi:
                case StackBehaviour.Popref:
                case StackBehaviour.Pop1:
                    return 1;

                case StackBehaviour.Pop1_pop1:
                case StackBehaviour.Popi_pop1:
                case StackBehaviour.Popi_popi:
                case StackBehaviour.Popi_popi8:
                case StackBehaviour.Popi_popr4:
                case StackBehaviour.Popi_popr8:
                case StackBehaviour.Popref_pop1:
                case StackBehaviour.Popref_popi:
                    return 2;

                case StackBehaviour.Popi_popi_popi:
                case StackBehaviour.Popref_popi_popi:
                case StackBehaviour.Popref_popi_popi8:
                case StackBehaviour.Popref_popi_popr4:
                case StackBehaviour.Popref_popi_popr8:
                case StackBehaviour.Popref_popi_popref:
                    return 3;

                case StackBehaviour.PopAll:
                    return -1;

                case StackBehaviour.Varpop:
                    if (code.FlowControl == FlowControl.Call)
                    {
                        IMethodSignature method = (IMethodSignature)instruction.Operand;

                        // All method's arguments are already loaded on the stack
                        int count = method.Parameters.Count;

                        if (OpCodes.Newobj.Value != code.Value)
                        {
                            // If the method has target, then it's also loaded on the stack
                            if (method.HasThis)
                            {
                                ++count;
                            }
                        }

                        if (code.Code == Code.Calli)
                        {
                            // The reference to the method should be on the top of the stack.
                            count++;
                        }

                        return count;
                    }

                    // After return the stack is empty.
                    if (code.Code == Code.Ret)
                    {
                        return -1;
                    }

                    break;
            }

            throw new ArgumentException("The instruction is not known which pop delta");
        }

        public static List<IndexMetadata> FindSetFieldInstructions(this Collection<Instruction> instructions, FieldReference backingField)
        {
            var indexes = new List<IndexMetadata>();

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
                        indexes.Add(new IndexMetadata(index, 1));
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
                    indexes.Add(new IndexMetadata(index, 2));
                }
            }

            return indexes.OrderByDescending(x => x.Index).ToList();
        }

        /// <summary>
        /// Binds the generic type definition to a field.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="genericTypeDefinition">The generic type definition.</param>
        /// <returns>The field bound to the generic type.</returns>
        public static FieldReference BindDefinition(this FieldReference field, TypeReference genericTypeDefinition)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (genericTypeDefinition == null)
            {
                throw new ArgumentNullException(nameof(genericTypeDefinition));
            }

            if (!genericTypeDefinition.HasGenericParameters)
            {
                return field;
            }

            var genericDeclaration = new GenericInstanceType(genericTypeDefinition);
            foreach (var parameter in genericTypeDefinition.GenericParameters)
            {
                genericDeclaration.GenericArguments.Add(parameter);
            }

            var reference = new FieldReference(field.Name, field.FieldType, genericDeclaration);
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

        public static IEnumerable<Instruction> AsReverseEnumerable(this Instruction instruction)
        {
            yield return instruction;
            while (instruction.Previous != null)
            {
                yield return instruction = instruction.Previous;
            }
        }

        /// <summary>
        /// Checks if <paramref name="type"/> is void or not.
        /// </summary>
        /// <param name="type">The type to be checked.</param>
        /// <returns>Returns true, if the type is void.</returns>
        private static bool IsVoid(TypeReference type)
        {
            if (type.IsPointer)
            {
                // void * is not considered void as void* represents pointer.
                return false;
            }

            if (type is IModifierType)
            {
                IModifierType? optional = (IModifierType)type;
                return IsVoid(optional.ElementType);
            }

            if (type.FullName == "System.Void")
            {
                return true;
            }

            return false;
        }
    }
}
