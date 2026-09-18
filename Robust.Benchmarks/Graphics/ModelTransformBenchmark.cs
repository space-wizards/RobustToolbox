using System.Numerics;
using BenchmarkDotNet.Attributes;

namespace Robust.Benchmarks.Graphics;

[MemoryDiagnoser]
public class ModelTransformBenchmark
{
    private readonly Vector2 _bottomLeft = new(-64f, -32f);
    private readonly Vector2 _bottomRight = new(64f, -32f);
    private readonly Vector2 _topRight = new(64f, 32f);
    private Matrix3x2 _currentModel;
    private Vector2 _currentModelTranslation;
    private bool _currentModelIsTranslation;
    private Matrix3x2 _translationModel;

    [GlobalSetup]
    public void Setup()
    {
        _translationModel = Matrix3x2.CreateTranslation(420f, 69f);
        SetModelTransform(_translationModel);
    }

    [Benchmark]
    public Vector2 CacheAndTranslate()
    {
        SetModelTransform(_translationModel);
        return TransformWithCachedPath();
    }

    [Benchmark]
    public Vector2 Translate()
    {
        var translation = new Vector2(_translationModel.M31, _translationModel.M32);
        var bottomLeft = _bottomLeft + translation;
        var bottomRight = _bottomRight + translation;
        var topRight = _topRight + translation;
        return topRight + bottomLeft - bottomRight;
    }

    [Benchmark(Baseline = true)]
    public Vector2 AlwaysTransform()
    {
        var bottomLeft = Vector2.Transform(_bottomLeft, _translationModel);
        var bottomRight = Vector2.Transform(_bottomRight, _translationModel);
        var topRight = Vector2.Transform(_topRight, _translationModel);
        return topRight + bottomLeft - bottomRight;
    }

    private void SetModelTransform(in Matrix3x2 matrix)
    {
        _currentModel = matrix;
        _currentModelTranslation = new Vector2(matrix.M31, matrix.M32);
        _currentModelIsTranslation = matrix.M11 == 1f && matrix.M12 == 0f &&
                                     matrix.M21 == 0f && matrix.M22 == 1f;
    }

    private Vector2 TransformWithCachedPath()
    {
        Vector2 bottomLeft;
        Vector2 bottomRight;
        Vector2 topRight;
        if (_currentModelIsTranslation)
        {
            bottomLeft = _bottomLeft + _currentModelTranslation;
            bottomRight = _bottomRight + _currentModelTranslation;
            topRight = _topRight + _currentModelTranslation;
        }
        else
        {
            bottomLeft = Vector2.Transform(_bottomLeft, _currentModel);
            bottomRight = Vector2.Transform(_bottomRight, _currentModel);
            topRight = Vector2.Transform(_topRight, _currentModel);
        }

        return topRight + bottomLeft - bottomRight;
    }
}
