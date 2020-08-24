// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace ReactiveUI.Fody
{
    internal struct IndexMetadata : IEquatable<IndexMetadata>
    {
        public int Index;
        public int Count;

        public IndexMetadata(int index, int count)
        {
            Index = index;
            Count = count;
        }

        public override bool Equals(object? obj)
        {
            return obj is IndexMetadata other && Equals(other);
        }

        public override int GetHashCode()
        {
            int hashCode = 1655212439;
            hashCode = (hashCode * -1521134295) + Index.GetHashCode();
            hashCode = (hashCode * -1521134295) + Count.GetHashCode();
            return hashCode;
        }

        public bool Equals(IndexMetadata other)
        {
            return Index == other.Index &&
                Count == other.Count;
        }
    }
}
