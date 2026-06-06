using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.Models
{
    public class CareerResult
    {
        public List<string> job_roles { get; set; }
        public List<string> skills_to_learn { get; set; }
        public List<string> career_path { get; set; }
    }
}