using System;

namespace Fluent.Data
{
    public class FluentConnectionInformation
    {
        public string Name { get; set; }

        public string ProviderName { get; set; }

        public string ConnectionString { get; set; }

        public Func<string, string> DecryptString { get; set; }
    }
}