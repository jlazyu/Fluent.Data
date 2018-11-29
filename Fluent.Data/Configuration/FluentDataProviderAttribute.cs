using System;

namespace Fluent.Data.Configuration
{
    public class FluentDataProviderAttribute : Attribute
    {
        public string DataProviderName { get; }

        public FluentDataProviderAttribute(string dataProviderName)
        {
            DataProviderName = dataProviderName;
        }
    }
}