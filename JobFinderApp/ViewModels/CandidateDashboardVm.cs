using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class CandidateDashboardVm
    {
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Skills { get; set; }

        public int TotalApplications { get; set; }
        public int SavedJobs { get; set; }
        public int Shortlisted { get; set; }

        public List<RecentApplicationVm> RecentApplications { get; set; }
    }

    public class RecentApplicationVm
    {
        public string JobTitle { get; set; }
        public string Company { get; set; }
        public string Status { get; set; }
        public DateTime AppliedDate { get; set; }
    }
}