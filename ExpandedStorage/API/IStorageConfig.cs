using System.Collections.Generic;

namespace ImJustMatt.ExpandedStorage.API
{
    public interface IStorageConfig
    {
        /// <summary>Modded capacity allows storing more/less than vanilla (36).</summary>
        public int Capacity { get; set; }

        /// <summary>List of features to toggle on.</summary>
        HashSet<string> EnabledFeatures { get; set; }

        /// <summary>List of features to toggle off.</summary>
        HashSet<string> DisabledFeatures { get; set; }
    }
}