using System;

namespace Simple1C.Interface.ObjectModel
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public class MaxLengthAttribute : Attribute
    {
        public MaxLengthAttribute(int value)
        {
            Value = value;
        }

        public int Value { get; private set; }
    }
}