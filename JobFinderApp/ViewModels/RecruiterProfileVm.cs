using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class RecruiterProfileVm
    {

        public int RecruiterId { get; set; }
        public int UserId { get; set; }

        public string CompanyName { get; set; }
        public string CompanyWebsite { get; set; }
        public string CompanyLocation { get; set; }

        // Optional User Table Fields
        public string Email { get; set; }
        public string Mobile { get; set; }

    }
}