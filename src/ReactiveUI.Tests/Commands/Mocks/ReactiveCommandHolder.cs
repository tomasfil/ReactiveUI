﻿// Copyright (c) 2022 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System.Reactive;

namespace ReactiveUI.Tests
{
    /// <summary>
    /// A ReactiveObject which hosts a ReactiveCommand.
    /// </summary>
    /// <seealso cref="ReactiveUI.ReactiveObject" />
    public class ReactiveCommandHolder : ReactiveObject
    {
        private ReactiveCommand<int, Unit>? _theCommand;

        /// <summary>
        /// Gets or sets the command.
        /// </summary>
        public ReactiveCommand<int, Unit>? TheCommand
        {
            get => _theCommand;
            set => this.RaiseAndSetIfChanged(ref _theCommand, value);
        }
    }
}
