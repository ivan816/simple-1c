using System.Collections.Generic;
using System.IO;
using Simple1C.Impl.Helpers;
using Simple1C.Interface;

namespace Simple1C.Impl.Generation
{
    internal class GenerationContext
    {
        private readonly string rootDirectoryFullPath;
        private readonly HashSet<ConfigurationName> seen = new HashSet<ConfigurationName>();
        private readonly List<string> writtenFiles = new List<string>();

        public GenerationContext(string rootDirectoryFullPath)
        {
            this.rootDirectoryFullPath = rootDirectoryFullPath;
            ItemsToProcess = new Queue<ConfigurationItem>();
        }

        public Queue<ConfigurationItem> ItemsToProcess { get; private set; }

        public void Write(ConfigurationScope scope, string name, string content)
        {
            var fileFullPath = Path.Combine(rootDirectoryFullPath, scope.ToString(), name) + ".cs";
            var directoryFullPath = PathHelpers.GetDirectoryName(fileFullPath);
            if (!Directory.Exists(directoryFullPath))
                Directory.CreateDirectory(directoryFullPath);
            File.WriteAllText(fileFullPath, content);
            writtenFiles.Add(fileFullPath);
        }

        public IEnumerable<string> GetWrittenFiles()
        {
            return writtenFiles;
        }

        public void EnqueueIfNeeded(ConfigurationItem item)
        {
            if (seen.Add(item.Name))
                ItemsToProcess.Enqueue(item);
        }
    }
}