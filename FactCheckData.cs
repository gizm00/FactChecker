using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactCheckerWPF
{
    public class FactCheckData
    {
        public string SpokenText { get; set; }

        public string SearchText { get; set; }

        public string Truthiness { get; set; }

        public decimal Relevancy { get; set; }
    }
}
