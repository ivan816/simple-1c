using System;

namespace Simple1C.Impl.Sql.SqlAccess.Syntax
{
    public class OperatorSynonymsAttribute : Attribute
    {
        public OperatorSynonymsAttribute(params string[] synonyms)
        {
            Synonyms = synonyms;
        }

        public string[] Synonyms { get; private set; }
    }
}