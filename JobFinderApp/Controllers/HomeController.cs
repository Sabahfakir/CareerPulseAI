using JobFinderApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace JobFinderApp.Controllers
{
    public class HomeController : Controller
    {
        JobFinderDBEntities db = new JobFinderDBEntities();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {


            return View();
        }

        public ActionResult Contact()
        {

            return View();
        }

        [HttpPost]
        public ActionResult Contact(Contact model)
        {
            if (ModelState.IsValid)
            {
                db.sp_InsertContact(
                    model.Name,
                    model.Email,
                    model.Subject,
                    model.Message
                );

                ViewBag.Message = "Sent Successfully";
            }

            return View();
        }
        public ActionResult Jobs()
        {
            var jobs = db.Jobs
                         .Include(j => j.Recruiter)
                         .Where(j => j.IsActive == true && j.IsDeleted == false)
                         .OrderByDescending(j => j.CreatedDate)
                         .ToList();

            return View(jobs);

        }
        public ActionResult JobDetails(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Jobs");
            }
            var job = db.Jobs
                        .Include(j => j.Recruiter)
                        .FirstOrDefault(j => j.JobId == id);

            if (job == null)
                return HttpNotFound();

            return View(job);
        }
        public ActionResult ApplyJob(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            // Get candidate
            var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

            // Get selected job
            var job = db.Jobs.FirstOrDefault(j => j.JobId == id);

            if (candidate == null || job == null)
            {
                return HttpNotFound();
            }

            // Candidate skills
            var candidateSkills = (candidate.Skills ?? "")
                .ToLower()
                .Split(',')
                .Select(s => s.Trim())
                .ToList();

            // Job required skills
            var requiredSkills = (job.Skills ?? "")
                .ToLower()
                .Split(',')
                .Select(s => s.Trim())
                .ToList();

            // Matching skills count
            var matchedSkills = requiredSkills
                .Where(skill => candidateSkills.Contains(skill))
                .ToList();

            int matchedCount = matchedSkills.Count;

            // Require at least 1 matching skill
            if (matchedCount == 0)
            {
                TempData["Error"] = "You cannot apply for this job because your skills do not match the required skills.";
                return RedirectToAction("JobDetails", new { id = id });
            }

            // Optional: Require at least 50% skills match
            // if (matchedCount < requiredSkills.Count / 2)
            // {
            //     TempData["Error"] = "You need at least 50% skill match to apply.";
            //     return RedirectToAction("JobDetails", new { id = id });
            // }

            // Check if already applied
            bool alreadyApplied = db.JobApplications.Any(a => a.JobId == id && a.CandidateId == candidate.CandidateId);

            if (alreadyApplied)
            {
                TempData["Error"] = "You have already applied for this job.";
                return RedirectToAction("JobDetails", new { id = id });
            }

            // Save application
            JobApplication application = new JobApplication()
            {
                JobId = job.JobId,
                CandidateId = candidate.CandidateId,
                AppliedDate = DateTime.Now,
                Status = "Pending"
            };

            db.JobApplications.Add(application);
            db.SaveChanges();

            TempData["Success"] = "Job applied successfully.";
            return RedirectToAction("MyApplications", "Candidate");
        }

        public ActionResult SaveJob(int jobId)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

            if (candidate == null)
                return RedirectToAction("Login", "Registration");

            // Check if already saved
            bool alreadySaved = db.SavedJobs
                .Any(s => s.JobId == jobId && s.CandidateId == candidate.CandidateId);

            if (alreadySaved)
            {
                TempData["Error"] = "Job already saved.";
                return RedirectToAction("FindJobs");
            }

            SavedJob save = new SavedJob()
            {
                JobId = jobId,
                CandidateId = candidate.CandidateId,
                SavedDate = DateTime.Now
            };

            db.SavedJobs.Add(save);
            db.SaveChanges();

            TempData["Success"] = "Job saved successfully!";
            return RedirectToAction("SavedJobs", "Candidate");
        }
    }
}