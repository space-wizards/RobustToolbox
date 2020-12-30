using System.Collections.Immutable;
using System.Reflection.Metadata;
using NUnit.Framework;
using Pidgin;
using static Robust.Shared.ContentPack.AssemblyTypeChecker;

namespace Robust.UnitTesting.Shared.ContentPack
{
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    public sealed class AssemblyTypeCheckerParsingTest
    {
        [Test]
        public void TestMethod()
        {
            var res = MethodParser.ParseOrThrow("  void foo ( int , string )");

            Assert.That(res.Name, Is.EqualTo("foo"));
            Assert.That(res.ReturnType, Is.EqualTo(new MTypePrimitive(PrimitiveTypeCode.Void)));
            Assert.That(res.ParameterTypes,
                Is.EquivalentTo(new[]
                {
                    new MTypePrimitive(PrimitiveTypeCode.Int32),
                    new MTypePrimitive(PrimitiveTypeCode.String)
                }));
        }

        [Test]
        public void TestParseConstructor()
        {
            var res = MethodParser.ParseOrThrow("  void .ctor ( int , string )");

            Assert.That(res.Name, Is.EqualTo(".ctor"));
            Assert.That(res.ReturnType, Is.EqualTo(new MTypePrimitive(PrimitiveTypeCode.Void)));
            Assert.That(res.ParameterTypes,
                Is.EquivalentTo(new[]
                {
                    new MTypePrimitive(PrimitiveTypeCode.Int32),
                    new MTypePrimitive(PrimitiveTypeCode.String)
                }));
        }

        [Test]
        public void TestMethodInvalid()
        {
            Assert.That(() => MethodParser.ParseOrThrow("foo ( int , string)"), Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void TestField()
        {
            var res = FieldParser.ParseOrThrow("string bar");

            Assert.That(res.Name, Is.EqualTo("bar"));
            Assert.That(res.FieldType, Is.EqualTo(new MTypePrimitive(PrimitiveTypeCode.String)));
        }

        [Test]
        public void TestGenericMethod()
        {
            var res = MethodParser.ParseOrThrow("string Concat(System.Collections.Generic.IEnumerable`1<!!0>)");

            Assert.That(res.Name, Is.EqualTo("Concat"));
            Assert.That(res.ReturnType, Is.EqualTo(new MTypePrimitive(PrimitiveTypeCode.String)));
            Assert.That(res.ParameterTypes,
                Is.EquivalentTo(new MType[]
                {
                    new MTypeGeneric(
                        new MTypeParsed("System.Collections.Generic.IEnumerable`1"),
                        new MType[] {new MTypeGenericMethodPlaceHolder(0)}.ToImmutableArray())
                }));
        }
    }
}
