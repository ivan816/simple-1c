using System.Reflection;
using Simple1C.Impl;
using Simple1C.Interface;

namespace Simple1C
{
    public static class DataContextFactory
    {
        public static IDataContext CreateCOM(object globalContext, Assembly mappingsAssembly)
        {
            return new ComDataContext(globalContext, mappingsAssembly);
        }

        public static IDataContext CreateInMemory(Assembly mappingsAssembly)
        {
            return new InMemoryDataContext(mappingsAssembly);
        }
    }
}