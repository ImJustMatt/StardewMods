using System.Reflection;

namespace Helpers.ConfigData
{
    internal interface IField
    {
        string DisplayName { get; }
        string Name { get; set; }
        string Description { get; set; }
        PropertyInfo Info { get; set; }
        object DefaultValue { get; set; }
    }
}