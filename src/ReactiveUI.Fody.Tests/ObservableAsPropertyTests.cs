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
            _testResult = moduleWeaver.ExecuteTestRun("ReactiveUI.Fody.TestArtifact.OAPHAssembly.dll", runPeVerify: false);
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
            var model = (Issue11TestModel?)_testResult.Assembly.CreateInstance(
                typeof(Issue11TestModel).FullName!,
                false,
                System.Reflection.BindingFlags.Public,
                default,
                new[] { "foo" },
                default,
                default);

            Assert.Equal("foo", model?.MyProperty);
        }

        [Fact]
        public void AccessingAChainedObservableAsPropertyOfDoubleDoesntThrow()
        {
            var vm = _testResult.GetInstance(typeof(Issue13TestModel).FullName!);
            Assert.Equal(0.0, vm.P2);
        }
    }
}
