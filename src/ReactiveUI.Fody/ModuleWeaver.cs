// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Fody;

namespace ReactiveUI.Fody
{
    /// <summary>
    /// Contains the main module weaver for the ReactiveUI Fodys.
    /// </summary>
    public partial class ModuleWeaver : BaseModuleWeaver
    {
        /// <inheritdoc/>
        public override void Execute()
        {
            GetTypes();
            BuildTypeNodes();
            ProcessPropertyChangedTypes();
        }

        /// <inheritdoc/>
        public override IEnumerable<string> GetAssembliesForScanning()
        {
            yield return "mscorlib";
            yield return "System";
            yield return "System.Runtime";
            yield return "System.Core";
            yield return "netstandard";
            yield return "System.Collections";
            yield return "System.ObjectModel";
            yield return "System.Threading";
            yield return "FSharp.Core";
            yield return "ReactiveUI";
            yield return "ReactiveUI.Fody.Helpers";

            // TODO: remove when move to only netstandard2.0
            yield return "System.Diagnostics.Tools";
            yield return "System.Diagnostics.Debug";
        }
    }
}
