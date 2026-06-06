using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace JobFinderApp.ViewModels
{
    public class MyApplicationVm
    {
        public int ApplicationId { get; set; }
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public string Location { get; set; }
        public DateTime AppliedDate { get; set; }
        public string Status { get; set; }
    }
}