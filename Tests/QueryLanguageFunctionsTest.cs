using System;
using NUnit.Framework;
using Simple1C.Interface;

namespace Simple1C.Tests
{
    public class QueryLanguageFunctionsTest
    {
        [Test] 
        public void NullPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation(null), Is.EqualTo(""));
        }

        [Test] 
        public void IntPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation(1), Is.EqualTo("1"));
        }

        [Test] 
        public void BytePresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((byte)1), Is.EqualTo("1"));
        }

        [Test] 
        public void SBytePresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((sbyte)1), Is.EqualTo("1"));
        }

        [Test] 
        public void ShortPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((short)1), Is.EqualTo("1"));
        }

        [Test] 
        public void UshortPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((ushort)1), Is.EqualTo("1"));
        }

        [Test] 
        public void UintPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((uint)1), Is.EqualTo("1"));
        }

        [Test] 
        public void LongPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((long)1), Is.EqualTo("1"));
        }

        [Test] 
        public void UlongPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((ulong)1), Is.EqualTo("1"));
        }

        [Test] 
        public void FloatPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((float)1.4), Is.EqualTo("1,4"));
        }

        [Test] 
        public void DoublePresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((double)1.4), Is.EqualTo("1,4"));
        }

        [Test] 
        public void DecimalPresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation((decimal)1.4), Is.EqualTo("1,4"));
        }

        [Test] 
        public void TruePresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation(true), Is.EqualTo("Да"));
        }

        [Test] 
        public void FalsePresentation()
        {
            Assert.That(QueryLanguageFunctions.Presentation(false), Is.EqualTo("Нет"));
        }

        [Test] 
        public void StringPresentation()
        {
            var str = Guid.NewGuid().ToString();
            Assert.That(QueryLanguageFunctions.Presentation(str), Is.EqualTo(str));
        }

        [Test] 
        public void GuidPresentation()
        {
            var guid = Guid.NewGuid();
            Assert.That(QueryLanguageFunctions.Presentation(guid), Is.EqualTo(guid.ToString()));
        }
    }
}