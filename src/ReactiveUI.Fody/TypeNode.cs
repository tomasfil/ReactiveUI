// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Cecil;

namespace ReactiveUI.Fody
{
    internal class TypeNode
    {
        public TypeNode(TypeDefinition typeDefinition, List<PropertyData> propertyDatas)
        {
            TypeDefinition = typeDefinition ?? throw new ArgumentNullException(nameof(typeDefinition));
            PropertyDatas = propertyDatas?.ToList() ?? throw new ArgumentNullException(nameof(propertyDatas));
        }

        public TypeDefinition TypeDefinition { get; }

        public List<PropertyData> PropertyDatas { get; }
    }
}
