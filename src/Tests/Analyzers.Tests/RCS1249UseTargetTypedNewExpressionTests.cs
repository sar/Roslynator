// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Roslynator.CSharp.CodeFixes;
using Roslynator.Testing.CSharp;
using Xunit;

namespace Roslynator.CSharp.Analysis.Tests
{
    public class RCS1249UseTargetTypedNewExpressionTests : AbstractCSharpDiagnosticVerifier<UseTargetTypedNewExpressionAnalyzer, UseTargetTypedNewExpressionCodeFixProvider>
    {
        public override DiagnosticDescriptor Descriptor { get; } = DiagnosticRules.UseTargetTypedNewExpression;

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_LocalDeclaration()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        string s = new [|string|](' ', 1);
    }
}
", @"
class C
{
    void M()
    {
        string s = new(' ', 1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_ThrowStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        throw new [|System.Exception|]();
    }
}
", @"
class C
{
    void M()
    {
        throw new();
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_ThrowExpression()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    string M() => throw new [|System.Exception|]();
}
", @"
class C
{
    string M() => throw new();
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_PropertyInitializer()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    string P { get; } = new [|string|](' ', 1);
}
", @"
class C
{
    string P { get; } = new(' ', 1);
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_ReturnStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    string M()
    {
        return new [|string|](' ', 1);
    }
}
", @"
class C
{
    string M()
    {
        return new(' ', 1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_YieldReturnStatement()
        {
            await VerifyDiagnosticAndFixAsync(@"
using System.Collections.Generic;

class C
{
    IEnumerable<string> M()
    {
        yield return new [|string|](' ', 1);
    }
}
", @"
using System.Collections.Generic;

class C
{
    IEnumerable<string> M()
    {
        yield return new(' ', 1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_ArrowExpressionClause()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    string M() => new [|string|](' ', 1);
}
", @"
class C
{
    string M() => new(' ', 1);
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_ArrayInitializer()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        var arr = new string[] { new [|string|](' ', 1) };
    }
}
", @"
class C
{
    void M()
    {
        var arr = new string[] { new(' ', 1) };
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_Assignment()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        string s = null;
        s = new [|string|](' ', 1);
    }
}
", @"
class C
{
    void M()
    {
        string s = null;
        s = new(' ', 1);
    }
}
");
        }

        [Fact, Trait(Traits.Analyzer, DiagnosticIdentifiers.UseTargetTypedNewExpression)]
        public async Task Test_CoalesceExpression()
        {
            await VerifyDiagnosticAndFixAsync(@"
class C
{
    void M()
    {
        string s = null;
        string s2 = null;
        s = s2 ?? new [|string|](' ', 1);
    }
}
", @"
class C
{
    void M()
    {
        string s = null;
        string s2 = null;
        s = s2 ?? new(' ', 1);
    }
}
");
        }
    }
}
