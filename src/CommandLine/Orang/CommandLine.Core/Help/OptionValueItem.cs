﻿// This code is originally from https://github.com/josefpihrt/orang. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Roslynator.CommandLine.Help
{
    public class OptionValueItem
    {
        public OptionValueItem(OptionValue value, string text)
        {
            Value = value;
            Text = text;
        }

        public OptionValue Value { get; }

        public string Text { get; }

        public override string ToString() => Text;
    }
}
