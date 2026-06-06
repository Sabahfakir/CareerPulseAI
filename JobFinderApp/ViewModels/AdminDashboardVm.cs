using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class AdminDashboardVm
    {
        public int TotalJobs { get; set; }
        public int TotalCandidates { get; set; }
        public int TotalRecruiters { get; set; }
        public int TotalApplications { get; set; }

        public List<string> Months { get; set; }
        public List<int> JobCounts { get; set; }

        public List<string> StatusLabels { get; set; }
        public List<int> StatusCounts { get; set; }

        public List<TopJobVm> TopJobs { get; set; }
    }

    public class TopJobVm
    {
        public string JobTitle { get; set; }
        public int ApplicationCount { get; set; }
    }
}
