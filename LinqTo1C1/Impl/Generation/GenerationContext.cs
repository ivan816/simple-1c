using System;
using System.Collections.Generic;

namespace LinqTo1C.Impl.Generation
{
    public class GenerationContext
    {
        private readonly HashSet<ConfigurationName> seen = new HashSet<ConfigurationName>();

        private readonly Dictionary<ConfigurationScope, List<string>> builders =
            new Dictionary<ConfigurationScope, List<string>>();

        public GenerationContext()
        {
            ItemsToProcess = new Queue<ConfigurationItem>();
            foreach (ConfigurationScope s in Enum.GetValues(typeof (ConfigurationScope)))
                builders.Add(s, new List<string>());
        }

        public Queue<ConfigurationItem> ItemsToProcess { get; private set; }

        public void AddItem(ConfigurationScope scope, string item)
        {
            builders[scope].Add(item);
        }

        public IEnumerable<KeyValuePair<ConfigurationScope, List<string>>> GetNamespaces()
        {
            return builders;
        }

        public void EnqueueIfNeeded(ConfigurationItem item)
        {
            if (seen.Add(item.Name))
                ItemsToProcess.Enqueue(item);
        }
    }
}