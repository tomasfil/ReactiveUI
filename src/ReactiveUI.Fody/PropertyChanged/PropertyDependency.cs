// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace ReactiveUI.Fody
{
    internal class PropertyDependency
    {
        public PropertyDependency(PropertyDefinition shouldAlsoNotifyFor, PropertyDefinition? whenPropertyIsSet)
        {
            ShouldAlsoNotifyFor = shouldAlsoNotifyFor;
            WhenPropertyIsSet = whenPropertyIsSet;
        }

        public PropertyDefinition ShouldAlsoNotifyFor { get; }

        public PropertyDefinition? WhenPropertyIsSet { get; }
    }
}
