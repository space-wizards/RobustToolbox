using NUnit.Framework;
using Robust.Shared.Collections;

namespace Robust.Shared.Tests.Collections;

[TestFixture]
public sealed class StringInternerTest
{
    [Test]
    public void InternsWithinConfiguredBounds()
    {
        var interner = new StringInterner(maximumEntries: 2, maximumLength: 4);
        var first = new string('a', 3);
        var equivalent = new string('a', 3);

        Assert.That(interner.Intern(first), Is.SameAs(first));
        Assert.That(interner.Intern(equivalent), Is.SameAs(first));

        var tooLong = new string('b', 5);
        Assert.That(interner.Intern(tooLong), Is.SameAs(tooLong));

        interner.Intern(new string('c', 3));
        var overCapacity = new string('d', 3);
        Assert.That(interner.Intern(overCapacity), Is.SameAs(overCapacity));
    }

    [Test]
    public void ClearReleasesEntries()
    {
        var interner = new StringInterner(maximumEntries: 1);
        var first = new string('a', 3);
        interner.Intern(first);

        interner.Clear();

        var replacement = new string('a', 3);
        Assert.That(interner.Intern(replacement), Is.SameAs(replacement));
    }
}
