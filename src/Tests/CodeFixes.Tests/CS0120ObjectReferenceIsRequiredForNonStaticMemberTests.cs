﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Roslynator.Testing.CSharp;
using Xunit;

namespace Roslynator.CSharp.CodeFixes.Tests
{
    public class CS0120ObjectReferenceIsRequiredForNonStaticMemberTests : AbstractCSharpCompilerDiagnosticFixVerifier<ObjectReferenceIsRequiredForNonStaticMemberCodeFixProvider>
    {
        public override string DiagnosticId { get; } = CompilerDiagnosticIdentifiers.CS0120_ObjectReferenceIsRequiredForNonStaticMember;

        [Fact, Trait(Traits.CodeFix, CompilerDiagnosticIdentifiers.CS0120_ObjectReferenceIsRequiredForNonStaticMember)]
        public async Task Test_MakeContainingMethodNonStatic()
        {
            await VerifyFixAsync(@"
class A
{
    public static void M()
    {
        int x = P;
    }

    public int P => 1;
}
", @"
class A
{
    public void M()
    {
        int x = P;
    }

    public int P => 1;
}
", equivalenceKey: EquivalenceKey.Create(DiagnosticId, CodeFixIdentifiers.MakeMemberNonStatic));
        }

        [Fact, Trait(Traits.CodeFix, CompilerDiagnosticIdentifiers.CS0120_ObjectReferenceIsRequiredForNonStaticMember)]
        public async Task TestNoFix()
        {
            await VerifyNoFixAsync(@"
class A
{
    public void M()
    {
        int x = B.P;
    }
}

class B
{
    public int P => 1;
}
", equivalenceKey: EquivalenceKey.Create(DiagnosticId));
        }
    }
}
