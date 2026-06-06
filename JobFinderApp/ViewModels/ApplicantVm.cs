using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class ApplicantVm
    {
        public int ApplicationId { get; set; }
        public string CandidateName { get; set; }
        public string Email { get; set; }
        public string Skills { get; set; }
        public int? Experience { get; set; }
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; }
        public string ResumePath { get; set; }
    }
}