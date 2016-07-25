using System;

namespace Simple1C.Interface.ObjectModel
{
    public class ObjectPresentationAttribute : Attribute
    {
        public ObjectPresentationAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}