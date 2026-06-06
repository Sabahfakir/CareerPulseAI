using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class CandidateProfileVm
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string Skills { get; set; }
        public int Experience { get; set; }
        public string ResumePath { get; set; }

        public bool IsAutoFilled { get; set; }
        public string ProfileImagePath { get; set; }
        public string About { get; set; }

        public List<ExperienceVm> ExperienceList { get; set; }
        public List<EducationVm> EducationList { get; set; }

    }
}