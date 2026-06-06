using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class RegisterVm
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Mobile { get; set; }
        public string Role { get; set; }

        // Candidate
        public string Skills { get; set; }
        public int? Experience { get; set; }
        public HttpPostedFileBase ResumeFile { get; set; }

        // Recruiter
        public string CompanyName { get; set; }
        public string CompanyWebsite { get; set; }
        public string CompanyLocation { get; set; }
    }
}