using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NUnit.Framework;
using Robust.Analyzers;

using VerifyCS = Microsoft.CodeAnalysis.CSharp.Testing.NUnit.AnalyzerVerifier<Robust.Analyzers.AccessAnalyzer>;
using static Microsoft.CodeAnalysis.Testing.DiagnosticResult;

namespace Robust.Analyzers.Tests;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class AccessAnalyzer_Test
{
    public Task Verifier(string code, params DiagnosticResult[] expected)
    {
        var test = new CSharpAnalyzerTest<AccessAnalyzer, NUnitVerifier>()
        {
            TestState =
            {
                AdditionalReferences = { typeof(AccessAnalyzer).Assembly },
                Sources = { code }
            },
        };

        // ExpectedDiagnostics cannot be set, so we need to AddRange here...
        test.TestState.ExpectedDiagnostics.AddRange(expected);

        return test.RunAsync();
    }

    /*
     *
     */

    [Test]
    public async Task ReadTest()
    {
        const string code = @"
using System;
using Robust.Shared.Analyzers;
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeThisQualifier
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

public struct MyData
{
    public int MyField;

    public static bool operator ==(MyData lhs, MyData rhs) => lhs.MyField == rhs.MyField;
    public static bool operator !=(MyData lhs, MyData rhs) => lhs.MyField != rhs.MyField;
}

[Access(typeof(FriendlyClass),
    Self = AccessPermissions.None,
    Friend = AccessPermissions.None,
    Other = AccessPermissions.None)]
public sealed class TypeNobodyCanRead
{
    public MyData Data = default;

    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.Read,
        Friend = AccessPermissions.Read,
        Other = AccessPermissions.Read)]
    public MyData Data2 = default;

    public void TestTypeNobodyCanRead(TypeNobodyCanRead obj)
    {
        // None of these accesses should be allowed.
        var copy = Data;
        var copy2 = this.Data;
        var copy3 = obj.Data;

        copy = Data;
        copy = this.Data;
        copy = obj.Data;

        var copy4 = Data.MyField;
        var copy5 = this.Data.MyField;
        var copy6 = obj.Data.MyField;

        if (Data == copy) {}
        if (this.Data == copy) {}
        if (obj.Data == copy) {}

        if(Data.MyField == 0) {}
        if(this.Data.MyField == 0) {}
        if(obj.Data.MyField == 0) {}

        // All of these accesses should be fine.
        var copy7 = Data2;
        var copy8 = this.Data2;
        var copy9 = obj.Data2;

        copy = Data2;
        copy = this.Data2;
        copy = obj.Data2;

        var copy10 = Data2.MyField;
        var copy11 = this.Data2.MyField;
        var copy12 = obj.Data2.MyField;

        if (Data2 == copy) {}
        if (this.Data2 == copy) {}
        if (obj.Data2 == copy) {}

        if(Data2.MyField == 0) {}
        if(this.Data2.MyField == 0) {}
        if(obj.Data2.MyField == 0) {}
    }
}

[Access(typeof(FriendlyClass),
    Self = AccessPermissions.Read,
    Friend = AccessPermissions.Read,
    Other = AccessPermissions.Read)]
public sealed class MemberNobodyCanRead
{
    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.None,
        Friend = AccessPermissions.None,
        Other = AccessPermissions.None)]
    public MyData Data = default;

    public MyData Data2 = default;

    public void TestMemberNobodyCanRead(TypeNobodyCanRead obj)
    {
        // None of these accesses should be allowed.
        var copy = Data;
        var copy2 = this.Data;
        var copy3 = obj.Data;

        copy = Data;
        copy = this.Data;
        copy = obj.Data;

        var copy4 = Data.MyField;
        var copy5 = this.Data.MyField;
        var copy6 = obj.Data.MyField;

        if (Data == copy) {}
        if (this.Data == copy) {}
        if (obj.Data == copy) {}

        if(Data.MyField == 0) {}
        if(this.Data.MyField == 0) {}
        if(obj.Data.MyField == 0) {}

        // All of these accesses should be fine.
        var copy7 = Data2;
        var copy8 = this.Data2;
        var copy9 = obj.Data2;

        copy = Data2;
        copy = this.Data2;
        copy = obj.Data2;

        var copy10 = Data2.MyField;
        var copy11 = this.Data2.MyField;
        var copy12 = obj.Data2.MyField;

        if (Data2 == copy) {}
        if (this.Data2 == copy) {}
        if (obj.Data2 == copy) {}

        if(Data2.MyField == 0) {}
        if(this.Data2.MyField == 0) {}
        if(obj.Data2.MyField == 0) {}
    }
}

public sealed class FriendlyClass
{
    public void TestTypeNobodyCanRead(TypeNobodyCanRead obj)
    {
        // We shouldn't be able to access any of these, even if we're a friend..
        var copy = obj.Data;
        copy = obj.Data;

        var copy2 = obj.Data.MyField;
        copy2 = obj.Data.MyField;

        if (obj.Data == copy) {}
        if(obj.Data.MyField == 0) {}

        // We should be allowed to access all of these, we're friends!
        var copy3 = obj.Data2;
        copy = obj.Data2;

        var copy4 = obj.Data2.MyField;
        copy4 = obj.Data2.MyField;

        if(obj.Data2 == copy) {}
        if(obj.Data2.MyField == 0) {}
    }

    public void TestMemberNobodyCanRead(MemberNobodyCanRead obj)
    {
        // We shouldn't be able to access any of these, even if we're a friend..
        var copy = obj.Data;
        copy = obj.Data;

        var copy2 = obj.Data.MyField;
        copy2 = obj.Data.MyField;

        if (obj.Data == copy) {}
        if(obj.Data.MyField == 0) {}

        // We should be allowed to access all of these, we're friends!
        var copy3 = obj.Data2;
        copy = obj.Data2;

        var copy4 = obj.Data2.MyField;
        copy4 = obj.Data2.MyField;

        if(obj.Data2 == copy) {}
        if(obj.Data2.MyField == 0) {}
    }
}

public sealed class OtherClass
{
    public void TestTypeNobodyCanRead(TypeNobodyCanRead obj)
    {
        // We shouldn't be able to access any of these, as 'other types' can't..
        var copy = obj.Data;
        copy = obj.Data;

        var copy2 = obj.Data.MyField;
        copy2 = obj.Data.MyField;

        if (obj.Data == copy) {}
        if(obj.Data.MyField == 0) {}

        // We should be allowed to access all of these, they let others read it!
        var copy3 = obj.Data2;
        copy = obj.Data2;

        var copy4 = obj.Data2.MyField;
        copy4 = obj.Data2.MyField;

        if(obj.Data2 == copy) {}
        if(obj.Data2.MyField == 0) {}
    }

    public void TestMemberNobodyCanRead(MemberNobodyCanRead obj)
    {
        // We shouldn't be able to access any of these, as 'other types' can't..
        var copy = obj.Data;
        copy = obj.Data;

        var copy2 = obj.Data.MyField;
        copy2 = obj.Data.MyField;

        if (obj.Data == copy) {}
        if(obj.Data.MyField == 0) {}

        // We should be allowed to access all of these, they let others read it!
        var copy3 = obj.Data2;
        copy = obj.Data2;

        var copy4 = obj.Data2.MyField;
        copy4 = obj.Data2.MyField;

        if(obj.Data2 == copy) {}
        if(obj.Data2.MyField == 0) {}
    }
}";

        await Verifier(code,
            // AUTO-GENERATED DIAGNOSTICS BELOW //
// /0/Test0.cs(35,20): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(35, 20, 35, 24).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(36,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(36, 21, 36, 30).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(37,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(37, 21, 37, 29).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(39,16): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(39, 16, 39, 20).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(40,16): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(40, 16, 40, 25).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(41,16): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(41, 16, 41, 24).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(43,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(43, 21, 43, 25).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(44,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(44, 21, 44, 30).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(45,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(45, 21, 45, 29).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(47,13): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(47, 13, 47, 17).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(48,13): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(48, 13, 48, 22).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(49,13): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(49, 13, 49, 21).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(51,12): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(51, 12, 51, 16).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(52,12): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(52, 12, 52, 21).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(53,12): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(53, 12, 53, 20).WithArguments("a 'Read' same-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(95,20): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(95, 20, 95, 24).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(96,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(96, 21, 96, 30).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(97,21): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(97, 21, 97, 29).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(99,16): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(99, 16, 99, 20).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(100,16): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(100, 16, 100, 25).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(101,16): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(101, 16, 101, 24).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(103,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(103, 21, 103, 25).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(104,21): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(104, 21, 104, 30).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(105,21): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(105, 21, 105, 29).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(107,13): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(107, 13, 107, 17).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(108,13): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(108, 13, 108, 22).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(109,13): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(109, 13, 109, 21).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(111,12): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(111, 12, 111, 16).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(112,12): error RA0002: Tried to perform a 'Read' same-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(112, 12, 112, 21).WithArguments("a 'Read' same-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(113,12): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(113, 12, 113, 20).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(143,20): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(143, 20, 143, 28).WithArguments("a 'Read' friend-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(144,16): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(144, 16, 144, 24).WithArguments("a 'Read' friend-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(146,21): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(146, 21, 146, 29).WithArguments("a 'Read' friend-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(147,17): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(147, 17, 147, 25).WithArguments("a 'Read' friend-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(149,13): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(149, 13, 149, 21).WithArguments("a 'Read' friend-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(150,12): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(150, 12, 150, 20).WithArguments("a 'Read' friend-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(166,20): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(166, 20, 166, 28).WithArguments("a 'Read' friend-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(167,16): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(167, 16, 167, 24).WithArguments("a 'Read' friend-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(169,21): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(169, 21, 169, 29).WithArguments("a 'Read' friend-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(170,17): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(170, 17, 170, 25).WithArguments("a 'Read' friend-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(172,13): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(172, 13, 172, 21).WithArguments("a 'Read' friend-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(173,12): error RA0002: Tried to perform a 'Read' friend-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(173, 12, 173, 20).WithArguments("a 'Read' friend-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(192,20): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(192, 20, 192, 28).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(193,16): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(193, 16, 193, 24).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(195,21): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(195, 21, 195, 29).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(196,17): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(196, 17, 196, 25).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(198,13): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(198, 13, 198, 21).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(199,12): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'TypeNobodyCanRead', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(199, 12, 199, 20).WithArguments("a 'Read' other-type", "Data", "TypeNobodyCanRead", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(215,20): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(215, 20, 215, 28).WithArguments("a 'Read' other-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(216,16): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(216, 16, 216, 24).WithArguments("a 'Read' other-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(218,21): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(218, 21, 218, 29).WithArguments("a 'Read' other-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(219,17): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(219, 17, 219, 25).WithArguments("a 'Read' other-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(221,13): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(221, 13, 221, 21).WithArguments("a 'Read' other-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(222,12): error RA0002: Tried to perform a 'Read' other-type access to member 'Data' in type 'MemberNobodyCanRead', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(222, 12, 222, 20).WithArguments("a 'Read' other-type", "Data", "MemberNobodyCanRead", "having no", "Member Permissions: ---------")
            );
    }

    [Test]
    public async Task WriteTest()
    {
        const string code = @"
using System;
using Robust.Shared.Analyzers;
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeThisQualifier
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable RedundantDefaultMemberInitializer

public struct MyData
{
    public int MyField;
}

[Access(typeof(FriendlyClass),
    Self = AccessPermissions.None,
    Friend = AccessPermissions.None,
    Other = AccessPermissions.None)]
public sealed class TypeNobodyCanWrite
{
    public MyData Data = default;

    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.Write,
        Friend = AccessPermissions.Write,
        Other = AccessPermissions.Write)]
    public MyData Data2 = default;

    public void TestTypeNobodyCanWrite(TypeNobodyCanWrite obj)
    {
        // None of these accesses should be allowed.
        Data = default;
        this.Data = default;
        obj.Data = default;

        Data.MyField = 0;
        this.Data.MyField = 0;
        obj.Data.MyField = 0;

        // All of these accesses should be fine.
        Data2 = default;
        this.Data2 = default;
        obj.Data2 = default;

        Data2.MyField = 0;
        this.Data2.MyField = 0;
        obj.Data2.MyField = 0;
    }
}

[Access(typeof(FriendlyClass),
    Self = AccessPermissions.Write,
    Friend = AccessPermissions.Write,
    Other = AccessPermissions.Write)]
public sealed class MemberNobodyCanWrite
{
    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.None,
        Friend = AccessPermissions.None,
        Other = AccessPermissions.None)]
    public MyData Data = default;

    public MyData Data2 = default;

    public void TestMemberNobodyCanWrite(TypeNobodyCanWrite obj)
    {
        // None of these accesses should be allowed.
        Data = default;
        this.Data = default;
        obj.Data = default;

        Data.MyField = 0;
        this.Data.MyField = 0;
        obj.Data.MyField = 0;

        // All of these accesses should be fine.
        Data2 = default;
        this.Data2 = default;
        obj.Data2 = default;

        Data2.MyField = 0;
        this.Data2.MyField = 0;
        obj.Data2.MyField = 0;
    }
}

public sealed class FriendlyClass
{
    public void TestTypeNobodyCanWrite(TypeNobodyCanWrite obj)
    {
        // We shouldn't be able to access any of these, even if we're a friend..
        obj.Data = default;
        obj.Data.MyField = 0;

        // We should be allowed to access all of these, we're friends!
        obj.Data2 = default;
        obj.Data2.MyField = 0;
    }

    public void TestMemberNobodyCanWrite(MemberNobodyCanWrite obj)
    {
        // We shouldn't be able to access any of these, even if we're a friend..
        obj.Data = default;
        obj.Data.MyField = 0;

        // We should be allowed to access all of these, we're friends!
        obj.Data2 = default;
        obj.Data2.MyField = 0;
    }
}

public sealed class OtherClass
{
    public void TestTypeNobodyCanWrite(TypeNobodyCanWrite obj)
    {
        // We shouldn't be able to access any of these, as 'other types' can't..
        obj.Data = default;
        obj.Data.MyField = 0;

        // We should be allowed to access all of these, they let others write!
        obj.Data2 = default;
        obj.Data2.MyField = 0;
    }

    public void TestMemberNobodyCanWrite(MemberNobodyCanWrite obj)
    {
        // We shouldn't be able to access any of these, as 'other types' can't..
        obj.Data = default;
        obj.Data.MyField = 0;

        // We should be allowed to access all of these, they let others write!
        obj.Data2 = default;
        obj.Data2.MyField = 0;
    }
}";

        await Verifier(code,
            // AUTO-GENERATED DIAGNOSTICS BELOW //
// /0/Test0.cs(34,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(34, 9, 34, 13).WithArguments("a 'Write' same-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(35,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(35, 9, 35, 18).WithArguments("a 'Write' same-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(36,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(36, 9, 36, 17).WithArguments("a 'Write' same-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(38,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(38, 9, 38, 13).WithArguments("a 'Write' same-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(39,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(39, 9, 39, 18).WithArguments("a 'Write' same-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(40,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(40, 9, 40, 17).WithArguments("a 'Write' same-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(70,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(70, 9, 70, 13).WithArguments("a 'Write' same-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(71,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(71, 9, 71, 18).WithArguments("a 'Write' same-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(72,9): error RA0002: Tried to perform a 'Write' other-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(72, 9, 72, 17).WithArguments("a 'Write' other-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(74,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(74, 9, 74, 13).WithArguments("a 'Write' same-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(75,9): error RA0002: Tried to perform a 'Write' same-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(75, 9, 75, 18).WithArguments("a 'Write' same-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(76,9): error RA0002: Tried to perform a 'Write' other-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(76, 9, 76, 17).WithArguments("a 'Write' other-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(94,9): error RA0002: Tried to perform a 'Write' friend-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(94, 9, 94, 17).WithArguments("a 'Write' friend-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(95,9): error RA0002: Tried to perform a 'Write' friend-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(95, 9, 95, 17).WithArguments("a 'Write' friend-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(105,9): error RA0002: Tried to perform a 'Write' friend-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(105, 9, 105, 17).WithArguments("a 'Write' friend-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(106,9): error RA0002: Tried to perform a 'Write' friend-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(106, 9, 106, 17).WithArguments("a 'Write' friend-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(119,9): error RA0002: Tried to perform a 'Write' other-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(119, 9, 119, 17).WithArguments("a 'Write' other-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(120,9): error RA0002: Tried to perform a 'Write' other-type access to member 'Data' in type 'TypeNobodyCanWrite', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(120, 9, 120, 17).WithArguments("a 'Write' other-type", "Data", "TypeNobodyCanWrite", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(130,9): error RA0002: Tried to perform a 'Write' other-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(130, 9, 130, 17).WithArguments("a 'Write' other-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(131,9): error RA0002: Tried to perform a 'Write' other-type access to member 'Data' in type 'MemberNobodyCanWrite', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(131, 9, 131, 17).WithArguments("a 'Write' other-type", "Data", "MemberNobodyCanWrite", "having no", "Member Permissions: ---------")
            );
    }

    [Test]
    public async Task ExecuteTest()
    {
        const string code = @"
using System;
using Robust.Shared.Analyzers;
// ReSharper disable RedundantAssignment
// ReSharper disable UnusedVariable
// ReSharper disable ArrangeThisQualifier
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable NotAccessedField.Global
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

public struct MyData
{
    public int MyField;
    public void MyMethod() {}
}

[Access(typeof(FriendlyClass),
    Self = AccessPermissions.None,
    Friend = AccessPermissions.None,
    Other = AccessPermissions.None)]
public sealed class TypeNobodyCanExecute
{
    public MyData Data = default;

    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.Execute,
        Friend = AccessPermissions.Execute,
        Other = AccessPermissions.Execute)]
    public MyData Data2 = default;

    public void MyMethod() {}

    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.Execute,
        Friend = AccessPermissions.Execute,
        Other = AccessPermissions.Execute)]
    public void MyMethod2() {}

    public void TestTypeNobodyCanExecute(TypeNobodyCanExecute obj)
    {
        // None of these accesses should be allowed.
        MyMethod();
        this.MyMethod();
        obj.MyMethod();

        Data.MyMethod();
        this.Data.MyMethod();
        obj.Data.MyMethod();

        Data.MyField.ToString();
        this.Data.MyField.ToString();
        obj.Data.MyField.ToString();

        // All of these accesses should be fine.
        MyMethod2();
        this.MyMethod2();
        obj.MyMethod2();

        Data2.MyMethod();
        this.Data2.MyMethod();
        obj.Data2.MyMethod();

        Data2.MyField.ToString();
        this.Data2.ToString();
        obj.Data2.ToString();
    }
}

[Access(typeof(FriendlyClass),
    Self = AccessPermissions.Execute,
    Friend = AccessPermissions.Execute,
    Other = AccessPermissions.Execute)]
public sealed class MemberNobodyCanExecute
{
    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.None,
        Friend = AccessPermissions.None,
        Other = AccessPermissions.None)]
    public MyData Data = default;

    public MyData Data2 = default;

    [Access(typeof(FriendlyClass),
        Self = AccessPermissions.None,
        Friend = AccessPermissions.None,
        Other = AccessPermissions.None)]
    public void MyMethod() {}

    public void MyMethod2() {}

    public void TestMemberNobodyCanExecute(TypeNobodyCanExecute obj)
    {
        // None of these accesses should be allowed.
        MyMethod();
        this.MyMethod();
        obj.MyMethod();

        Data.MyMethod();
        this.Data.MyMethod();
        obj.Data.MyMethod();

        Data.MyField.ToString();
        this.Data.MyField.ToString();
        obj.Data.MyField.ToString();

        // All of these accesses should be fine.
        MyMethod2();
        this.MyMethod2();
        obj.MyMethod2();

        Data2.MyMethod();
        this.Data2.MyMethod();
        obj.Data2.MyMethod();

        Data2.MyField.ToString();
        this.Data2.ToString();
        obj.Data2.ToString();
    }
}

public sealed class FriendlyClass
{
    public void TestTypeNobodyCanExecute(TypeNobodyCanExecute obj)
    {
        // We shouldn't be able to access any of these, even if we're a friend..
        obj.MyMethod();
        obj.Data.MyMethod();
        obj.Data.MyField.ToString();

        // We should be allowed to access all of these, we're friends!
        obj.MyMethod2();
        obj.Data2.MyMethod();
        obj.Data2.MyField.ToString();
    }

    public void TestMemberNobodyCanExecute(MemberNobodyCanExecute obj)
    {
        // We shouldn't be able to access any of these, even if we're a friend..
        obj.MyMethod();
        obj.Data.MyMethod();
        obj.Data.MyField.ToString();

        // We should be allowed to access all of these, we're friends!
        obj.MyMethod2();
        obj.Data2.MyMethod();
        obj.Data2.MyField.ToString();
    }
}

public sealed class OtherClass
{
    public void TestTypeNobodyCanExecute(TypeNobodyCanExecute obj)
    {
        // We shouldn't be able to access any of these, as 'other types' can't..
        obj.MyMethod();
        obj.Data.MyMethod();
        obj.Data.MyField.ToString();

        // We should be allowed to access all of these, they let others Execute!
        obj.MyMethod2();
        obj.Data2.MyMethod();
        obj.Data2.MyField.ToString();
    }

    public void TestMemberNobodyCanExecute(MemberNobodyCanExecute obj)
    {
        // We shouldn't be able to access any of these, as 'other types' can't..
        obj.MyMethod();
        obj.Data.MyMethod();
        obj.Data.MyField.ToString();

        // We should be allowed to access all of these, they let others Execute!
        obj.MyMethod2();
        obj.Data2.MyMethod();
        obj.Data2.MyField.ToString();
    }
}";

        await Verifier(code,
            // AUTO-GENERATED DIAGNOSTICS BELOW //
// /0/Test0.cs(44,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'MyMethod' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(44, 9, 44, 19).WithArguments("an 'Execute' same-type", "MyMethod", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(45,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'MyMethod' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(45, 9, 45, 24).WithArguments("an 'Execute' same-type", "MyMethod", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(46,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'MyMethod' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(46, 9, 46, 23).WithArguments("an 'Execute' same-type", "MyMethod", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(48,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(48, 9, 48, 13).WithArguments("an 'Execute' same-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(49,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(49, 9, 49, 18).WithArguments("an 'Execute' same-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(50,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(50, 9, 50, 17).WithArguments("an 'Execute' same-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(52,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(52, 9, 52, 13).WithArguments("an 'Execute' same-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(53,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(53, 9, 53, 18).WithArguments("an 'Execute' same-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(54,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(54, 9, 54, 17).WithArguments("an 'Execute' same-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(96,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'MyMethod' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(96, 9, 96, 19).WithArguments("an 'Execute' same-type", "MyMethod", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(97,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'MyMethod' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(97, 9, 97, 24).WithArguments("an 'Execute' same-type", "MyMethod", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(98,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'MyMethod' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(98, 9, 98, 23).WithArguments("an 'Execute' other-type", "MyMethod", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(100,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(100, 9, 100, 13).WithArguments("an 'Execute' same-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(101,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(101, 9, 101, 18).WithArguments("an 'Execute' same-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(102,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(102, 9, 102, 17).WithArguments("an 'Execute' other-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(104,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(104, 9, 104, 13).WithArguments("an 'Execute' same-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(105,9): error RA0002: Tried to perform an 'Execute' same-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(105, 9, 105, 18).WithArguments("an 'Execute' same-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(106,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(106, 9, 106, 17).WithArguments("an 'Execute' other-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(128,9): error RA0002: Tried to perform an 'Execute' friend-type access to member 'MyMethod' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(128, 9, 128, 23).WithArguments("an 'Execute' friend-type", "MyMethod", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(129,9): error RA0002: Tried to perform an 'Execute' friend-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(129, 9, 129, 17).WithArguments("an 'Execute' friend-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(130,9): error RA0002: Tried to perform an 'Execute' friend-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(130, 9, 130, 17).WithArguments("an 'Execute' friend-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(141,9): error RA0002: Tried to perform an 'Execute' friend-type access to member 'MyMethod' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(141, 9, 141, 23).WithArguments("an 'Execute' friend-type", "MyMethod", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(142,9): error RA0002: Tried to perform an 'Execute' friend-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(142, 9, 142, 17).WithArguments("an 'Execute' friend-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(143,9): error RA0002: Tried to perform an 'Execute' friend-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(143, 9, 143, 17).WithArguments("an 'Execute' friend-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(157,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'MyMethod' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(157, 9, 157, 23).WithArguments("an 'Execute' other-type", "MyMethod", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(158,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(158, 9, 158, 17).WithArguments("an 'Execute' other-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(159,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'Data' in type 'TypeNobodyCanExecute', despite having no access. Type Permissions: ---------
VerifyCS.Diagnostic().WithSpan(159, 9, 159, 17).WithArguments("an 'Execute' other-type", "Data", "TypeNobodyCanExecute", "having no", "Type Permissions: ---------"),
// /0/Test0.cs(170,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'MyMethod' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(170, 9, 170, 23).WithArguments("an 'Execute' other-type", "MyMethod", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(171,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(171, 9, 171, 17).WithArguments("an 'Execute' other-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------"),
// /0/Test0.cs(172,9): error RA0002: Tried to perform an 'Execute' other-type access to member 'Data' in type 'MemberNobodyCanExecute', despite having no access. Member Permissions: ---------
VerifyCS.Diagnostic().WithSpan(172, 9, 172, 17).WithArguments("an 'Execute' other-type", "Data", "MemberNobodyCanExecute", "having no", "Member Permissions: ---------")
            );
    }
}
