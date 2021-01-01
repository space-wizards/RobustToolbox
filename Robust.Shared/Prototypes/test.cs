#nullable enable
using System;
using System.Linq;
using Newtonsoft.Json.Serialization;


namespace Robust.Shared.Prototypes
{
    public class test2 : test
    {
        public override string[] Tags => base.Tags.Concat(new string[]
        {
            "ayy"
        }).ToArray();
    }

    public class test : ComponentData
    {
        public override string[] Tags => new string[]
        {
            "abc",
            "bcd"
        };

        public string? abc;
        public string? bcd;


        /// <inheritdoc />
        public override object? GetValue(string tag)
        {
            return tag switch
            {
                "abc" => abc,
                "bcd" => bcd,
                _ => base.GetValue(tag)
            };
        }

        public override void SetValue(string tag, object? value)
        {
            switch (tag)
            {
                case "abc":
                    abc = (string?)value;
                    break;
                case "bcd":
                    bcd = (string?)value;
                    break;
                default:
                    throw new ArgumentException($"Tag {tag} not defined.", nameof(tag));
            }
        }
    }
}
