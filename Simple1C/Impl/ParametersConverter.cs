using System;
using System.Collections.Generic;
using Simple1C.Impl.Com;
using Simple1C.Impl.Queriables;

namespace Simple1C.Impl
{
    internal class ParametersConverter
    {
        private readonly EnumMapper enumMapper;
        private readonly GlobalContext globalContext;

        public ParametersConverter(EnumMapper enumMapper, GlobalContext globalContext)
        {
            this.enumMapper = enumMapper;
            this.globalContext = globalContext;
        }

        public void ConvertParametersTo1C(Dictionary<string, object> parameters)
        {
            List<string> convertParameterKeys = null;
            foreach (var p in parameters)
                if (p.Value is IConvertParmeterCmd)
                {
                    if (convertParameterKeys == null)
                        convertParameterKeys = new List<string>();
                    convertParameterKeys.Add(p.Key);
                }
            if (convertParameterKeys == null)
                return;
            foreach (var k in convertParameterKeys)
            {
                var value = parameters[k];
                var convertEnum = value as ConvertEnumCmd;
                if (convertEnum != null)
                {
                    parameters[k] = enumMapper.MapTo1C(convertEnum.value);
                    continue;
                }
                var convertUniqueIdentifier = value as ConvertUniqueIdentifierCmd;
                if (convertUniqueIdentifier != null)
                {
                    var name = ConfigurationName.Get(convertUniqueIdentifier.entityType);
                    var scopeManager = ComHelpers.GetProperty(globalContext.ComObject(), name.Scope.ToString());
                    var itemManager = ComHelpers.GetProperty(scopeManager, name.Name);
                    var guidComObject = ComHelpers.Invoke(globalContext.ComObject(),
                        "NewObject", "”никальный»дентификатор", convertUniqueIdentifier.id.ToString());
                    parameters[k] = ComHelpers.Invoke(itemManager, "ѕолучить—сылку", guidComObject);
                    continue;
                }
                throw new InvalidOperationException("assertion failure");
            }
        }
    }
}