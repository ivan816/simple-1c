namespace Simple1C.Interface
{
    public static class DataContextExtensions
    {
        public static void Save(this IDataContext dataContext, params object[] entities)
        {
            dataContext.Save(entities);
        }
    }
}