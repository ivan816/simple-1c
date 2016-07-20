using System;

namespace Simple1C.Interface.ObjectModel
{
    public class SynonymAttribute : Attribute
    {
        public SynonymAttribute(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
    }
}