using System;
using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl
{
    internal class ParametersConverter
    {
        private readonly ComObjectMapper comObjectMapper;
        private readonly GlobalContext globalContext;

        public ParametersConverter(ComObjectMapper comObjectMapper, 
            GlobalContext globalContext)
        {
            this.comObjectMapper = comObjectMapper;
            this.globalContext = globalContext;
        }

        public void ConvertParametersTo1C(Dictionary<string, object> parameters)
        {
            List<string> keys = null;
            foreach (var p in parameters)
                if (p.Value is IConvertParameterCmd)
                {
                    if (keys == null)
                        keys = new List<string>();
                    keys.Add(p.Key);
                }
            if (keys != null)
                foreach (var k in keys)
                    parameters[k] = ConvertParameterValue(parameters[k]);
        }

        private object ConvertParameterValue(object value)
        {
            var convertEnum = value as ConvertEnumCmd;
            if (convertEnum != null)
                return comObjectMapper.MapEnumTo1C(convertEnum.valueIndex, convertEnum.enumType);
            var convertUniqueIdentifier = value as ConvertUniqueIdentifierCmd;
            if (convertUniqueIdentifier != null)
            {
                var name = ConfigurationName.Get(convertUniqueIdentifier.entityType);
                var itemManager = globalContext.GetManager(name);
                var guidComObject = comObjectMapper.MapGuidTo1C(convertUniqueIdentifier.id);
                return ComHelpers.Invoke(itemManager, "ѕолучить—сылку", guidComObject);
            }
            throw new InvalidOperationException("assertion failure");
        }
    }
}