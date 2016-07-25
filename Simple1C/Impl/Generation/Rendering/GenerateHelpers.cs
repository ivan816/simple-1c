namespace Simple1C.Impl.Generation.Rendering
{
    public static class GenerateHelpers
    {
        public static string EscapeString(string input)
        {
            return input == null ? null : input.Replace("\"", "\\\"");
        }
    }
}