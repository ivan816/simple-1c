using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple1C.Impl
{
    internal class Metadata
    {
        private readonly Dictionary<string, MetadataRequisite> requisiteByName;

        public Metadata(string fullname, MetadataRequisite[] requisites)
        {
            Fullname = fullname;
            Requisites = requisites;
            requisiteByName = requisites.ToDictionary(x => x.Name ?? "");
        }

        public string Fullname { get; private set; }
        public MetadataRequisite[] Requisites { get; private set; }

        public void Validate(string name, object value)
        {
            var requisite = requisiteByName[name ?? ""];
            if (!requisite.MaxLength.HasValue)
                return;
            var stringValue = value as string;
            if (stringValue == null)
                throw new InvalidOperationException("assertion failure");
            if (stringValue.Length <= requisite.MaxLength.Value)
                return;
            const string messageFormat = "[{0}{1}] value [{2}] length [{3}] " +
                                         "is greater than configured max [{4}]";
            throw new InvalidOperationException(string.Format(messageFormat,
                Fullname, string.IsNullOrEmpty(name) ? name : "." + name,
                stringValue, stringValue.Length, requisite.MaxLength.Value));
        }
    }
}