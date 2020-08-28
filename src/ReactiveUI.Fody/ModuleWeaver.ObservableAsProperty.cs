// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

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
        internal void ProcessObservableAsPropertyHelper(TypeNode typeNode)
        {
            var typeDefinition = typeNode.TypeDefinition;

            foreach (var method in typeDefinition.Methods.Where(x => x.HasBody))
            {
                ProcessMethod(method);
            }
        }

        private void ProcessMethod(MethodDefinition method)
        {
            method.Body.SimplifyMacros();

            var list = new List<IndexMetadata>();

            bool hasSome = false;

            for (int i = 0; i < method.Body.Instructions.Count; ++i)
            {
                var instruction = method.Body.Instructions[i];
                if (instruction.OpCode != OpCodes.Call)
                {
                    continue;
                }

                if (!(instruction.Operand is MethodReference methodReference))
                {
                    continue;
                }

                if (methodReference.DeclaringType.FullName != "ReactiveUI.Fody.Helpers.ObservableAsPropertyExtensions" && methodReference.Name != "ToFodyProperty")
                {
                    continue;
                }

                hasSome = true;
            }

            if (hasSome == false)
            {
                return;
            }

            WriteWarning("========== Start Method " + method.FullName);

            for (int i = 0; i < method.Body.Instructions.Count; ++i)
            {
                var instruction = method.Body.Instructions[i];

                WriteWarning(instruction.ToString());
            }

            WriteWarning("========== End Method " + method.FullName);

            method.Body.OptimizeMacros();
        }
    }
}
