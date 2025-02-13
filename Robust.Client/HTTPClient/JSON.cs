using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;


namespace Robust.Client.HTTPClient
{
    public interface IJSON
    {
        string Serialize(object obj);

        object? Parse(string json);

    }
    public class JSON : IJSON
    {
        private int index = 0;

        private string? json;

        public object? Parse(string json)
        {
            this.json = json;

            object? ParseValue()
            {
                SkipWhitespace();
                switch (this.json![this.index])
                {
                    case '{':
                        return ParseObject();
                    case '[':
                        return ParseArray();
                    case '"':
                        return ParseString();
                    case 't':
                        return ParseTrue();
                    case 'f':
                        return ParseFalse();
                    case 'n':
                        return ParseNull();
                    default:
                        return ParseNumber();
                }
            }

            void SkipWhitespace()
            {
                while (char.IsWhiteSpace(this.json![this.index]))
                {
                    this.index++;
                }
            }

            object ParseObject()
            {
                var obj = new Dictionary<string, object?>();
                this.index++;
                while (this.json![this.index] != '}')
                {
                    SkipWhitespace();
                    var key = ParseString();
                    SkipWhitespace();
                    if (this.json[this.index] != ':')
                    {
                        throw new Exception("Expected ':'");
                    }
                    this.index++;
                    SkipWhitespace();
                    var value = ParseValue();
                    obj[key] = value;
                    SkipWhitespace();
                    if (this.json[this.index] == ',')
                    {
                        this.index++;
                    }
                }
                this.index++;
                return obj;
            }

            object ParseArray()
            {
                var list = new List<object?>();
                this.index++;
                while (this.json![this.index] != ']')
                {
                    SkipWhitespace();
                    var value = ParseValue();
                    list.Add(value);
                    SkipWhitespace();
                    if (this.json[this.index] == ',')
                    {
                        this.index++;
                    }
                }
                this.index++;
                return list;
            }

            string ParseString()
            {
                var sb = new StringBuilder();
                this.index++;
                while (this.json![this.index] != '"')
                {
                    if (this.json[this.index] == '\\')
                    {
                        this.index++;
                        switch (this.json[this.index])
                        {
                            case '"':
                                sb.Append('"');
                                break;
                            case '\\':
                                sb.Append('\\');
                                break;
                            case '/':
                                sb.Append('/');
                                break;
                            case 'b':
                                sb.Append('\b');
                                break;
                            case 'f':
                                sb.Append('\f');
                                break;
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'u':
                                var hex = this.json.Substring(this.index + 1, 4);
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                this.index += 4;
                                break;
                        }
                    }
                    else
                    {
                        sb.Append(this.json[this.index]);
                    }
                    this.index++;
                }
                this.index++;
                return sb.ToString();
            }

            object ParseTrue()
            {
                if (this.json!.Substring(this.index, 4) == "true")
                {
                    this.index += 4;
                    return true;
                }
                throw new Exception("Expected 'true'");
            }

            object ParseFalse()
            {
                if (this.json!.Substring(this.index, 5) == "false")
                {
                    this.index += 5;
                    return false;
                }
                throw new Exception("Expected 'false'");
            }

            object? ParseNull()
            {
                if (this.json!.Substring(this.index, 4) == "null")
                {
                    this.index += 4;
                    return null;
                }
                throw new Exception("Expected 'null'");
            }

            object ParseNumber()
            {
                var start = this.index;
                while (char.IsDigit(this.json![this.index]) || this.json[this.index] == '.' || this.json[this.index] == '-' || this.json[this.index] == 'e' || this.json[this.index] == 'E')
                {
                    this.index++;
                }
                var str = this.json.Substring(start, this.index - start);
                if (int.TryParse(str, out var i))
                {
                    return i;
                }
                if (double.TryParse(str, out var d))
                {
                    return d;
                }
                throw new Exception("Invalid number");
            }

            return ParseValue();
        }

        public string Serialize(object? obj)
        {
            if (obj == null)
            {
                return "null";
            }

            if (obj is string)
            {
                return $"\"{obj}\"";

            }

            if (obj is bool b)
            {
                return b.ToString().ToLower() ?? "false";
            }

            if (obj is IDictionary)
            {
                var dict = (IDictionary)obj;
                var items = new List<string?>();
                foreach (var key in dict.Keys)
                {
                    items.Add($"\"{key}\": {Serialize(dict[key])}");
                }
                return $"{{{string.Join(", ", items)}}}";
            }

            if (obj is IEnumerable)
            {
                var items = new List<string>();
                foreach (var item in (IEnumerable)obj)
                {
                    items.Add(Serialize(item));
                }
                return $"[{string.Join(", ", items)}]";
            }

            if (obj is ValueType v)
            {
                return v.ToString() ?? "null";
            }

            var properties = obj.GetType().GetProperties();
            var propItems = new List<string>();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(obj, null);
                propItems.Add($"\"{prop.Name}\": {Serialize(value)}");
            }
            return $"{{{string.Join(", ", propItems)}}}";
        }
    }
}
