// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using Xunit;

namespace ReactiveUI.Fody.Tests.Issues
{
    public class Issue13Tests
    {
        [Fact]
        public void AccessingAChainedObservableAsPropertyOfDoubleDoesntThrow()
        {
            var vm = new VM();
            Assert.Equal(0.0, vm.P2);
        }

        private class VM : ReactiveObject
        {
            public VM()
            {
                Observable.Return(0.0).ToFodyProperty(this, nameof(P1));
                this.WhenAnyValue(vm => vm.P1).ToFodyProperty(this, nameof(P2));
            }

            public double P1 { get; }

            public double P2 { get; }
        }
    }
}
