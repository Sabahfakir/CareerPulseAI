using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class RecruiterDashboardVm
    {
        public int ActiveJobs { get; set; }
        public int TotalApplications { get; set; }
        public int Shortlisted { get; set; }
        public int Interviews { get; set; }

        public List<JobListVm> Jobs { get; set; }
        public List<ApplicationVm> Applications { get; set; }
    }

    public class JobListVm
    {
        public int JobId { get; set; }
        public string Title { get; set; }        // maps to Job.JobTitle
        public string Location { get; set; }
        public string JobType { get; set; }
        public int ApplicantCount { get; set; }
        public string Status { get; set; }       // Active / Inactive
    }

    public class ApplicationVm
    {
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; }   // from User table
        public string Role { get; set; }            // JobTitle
        public int Experience { get; set; }         // Candidate.Experience
    }
}