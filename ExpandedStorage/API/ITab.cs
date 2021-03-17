using System.Collections.Generic;

namespace ImJustMatt.ExpandedStorage.API
{
    public interface ITab
    {
        /// <summary>Display Name for tab.</summary>
        string TabName { get; set; }

        /// <summary>Image to display for tab, will search asset folder first and default next.</summary>
        string TabImage { get; set; }

        /// <summary>When specified, tab will only show the listed item/category IDs.</summary>
        HashSet<string> AllowList { get; set; }

        /// <summary>When specified, tab will show all/allowed items except for listed item/category IDs.</summary>
        HashSet<string> BlockList { get; set; }
    }
}