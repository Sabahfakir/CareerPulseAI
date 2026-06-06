using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.Models
{
    public class ATSResult
    {

        public int score { get; set; }
        public List<string> missing_keywords { get; set; }
        public List<string> matched_skills { get; set; }
        public List<string> suggestions { get; set; }
    }
}