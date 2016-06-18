namespace Tests.Helpers
{
    public static class Robocopy
    {
        public static void Execute(string source, string target, bool onlyNewer)
        {
            ProcessesHelpers.ExecuteProcess("robocopy.exe",
                string.Format("\"{0}\" \"{1}\" /E{2}",
                    source, target, onlyNewer ? " /XO" : ""), null,
                exitCode => exitCode >= 0 && exitCode <= 7);
        }
    }
}