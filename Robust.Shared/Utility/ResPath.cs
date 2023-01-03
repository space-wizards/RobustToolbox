using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Robust.Shared.Utility;

[PublicAPI, Serializable, NetSerializable]
public struct ResPath : IEquatable<ResPath>
{
    /// <summary>
    ///     "." as a static. Separator used is <c>/</c>.
    /// </summary>
    public static readonly string Self = ".";

    /// <summary>
    ///     "/" (root) as a static. Separator used is <c>/</c>.
    /// </summary>
    public static readonly string Root = "/";

    internal string CanonicalResource;


    /// <summary>
    ///     Create a new path from a string, splitting it by the separator provided.
    /// </summary>
    /// <param name="path">The string path to turn into a resource path.</param>
    /// <param name="separator">The separator for the resource path.</param>
    /// <exception cref="ArgumentException">Thrown if you try to use "." as separator.</exception>
    /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
    public ResPath(string path = ".", char separator = '/')
    {
        if (separator == '.')
        {
            throw new ArgumentException("Separator may not be .  Prefer \\ or /");
        }

        if (separator != '/')
        {
            // Stupid but should work in all cases (UTF-8/UTF-16 string)
            // maybe replace with some unsafe in future for speed
            var stringBuilder = new StringBuilder(path);
            for (var pos = 0; pos < path.Length; pos++)
            {
                if (path[pos] == separator)
                {
                    stringBuilder[pos] = '/';
                }
            }

            CanonicalResource = stringBuilder.ToString();
        }
        else
        {
            CanonicalResource = path;
        }
    }

    public string Directory
    {
        get
        {
            if (IsSelf) return Self;

            foreach (var VARIABLE in this.Segments())
            {
                return VARIABLE;
            }

            return "";
        }
    }

    public bool IsSelf => CanonicalResource == Self;

    public string Extension
    {
        get
        {
            if (IsSelf) return Self;

            foreach (var VARIABLE in this.Segments())
            {
                return VARIABLE;
            }

            return "";
        }
    }

    public string Filename
    {
        get
        {
            if (IsSelf) return Self;

            foreach (var VARIABLE in this.Segments())
            {
                return VARIABLE;
            }

            return "";
        }
    }

    public bool Equals(ResPath other)
    {
        return CanonicalResource == other.CanonicalResource;
    }

    public override bool Equals(object? obj)
    {
        return obj is ResPath other && Equals(other);
    }

    public override int GetHashCode()
    {
        return CanonicalResource.GetHashCode();
    }

    public static bool operator ==(ResPath left, ResPath right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ResPath left, ResPath right)
    {
        return !left.Equals(right);
    }

    public static ResPath operator /(ResPath left, ResPath right)
    {
        return new ResPath(left.CanonicalResource + "/" + right.CanonicalResource);
    }

    public static ResPath operator /(ResPath left, string right)
    {
        return new ResPath(left.CanonicalResource + "/" + right);
    }

    public ResPath ToRelativePath()
    {
        throw new NotImplementedException();
    }

    public object WithExtension(string newExt)
    {
        throw new NotImplementedException();
    }

    public ResPath WithName(string name)
    {
        throw new NotImplementedException();
    }

    public ResPath RelativeTo(ResPath basePath)
    {
        throw new NotImplementedException();
    }

    public bool IsRooted()
    {
        throw new NotImplementedException();
    }

    public ResPath ToRootedPath()
    {
        throw new NotImplementedException();
    }

    public bool IsRelative()
    {
        throw new NotImplementedException();
    }

    public ResPath Clean()
    {
        throw new NotImplementedException();
    }

    public bool IsClean()
    {
        throw new NotImplementedException();
    }

    public string ChangeSeparator(string s)
    {
        throw new NotImplementedException();
    }

    public string FilenameWithoutExtension()
    {
        throw new NotImplementedException();
    }

    public ResPath CommonBase(ResPath basePath)
    {
        throw new NotImplementedException();
    }
}

public struct SegmentEnumerator : IEnumerator<string>
{
    private readonly ResPath _owner;
    private int _positionStart;
    private int _len;

    public SegmentEnumerator(ResPath resPath)
    {
        _owner = resPath;
        _positionStart = 0;
        _len = 0;
    }

    public bool MoveNext()
    {
        if (_positionStart + _len >= _owner.CanonicalResource.Length)
        {
            return false;
        }

        _positionStart += _len;
        if (_owner.CanonicalResource[_positionStart] == '/')
        {
            _positionStart++;
        }

        var ind = _owner.CanonicalResource[_positionStart ..].IndexOf('/');
        _len = ind == -1 ? _owner.CanonicalResource.Length - _positionStart : ind;

        // skip last `/`
        return _len + _positionStart < _owner.CanonicalResource.Length || _len > 0;
    }

    public void Reset()
    {
        _positionStart = 0;
        _len = 0;
    }

    object IEnumerator.Current => Current;

    public string Current => _owner.CanonicalResource.AsSpan(_positionStart, _len).ToString();


    public void Dispose()
    {
    }
}

public struct ReverseSegmentEnumerator : IEnumerator<string>
{
    private readonly ResPath _owner;
    private int _positionEnd;
    private int _len;

    public ReverseSegmentEnumerator(ResPath resPath)
    {
        _owner = resPath;
        _positionEnd = resPath.CanonicalResource.Length - 1;
        _len = 0;
    }

    public bool MoveNext()
    {
        if (_positionEnd - _len <= 0)
        {
            return false;
        }

        _positionEnd -= _len;
        if (_owner.CanonicalResource[_positionEnd] == '/')
        {
            _positionEnd--;
        }

        var ind = _owner.CanonicalResource[.._positionEnd].LastIndexOf('/');
        if (ind == -1)
        {
            _len = _positionEnd ++;
            _positionEnd = 0;
        }
        else
        {
            _len = _positionEnd - ind;
            _positionEnd = ind + 1;
        }
        

        // skip last `/`
        return _positionEnd >= 0 || _len > 0;
    }

    public void Reset()
    {
        _positionEnd = 0;
        _len = 0;
    }

    public string Current => _owner.CanonicalResource.AsSpan(_positionEnd, _len).ToString();

    object IEnumerator.Current => Current;

    public void Dispose()
    {
    }
}

public static class ResPathExtension
{
    public static IEnumerable<string> Segments(this ResPath resPath)
    {
        var iter = new SegmentEnumerator(resPath);
        while (iter.MoveNext())
        {
            yield return iter.Current;
        }
    }

    public static IEnumerable<string> ReverseSegments(this ResPath resPath)
    {
        var iter = new ReverseSegmentEnumerator(resPath);
        while (iter.MoveNext())
        {
            yield return iter.Current;
        }
    }
}