// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Contains the main module weaver for the property changed notifications.
    /// </summary>
    public partial class ModuleWeaver
    {
        internal void ProcessPropertyChangedTypes()
        {
            foreach (var node in ReactiveObjects)
            {
                WriteWarning("Process node: " + node.TypeDefinition?.FullName);
                var typeDefinition = node.TypeDefinition!;

                WriteDebug("\t" + typeDefinition.FullName);

                foreach (var propertyData in node.PropertyDatas.Where(x => x.PropertyDefinition.CustomAttributes.Any(attr => attr.AttributeType.FullName == "ReactiveUI.Fody.Helpers.ReactiveAttribute")))
                {
                    if (propertyData.PropertyDefinition.SetMethod == null)
                    {
                        continue;
                    }

                    if (propertyData.PropertyDefinition.SetMethod.IsStatic)
                    {
                        continue;
                    }

                    WriteWarning("Property Data: " + propertyData.PropertyDefinition.FullName);
                    var body = propertyData.PropertyDefinition.SetMethod.Body;

                    body.SimplifyMacros();

                    if (!PropertyWeaver.Execute(this, propertyData, node))
                    {
                        WriteWarning($"Could not find valid backing field on property {propertyData.PropertyDefinition.FullName}.");
                        continue;
                    }

                    body.InitLocals = true;
                    body.OptimizeMacros();
                }
            }
        }
    }
}
