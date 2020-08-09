// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Mono.Cecil;

namespace ReactiveUI.Fody
{
    internal class PropertyData
    {
        public PropertyData(FieldReference? backingFieldReference, PropertyDefinition propertyDefinition)
        {
            BackingFieldReference = backingFieldReference;
            PropertyDefinition = propertyDefinition;
        }

        public FieldReference? BackingFieldReference { get; }

        public PropertyDefinition PropertyDefinition { get; }

        public MethodReference? EqualsMethod { get; set; }
    }
}
