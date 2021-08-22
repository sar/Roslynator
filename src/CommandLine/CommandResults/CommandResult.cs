﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Roslynator.CommandLine
{
    internal class CommandResult
    {
        public CommandResult(CommandStatus status)
        {
            Status = status;
        }

        public CommandStatus Status { get; }
    }
}
