// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using Fody;

using Xunit;

namespace ReactiveUI.Fody.Tests
{
    public class ObservableAsPropertyTests
    {
        private static TestResult _testResult;

        static ObservableAsPropertyTests()
        {
            var moduleWeaver = new ModuleWeaver();
            _testResult = moduleWeaver.ExecuteTestRun("ReactiveUI.Fody.Tests.OAPHAssembly.dll", runPeVerify: false);
        }

        ////[Fact]
        ////public void TestPropertyReturnsFoo()
        ////{
        ////    var model = new ObservableAsTestModel();
        ////    Assert.Equal("foo", model.TestProperty);
        ////}

        [Fact]
        public void AllowObservableAsPropertyAttributeOnAccessor()
        {
            var model = new Issue11TestModel("foo");
            Assert.Equal("foo", model.MyProperty);
        }

        [Fact]
        public void AccessingAChainedObservableAsPropertyOfDoubleDoesntThrow()
        {
            var vm = new Issue13TestModel();
            Assert.Equal(0.0, vm.P2);
        }
    }
}
