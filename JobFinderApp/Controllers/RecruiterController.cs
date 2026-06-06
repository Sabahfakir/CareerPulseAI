using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobFinderApp.Models;
using JobFinderApp.ViewModels;
using System.Data.Entity;

namespace JobFinderApp.Controllers
{
    public class RecruiterController : Controller
    {
        JobFinderDBEntities db = new JobFinderDBEntities();
        // GET: Recruiter
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            // ✅ Get recruiter
            var recruiter = db.Recruiters
                              .FirstOrDefault(r => r.UserId == userId);

            if (recruiter == null)
                return RedirectToAction("Login", "Registration");

            int recruiterId = recruiter.RecruiterId;

            // ✅ Fetch Jobs
            var jobs = db.Jobs
                         .Where(j => j.RecruiterId == recruiterId && j.IsDeleted != true)
                         .ToList();

            // ✅ Fetch Applications with relations
            var applications = db.JobApplications
                                 .Include(a => a.Job)
                                 .Include(a => a.Candidate.User)
                                 .Where(a => a.Job.RecruiterId == recruiterId)
                                 .ToList();

            // ✅ Applicant count (optimized)
            var applicantCounts = applications
                .GroupBy(a => a.JobId)
                .ToDictionary(g => g.Key, g => g.Count());

            // ✅ Build ViewModel
            var vm = new RecruiterDashboardVm
            {
                ActiveJobs = jobs.Count(j => j.IsActive == true),
                TotalApplications = applications.Count,
                Shortlisted = applications.Count(a => a.Status == "Shortlisted"),
                Interviews = applications.Count(a => a.Status == "Interview"),

                Jobs = jobs.Select(j => new JobListVm
                {
                    JobId = j.JobId,
                    Title = j.JobTitle,
                    Location = j.Location,
                    JobType = j.JobType,
                    ApplicantCount = applicantCounts.ContainsKey(j.JobId)
                                        ? applicantCounts[j.JobId]
                                        : 0,
                    Status = j.IsActive == true ? "Active" : "Inactive"
                }).ToList(),

               Applications = applications
                    .OrderByDescending(a => a.AppliedDate)
                    .Take(5)
                    .Select(a => new ApplicationVm
                    {
                        ApplicationId = a.ApplicationId,
                        CandidateName = a.Candidate?.User?.FullName ?? "N/A",
                        Role = a.Job?.JobTitle ?? "N/A",
                        Experience = a.Candidate?.Experience ?? 0
                    }).ToList()
            };

            return View(vm);


        }

        public ActionResult PostJob()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult PostJob(Job model)
        {
            int userId = Convert.ToInt32(Session["UserId"]);

            var recruiter = db.Recruiters.FirstOrDefault(r => r.UserId == userId);

            if (recruiter == null)
                return RedirectToAction("Login", "Registration");

            if (ModelState.IsValid)
            {
                model.RecruiterId = recruiter.RecruiterId; // 🔥 KEY LINE
                model.CreatedDate = DateTime.Now;
                model.IsActive = true;
                model.IsDeleted = false;

                db.Jobs.Add(model);
                db.SaveChanges();

                TempData["Success"] = "Job posted successfully!";
                return RedirectToAction("PostJob");
            }

            return View(model);
        }

        public ActionResult RecruiterProfile()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Registration");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            var recruiter = db.Recruiters.FirstOrDefault(x => x.UserId == userId);

            if (recruiter == null)
            {
                return HttpNotFound();
            }

            var user = db.Users.FirstOrDefault(x => x.UserId == recruiter.UserId);
            RecruiterProfileVm model = new RecruiterProfileVm()
    {
        RecruiterId = recruiter.RecruiterId,
        UserId = (int)recruiter.UserId,
        CompanyName = recruiter.CompanyName,
        CompanyWebsite = recruiter.CompanyWebsite,
        CompanyLocation = recruiter.CompanyLocation,

        Email = user != null ? user.Email : "",
        Mobile = user != null ? user.Mobile : ""
    };

    return View(model);
}

[HttpPost]
        public ActionResult UpdateCompany(RecruiterProfileVm model)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login", "Registration");
            }

            var recruiter = db.Recruiters.FirstOrDefault(x => x.RecruiterId == model.RecruiterId);

            if (recruiter != null)
            {
                recruiter.CompanyName = model.CompanyName;
                recruiter.CompanyWebsite = model.CompanyWebsite;
                recruiter.CompanyLocation = model.CompanyLocation;

                db.SaveChanges();
            }

            var user = db.Users.FirstOrDefault(x => x.UserId == model.UserId);

            if (user != null)
            {
                user.Email = model.Email;
                user.Mobile = model.Mobile;

                db.SaveChanges();
            }

            TempData["SuccessMessage"] = "Recruiter profile updated successfully.";

            return RedirectToAction("RecruiterProfile");
        }

        public ActionResult DeleteCompany(int id)
        {
            var recruiter = db.Recruiters.FirstOrDefault(x => x.RecruiterId == id);

            if (recruiter != null)
            {
                // Delete related jobs first
                var jobs = db.Jobs.Where(x => x.RecruiterId == recruiter.RecruiterId).ToList();

                if (jobs.Any())
                {
                    db.Jobs.RemoveRange(jobs);
                }

                db.Recruiters.Remove(recruiter);
                db.SaveChanges();
            }
            Session.Clear();
            return RedirectToAction("Login", "Registration");
        }

        public ActionResult Applicants(int jobId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            // Get recruiter using logged-in user id
            var recruiter = db.Recruiters.FirstOrDefault(r => r.UserId == userId);

            if (recruiter == null)
                return RedirectToAction("Login", "Registration");

            // Verify that this job belongs to the logged-in recruiter
            var job = db.Jobs.FirstOrDefault(j => j.JobId == jobId && j.RecruiterId == recruiter.RecruiterId);

            if (job == null)
            {
                TempData["Error"] = "Job not found or you are not authorized to view applicants.";
                return RedirectToAction("MyPostedJobs");
            }

            var applicants = db.JobApplications
                .Where(a => a.JobId == jobId)
                .Select(a => new ApplicantVm
                {
                    ApplicationId = a.ApplicationId,
                    CandidateName = a.Candidate.User.FullName,
                    Email = a.Candidate.User.Email,
                    Skills = a.Candidate.Skills,
                    Experience = a.Candidate.Experience,
                    AppliedDate = a.AppliedDate,
                    Status = a.Status,
                    ResumePath = a.Candidate.ResumePath
                })
                .OrderByDescending(a => a.AppliedDate)
                .ToList();

            ViewBag.JobTitle = job.JobTitle;

            return View(applicants);
        }

        public ActionResult UpdateApplicationStatus(int applicationId, string status)
        {
            var application = db.JobApplications.FirstOrDefault(a => a.ApplicationId == applicationId);

            if (application != null)
            {
                application.Status = status;
                db.SaveChanges();
            }

            return RedirectToAction("Applicants", new { jobId = application.JobId });
        }

        public ActionResult MyPostedJobs()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var recruiter = db.Recruiters.FirstOrDefault(r => r.UserId == userId);

            if (recruiter == null)
            {
                return View(new List<Job>());
            }

            var jobs = db.Jobs
                .Include("Recruiter")
                .Where(j => j.RecruiterId == recruiter.RecruiterId
                         && (j.IsDeleted == false || j.IsDeleted == null)
                         && (j.IsActive == true || j.IsActive == null))
                .OrderByDescending(j => j.JobId)
                .ToList();

            return View(jobs);
        }

        // GET: Edit Job
        public ActionResult EditJob(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var recruiter = db.Recruiters.FirstOrDefault(r => r.UserId == userId);

            if (recruiter == null)
                return RedirectToAction("Login", "Registration");

            var job = db.Jobs.FirstOrDefault(j => j.JobId == id && j.RecruiterId == recruiter.RecruiterId);

            if (job == null)
                return RedirectToAction("MyPostedJobs");

            return View(job);
        }

        // POST: Edit Job
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditJob(Job model)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var recruiter = db.Recruiters.FirstOrDefault(r => r.UserId == userId);

            if (recruiter == null)
                return RedirectToAction("Login", "Registration");

            var job = db.Jobs.FirstOrDefault(j => j.JobId == model.JobId && j.RecruiterId == recruiter.RecruiterId);

            if (job == null)
                return RedirectToAction("MyPostedJobs");

            if (ModelState.IsValid)
            {
                job.JobTitle = model.JobTitle;
                job.Location = model.Location;
                job.JobType = model.JobType;
                job.ExperienceRequired = model.ExperienceRequired;
                job.SalaryRange = model.SalaryRange;
                job.JobDescription = model.JobDescription;
                job.Responsibilities = model.Responsibilities;
                job.Skills = model.Skills;
                job.ApplicationDeadline = model.ApplicationDeadline;

                db.SaveChanges();

                TempData["Success"] = "Job updated successfully!";
                return RedirectToAction("MyPostedJobs");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteJob(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var recruiter = db.Recruiters.FirstOrDefault(r => r.UserId == userId);
            if (recruiter == null)
                return RedirectToAction("Login", "Registration");

            // only delete OWN job
            var job = db.Jobs.FirstOrDefault(j =>
                j.JobId == id &&
                j.RecruiterId == recruiter.RecruiterId);

            if (job == null)
            {
                TempData["Error"] = "Unauthorized or job not found.";
                return RedirectToAction("MyPostedJobs");
            }

            // ✅ SOFT DELETE
            job.IsDeleted = true;
            job.IsActive = false;

            db.SaveChanges();

            TempData["Success"] = "Job deleted successfully!";
            return RedirectToAction("MyPostedJobs");
        }
    }
}