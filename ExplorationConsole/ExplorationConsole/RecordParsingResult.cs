using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExplorationConsole
{
    class RecordParsingResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public bool IsLiving { get; set; }
        public string FatherId { get; set; }
        public string MotherId { get; set; }
        public List<string> SpouseIds { get; set; }
        public List<string> ChildrenIds { get; set; }
        public string DetailSuggestions { get; set; }
        public string ParentSuggestions { get; set; }
        public string HintSuggestions { get; set; }
    }
}
