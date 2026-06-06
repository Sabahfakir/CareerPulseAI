using System;
using System.Collections.Generic;

namespace JobFinderApp.ViewModels
{
    public class ResumeBuilderModel
    {
        public int ResumeId { get; set; }
        // ===========================
        // BASIC INFO
        // ===========================
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Summary { get; set; }

        // ===========================
        // ADDRESS
        // ===========================
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }

        public DateTime? DOB { get; set; }

        // ===========================
        // USER TYPE (🔥 NEW)
        // ===========================
        public string UserType { get; set; } // "fresher" or "experienced"

        // ===========================
        // SKILLS
        // ===========================
        public List<string> Skills { get; set; }

        // ===========================
        // EXPERIENCE
        // ===========================
        public List<ExperienceModel> Experiences { get; set; }

        // ===========================
        // EDUCATION
        // ===========================
        public List<EducationModel> EducationList { get; set; }

        // ===========================
        // PROJECTS
        // ===========================
        public List<ProjectModel> Projects { get; set; }
    }

    // ===========================
    // EXPERIENCE MODEL
    // ===========================
    public class ExperienceModel
    {
        public string JobTitle { get; set; }
        public string CompanyName { get; set; }
        public string Location { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public bool IsCurrent { get; set; }

        public string Description { get; set; }
    }

    // ===========================
    // EDUCATION MODEL
    // ===========================
    public class EducationModel
    {
        public string Degree { get; set; }
        public string Institution { get; set; }
        public string Year { get; set; }
        public string Score { get; set; }
    }

    // ===========================
    // PROJECT MODEL
    // ===========================
    public class ProjectModel
    {
        public string Title { get; set; }
        public string Technologies { get; set; }
        public string Description { get; set; }
    }
}