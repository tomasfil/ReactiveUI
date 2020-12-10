// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

using ReactiveUI.Fody.Helpers;

namespace ReactiveUI.Fody.OAPHAssembly
{
    public class OaphGeneralTests : ReactiveObject
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "For the test")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "For the test")]
        public bool TestBoolField;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "For the test")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "For the test")]
        public int Test = 10;

        private readonly Random _random = new Random();

        private ObservableAsPropertyHelper<int>? _nonFodyIntTest;

        public OaphGeneralTests()
        {
            GeneratePlainOne();
            GenerateWithParamsFilled();
            GenerateRandomInt();
            GenerateRandomBool();
            GenerateValueFromDelegate();
            GenerateValueTestModel();
            GenerateValueBoxing();
            GenerateStringTest();
            GenerateUseBoolField();
            GenerateUseBoolProperty();
        }

        public bool TestBoolPass { get; set; }

        public int IntTest { get; set; }

        public int NonFodyIntTest => _nonFodyIntTest?.Value ?? 0;

        public TestModel? ModelTest { get; set; }

        public int IntTest2 { get; set; }

        public object? TestBox { get; set; }

        public int IntTest3 { get; set; }

        public int IntTest4 { get; set; } = 15;

        public int IntTest5 { get; set; }

        public int IntTest6 { get; set; }

        public int IntTest7 { get; set; }

        public string? StringTest { get; set; }

        public string StringTest2 { get; set; } = "test";

        private void GenerateStringTest() => StringTest = Observable.Return("hello").ToFodyProperty();

        private void GenerateStringDefaultValue() => StringTest2 = Observable.Return("hello2").ToFodyProperty();

        private void GeneratePlainOne() => IntTest = Observable.Return(0).ToFodyProperty();

        private void GenerateNonFodyPlainOne() => _nonFodyIntTest = Observable.Return(0).ToProperty(this, nameof(NonFodyIntTest), initialValue: Test);

        private void GenerateWithParamsFilled() => IntTest2 = Observable.Return(0).ToFodyProperty(deferSubscription: true, scheduler: ImmediateScheduler.Instance);

        private void GenerateRandomInt() => IntTest3 = Observable.Return(0).ToFodyProperty(deferSubscription: true, scheduler: new NewThreadScheduler());

        private void GenerateRandomBool() => IntTest4 = Observable.Return(0).ToFodyProperty(GetRandomBool(), new NewThreadScheduler());

        private void GenerateValueFromDelegate() => IntTest5 = Observable.Return(0).ToFodyProperty(GetRandomBool(), scheduler: new NewThreadScheduler());

        private void GenerateValueTestModel() => ModelTest = Observable.Return<TestModel>(new TestModel(1111)).ToFodyProperty(deferSubscription: true, scheduler: ImmediateScheduler.Instance);

        private void GenerateValueBoxing() => TestBox = Observable.Return((object)0).ToFodyProperty(GetRandomBool(), scheduler: new NewThreadScheduler());

        private void GenerateUseBoolProperty() => IntTest6 = Observable.Return(0).ToFodyProperty(deferSubscription: TestBoolPass, scheduler: new NewThreadScheduler());

        private void GenerateUseBoolField() => IntTest7 = Observable.Return(0).ToFodyProperty(deferSubscription: TestBoolField, scheduler: new NewThreadScheduler());

        private bool GetRandomBool() => _random.Next(0, 2) == 1;

        public class TestModel
        {
            public TestModel(int value) => Value = value;

            public int Value { get; set; }
        }
    }
}
