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

        private readonly Random _random = new Random();

        public OaphGeneralTests()
        {
            GeneratePlainOne();
            GenerateWithParamsFilled();
            GeneratePremadeFunc();
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

        public TestModel? ModelTest { get; set; }

        public int IntTest2 { get; set; }

        public object? TestBox { get; set; }

        public int IntTest3 { get; set; }

        public int IntTest4 { get; set; } = 15;

        public int IntTest5 { get; set; }

        public string? StringTest { get; set; }

        public string StringTest2 { get; set; } = "test";

        private void GenerateStringTest()
        {
            Observable.Return("hello").ToFodyProperty(this, x => x.StringTest);
        }

        private void GenerateStringDefaultValue()
        {
            Observable.Return("hello2").ToFodyProperty(this, x => x.StringTest2);
        }

        private void GeneratePlainOne()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest);
        }

        private void GenerateWithParamsFilled()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest2, true, ImmediateScheduler.Instance);
        }

        private void GeneratePremadeFunc()
        {
            Func<OaphGeneralTests, int> toFunc = (method) => method.IntTest3;
            Observable.Return(0).ToFodyProperty(this, toFunc);
        }

        private void GenerateRandomInt()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest4, true, new NewThreadScheduler());
        }

        private void GenerateRandomBool()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest5, GetRandomBool(), new NewThreadScheduler());
        }

        private void GenerateValueFromDelegate()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest5, GetRandomBool(), new NewThreadScheduler());
        }

        private void GenerateValueTestModel()
        {
            Observable.Return<TestModel>(new TestModel(1111)).ToFodyProperty(this, x => x.ModelTest, true, ImmediateScheduler.Instance);
        }

        private void GenerateValueBoxing()
        {
            Observable.Return((object)0).ToFodyProperty(this, x => x.TestBox, GetRandomBool(), new NewThreadScheduler());
        }

        private void GenerateUseBoolProperty()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest5, TestBoolPass, new NewThreadScheduler());
        }

        private void GenerateUseBoolField()
        {
            Observable.Return(0).ToFodyProperty(this, x => x.IntTest5, TestBoolField, new NewThreadScheduler());
        }

        private bool GetRandomBool()
        {
            return _random.Next(0, 2) == 1;
        }

        public class TestModel
        {
            public TestModel(int value)
            {
                Value = value;
            }

            public int Value { get; set; }
        }
    }
}
