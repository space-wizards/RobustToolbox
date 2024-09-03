using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Shared.Collections;
using Robust.Shared.Random;

namespace Robust.UnitTesting.Shared.Random;

/// <summary> Instantiable tests for <see cref="RandomExtensions.GetItems{T}(IRobustRandom,IList{T},int,bool)"/>. </summary>
[TestFixture]
public sealed class RandomExtensionsGetItemsWithListTests : RandomExtensionsTests<IList<string>>
{
    /// <inheritdoc />
    protected override IList<string> CreateCollection()
        => new List<string>(CollectionForTests);

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> Invoke(IList<string> collection, int count, bool allowDuplicates)
        => _underlyingRandom.GetItems(collection, count, allowDuplicates);
}

/// <summary> Instantiable tests for <see cref="RandomExtensions.GetItems{T}(IRobustRandom,Span{T},int,bool)"/>. </summary>
[TestFixture]
public sealed class RandomExtensionsGetItemsWithSpanTests : RandomExtensionsTests<string[]>
{
    /// <inheritdoc />
    protected override string[] CreateCollection()
        => CollectionForTests;

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> Invoke(string[] collection, int count, bool allowDuplicates)
    {
        var span = new Span<string>(collection);
        return _underlyingRandom.GetItems(span, count, allowDuplicates)
                                .ToArray();
    }
}

/// <summary> Instantiable tests for <see cref="RandomExtensions.GetItems{T}(IRobustRandom,ValueList{T},int,bool)"/>. </summary>
[TestFixture]
public sealed class RandomExtensionsGetItemsWithValueListTests : RandomExtensionsTests<ValueList<string>>
{
    /// <inheritdoc />
    protected override ValueList<string> CreateCollection()
        => new ValueList<string>(CollectionForTests);

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> Invoke(ValueList<string> collection, int count, bool allowDuplicates)
        => _underlyingRandom.GetItems(collection, count, allowDuplicates)
                            .ToArray();
}

[TestFixture]
public abstract class RandomExtensionsTests<T>
{
    protected IRobustRandom _underlyingRandom = default!;

    protected readonly string[] CollectionForTests = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" };

    private T _collection = default!;

    private int Count => CollectionForTests.Length;

    [SetUp]
    public void Setup()
    {
        _underlyingRandom = Mock.Of<IRobustRandom>();
        _collection = CreateCollection();
    }

    [Test]
    public void GetItems_PickOneFromList_ReturnOfRandomizedIndex()
    {
        // Arrange
        Mock.Get(_underlyingRandom)
            .Setup(x => x.Next(Count))
            .Returns(8);

        // Act
        var result = Invoke(_collection, 1, true);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Single(), Is.EqualTo("8"));
    }


    [Test]
    public void GetItems_PickOneFromListWithoutDuplicates_ReturnOfRandomizedIndex()
    {
        // Arrange
        Mock.Get(_underlyingRandom)
            .Setup(x => x.Next(Count))
            .Returns(8);

        // Act
        var result = Invoke(_collection, 1, allowDuplicates: false);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1));
        Assert.That(result.Single(), Is.EqualTo("8"));
    }

    [Test]
    public void GetItems_PickSomeFromList_ReturnOfRandomizedIndex()
    {
        // Arrange
        Mock.Get(_underlyingRandom)
            .SetupSequence(x => x.Next(Count))
            .Returns(8)
            .Returns(3)
            .Returns(2);

        // Act
        var result = Invoke(_collection, 3, true);

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "8", "3", "2" }));
    }

    [Test]
    public void GetItems_PickSomeFromListWhileRollingDuplicates_ReturnWithDuplicates()
    {
        // Arrange
        Mock.Get(_underlyingRandom)
            .SetupSequence(x => x.Next(Count))
            .Returns(8)
            .Returns(2)
            .Returns(2)
            .Returns(2);

        // Act
        var result = Invoke(_collection, 4, allowDuplicates: true);

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "8", "2", "2", "2" }));
    }

    [Test]
    public void GetItems_PickSameAmountAsOriginalCollection_ReturnWithDuplicates()
    {
        // Arrange
        Mock.Get(_underlyingRandom)
            .SetupSequence(x => x.Next(Count))
            .Returns(0)
            .Returns(2)
            .Returns(2)
            .Returns(4)
            .Returns(6)
            .Returns(5)
            .Returns(4)
            .Returns(3)
            .Returns(2)
            .Returns(1)
            .Returns(0);

        // Act
        var result = Invoke(_collection, 11, allowDuplicates: true);

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "0", "2", "2", "4", "6", "5", "4", "3", "2", "1", "0" }));
    }

    [Test]
    public void GetItems_PickMoreItemsThenOriginalCollectionHave_ReturnWithDuplicates()
    {
        // Arrange
        Mock.Get(_underlyingRandom)
            .SetupSequence(x => x.Next(Count))
            .Returns(0)
            .Returns(2)
            .Returns(2)
            .Returns(4)
            .Returns(6)
            .Returns(5)
            .Returns(4)
            .Returns(3)
            .Returns(2)
            .Returns(1)
            .Returns(9)
            .Returns(9);

        // Act
        var result = Invoke(_collection, 12, allowDuplicates: true);

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "0", "2", "2", "4", "6", "5", "4", "3", "2", "1", "9", "9" }));
    }

    [Test]
    public void GetItems_PickSomeItemsWithoutDuplicates_ReturnWithoutDuplicates()
    {
        // Arrange
        var mock = Mock.Get(_underlyingRandom);
        mock.Setup(x => x.Next(Count)).Returns(1);
        mock.Setup(x => x.Next(Count - 1)).Returns(1);
        mock.Setup(x => x.Next(Count - 2)).Returns(6);

        // Act
        var result = Invoke(_collection, 3, allowDuplicates: false);

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "1", "10", "6" }));
    }

    [Test]
    public void GetItems_PickOneLessItemsThenOriginalCollectionHaveWithoutDuplicates_ReturnWithoutDuplicates()
    {
        // Arrange
        var mock = Mock.Get(_underlyingRandom);
        mock.Setup(x => x.Next(Count)).Returns(1);
        mock.Setup(x => x.Next(Count - 1)).Returns(1);
        mock.Setup(x => x.Next(Count - 2)).Returns(6);
        mock.Setup(x => x.Next(Count - 3)).Returns(6);
        mock.Setup(x => x.Next(Count - 4)).Returns(3);
        mock.Setup(x => x.Next(Count - 5)).Returns(4);
        mock.Setup(x => x.Next(Count - 6)).Returns(4);
        mock.Setup(x => x.Next(Count - 7)).Returns(3);
        mock.Setup(x => x.Next(Count - 8)).Returns(1);
        mock.Setup(x => x.Next(Count - 9)).Returns(1);

        // Act
        var result = Invoke(_collection, 10, allowDuplicates: false);

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "1", "10", "6", "8", "3", "4", "5", "7", "9", "2" }));
    }

    [Test]
    public void GetItems_PickAllItemsWithoutDuplicates_ReturnOriginalCollectionShuffledWithoutDuplicates()
    {
        // Arrange
        var shuffled = new[] { "9", "0", "4", "2", "3", "7", "5", "8", "6", "10", "1" };
        Mock.Get(_underlyingRandom)
            .Setup(x => x.Shuffle(It.IsAny<IList<string>>()))
            .Callback<IList<string>>(x =>
            {
                for (int i = 0; i < shuffled.Length; i++)
                {
                    x[i] = shuffled[i];
                }
            });

        // Act
        var result = Invoke(_collection, 11, allowDuplicates: false);

        // Assert
        Assert.That(result, Is.EqualTo(shuffled));
        Mock.Get(_underlyingRandom).Verify(x => x.Next(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public void GetItems_PickMoreItemsThenOriginalHaveWithoutDuplicates_ReturnOriginalShuffledOriginalCollectionWithoutDuplicates()
    {
        // Arrange
        var shuffled = new[] { "9", "0", "4", "2", "3", "7", "5", "8", "6", "10", "1" };
        Mock.Get(_underlyingRandom)
            .Setup(x => x.Shuffle(It.IsAny<IList<string>>()))
            .Callback<IList<string>>(x =>
            {
                for (int i = 0; i < shuffled.Length; i++)
                {
                    x[i] = shuffled[i];
                }
            });

        // Act
        var result = Invoke(_collection, 30, allowDuplicates: false);

        // Assert
        Assert.That(result, Is.EqualTo(shuffled));
        Mock.Get(_underlyingRandom).Verify(x => x.Next(It.IsAny<int>()), Times.Never);
    }

    /// <summary> Create concrete collection for tests. </summary>
    protected abstract T CreateCollection();

    /// <summary> Invoke method under test. Separate implementation types will have different overrides to be tested. </summary>
    protected abstract IReadOnlyCollection<string> Invoke(T collection, int count, bool allowDuplicates);
}
