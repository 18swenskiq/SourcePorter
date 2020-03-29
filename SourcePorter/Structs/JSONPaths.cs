using System;
using System.Collections.Generic;
using System.Text;

namespace SourcePorter.Structs
{
    // This class defines the structure for our paths.json file. The variable names are used in the actual file so ignore the Naming Rule Violation.
    public class JSONPaths
    {
        public string main_path { get; set; } = "";

        public string game_path { get; set; } = "";

        public string game_info { get; set; } = "";

        public string vpk_exe { get; set; } = "";

        public string pak_dir { get; set; } = "";


    }
}
