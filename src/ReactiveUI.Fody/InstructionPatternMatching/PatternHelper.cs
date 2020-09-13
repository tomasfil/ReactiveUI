// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ReactiveUI.Fody
{
    internal static class PatternHelper
    {
        public static OpCode[] LoadOpCodes { get; } = new[] { OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda, OpCodes.Ldloc, OpCodes.Ldloc_S, OpCodes.Ldloca, OpCodes.Ldloca_S, OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 };

        public static OpCode[] LoadFieldOpCodes { get; } = new[] { OpCodes.Ldfld, OpCodes.Ldsfld, OpCodes.Ldflda, OpCodes.Ldsflda };

        public static OpCode[] CallOpCodes { get; } = new[] { OpCodes.Call, OpCodes.Calli, OpCodes.Callvirt };

        public static OpCode[] LoadLocalObjectOpCodes { get; } = new[] { OpCodes.Ldloc, OpCodes.Ldloc_S, OpCodes.Ldloca, OpCodes.Ldloca_S, OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 };

        public static OpCode[] LoadArgumentObjectOpCodes { get; } = new[] { OpCodes.Ldarg, OpCodes.Ldarg_S, OpCodes.Ldarga, OpCodes.Ldarga_S, OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3 };

        public static OpCode[] LoadObjectOpCodes { get; } = new[] { OpCodes.Ldarg, OpCodes.Ldarg_S, OpCodes.Ldarga, OpCodes.Ldarga_S, OpCodes.Ldarg_0, OpCodes.Ldarg_1, OpCodes.Ldarg_2, OpCodes.Ldarg_3, OpCodes.Ldloc, OpCodes.Ldloc_S, OpCodes.Ldloca, OpCodes.Ldloca_S, OpCodes.Ldloc_0, OpCodes.Ldloc_1, OpCodes.Ldloc_2, OpCodes.Ldloc_3 };

        public static OpCode[] BooleanOpCodes { get; } = new[] { OpCodes.Ldc_I4, OpCodes.Ldc_I4_0, OpCodes.Ldc_I4_1 };
    }
}
