using JobFinderApp.Models;
using JobFinderApp.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;

namespace JobFinderApp.Controllers
{
    public class CandidateController : Controller
    {
        // GET: Candidate
        JobFinderDBEntities db = new JobFinderDBEntities();
        public ActionResult Index()
        {
            int userId = Convert.ToInt32(Session["UserId"]);

            // ✅ FIX 1: Correct mapping + include User
            var candidate = db.Candidates
                .Include(c => c.User)
                .FirstOrDefault(c => c.UserId == userId);

            if (candidate == null)
            {
                return RedirectToAction("Login", "Registration");
            }

            // ✅ FIX 2: Include Job + Recruiter
            var applications = db.JobApplications
                .Where(a => a.CandidateId == candidate.CandidateId)
                .Include(a => a.Job)
                .Include(a => a.Job.Recruiter)
                .ToList();

            var vm = new CandidateDashboardVm
            {
                UserName = candidate.User?.FullName ?? "N/A",
                Email = candidate.User?.Email ?? "N/A",
                Skills = candidate.Skills ?? "Not Added",

                TotalApplications = applications.Count(),
                SavedJobs = db.SavedJobs.Count(s => s.CandidateId == candidate.CandidateId),
                Shortlisted = applications.Count(a => a.Status == "Shortlisted"),

                RecentApplications = applications
                    .OrderByDescending(a => a.AppliedDate)
                    .Take(5)
                    .Select(a => new RecentApplicationVm
                    {
                        JobTitle = a.Job?.JobTitle ?? "N/A",
                        Company = a.Job?.Recruiter?.CompanyName ?? "N/A",
                        Status = a.Status ?? "Pending",
                        AppliedDate = a.AppliedDate
                    }).ToList()
            };

            return View(vm);
        }
        public ActionResult UserProfile()
        {
            int userId = Convert.ToInt32(Session["UserId"]);

            var user = db.Users.FirstOrDefault(u => u.UserId == userId);
            var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

            var experiences = db.CandidateExperiences
                                .Where(x => x.UserId == userId)
                                .ToList();

            var educations = db.CandidateEducations
                                .Where(x => x.UserId == userId)
                                .ToList();

            var model = new CandidateProfileVm()
            {
                FullName = user?.FullName,
                Email = user?.Email,
                Mobile = user?.Mobile,
                Skills = candidate?.Skills,
                Experience = candidate?.Experience ?? 0,
                ResumePath = candidate?.ResumePath,
                ProfileImagePath = candidate?.ProfileImagePath,
                About = candidate?.About,

                ExperienceList = experiences.Select(x => new ExperienceVm
                {
                    CompanyName = x.CompanyName,
                    JobRole = x.JobRole,
                    Years = (int)x.Years,
                    Description = x.Description
                }).ToList(),

                EducationList = educations.Select(x => new EducationVm
                {
                    Degree = x.Degree,
                    Institution = x.Institution,
                    Year = x.Year
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public ActionResult AddExperience(ExperienceVm model)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var exp = new CandidateExperience()
            {
                UserId = userId,
                CompanyName = model.CompanyName,
                JobRole = model.JobRole,
                Years = model.Years,
                Description = model.Description
            };

            db.CandidateExperiences.Add(exp);
            db.SaveChanges();

            return RedirectToAction("UserProfile");
        }

        [HttpPost]
        public ActionResult AddEducation(EducationVm model)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var edu = new CandidateEducation()
            {
                UserId = userId,
                Degree = model.Degree,
                Institution = model.Institution,
                Year = model.Year
            };

            db.CandidateEducations.Add(edu);
            db.SaveChanges();

            return RedirectToAction("UserProfile");
        }

        [HttpPost]
        public ActionResult SaveProfile(CandidateProfileVm model, HttpPostedFileBase ResumeFile, HttpPostedFileBase ProfileImage)
        {
            try
            {
                // ✅ Session check
                if (Session["UserId"] == null)
                    return RedirectToAction("Login", "Registration");

                int userId = Convert.ToInt32(Session["UserId"]);

                var user = db.Users.FirstOrDefault(u => u.UserId == userId);
                var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

                // ✅ Create candidate if not exists
                if (candidate == null)
                {
                    candidate = new Candidate()
                    {
                        UserId = userId
                    };
                    db.Candidates.Add(candidate);
                }

                // ✅ USER UPDATE (WITH EMAIL DUPLICATE CHECK)
                if (user != null)
                {
                    user.FullName = model.FullName;
                    user.Mobile = model.Mobile;

                    // 🔥 Prevent duplicate email error
                    if (!string.IsNullOrEmpty(model.Email) && user.Email != model.Email)
                    {
                        var existingUser = db.Users
                            .FirstOrDefault(u => u.Email == model.Email && u.UserId != userId);

                        if (existingUser != null)
                        {
                            TempData["Error"] = "Email already exists!";
                            return RedirectToAction("UserProfile");
                        }

                        user.Email = model.Email;
                    }
                }

                // ✅ Candidate update
                candidate.Skills = model.Skills;
                candidate.Experience = model.Experience;
                candidate.About = model.About;

                // ✅ PROFILE IMAGE UPLOAD
                if (ProfileImage != null && ProfileImage.ContentLength > 0)
                {
                    string folder = Server.MapPath("~/Uploads/ProfileImages/");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(ProfileImage.FileName);
                    string path = Path.Combine(folder, fileName);

                    ProfileImage.SaveAs(path);
                    candidate.ProfileImagePath = "/Uploads/ProfileImages/" + fileName;
                }

                // ✅ RESUME UPLOAD
                if (ResumeFile != null && ResumeFile.ContentLength > 0)
                {
                    string folder = Server.MapPath("~/Uploads/Resumes/");

                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(ResumeFile.FileName);
                    string path = Path.Combine(folder, fileName);

                    ResumeFile.SaveAs(path);
                    candidate.ResumePath = "/Uploads/Resumes/" + fileName;
                }

                // ✅ SAVE CHANGES
                db.SaveChanges();

                TempData["Success"] = "Profile updated successfully!";
            }
            catch (Exception ex)
            {
                // 🔥 Handle unexpected errors safely
                TempData["Error"] = "Something went wrong! " + ex.Message;
            }

            return RedirectToAction("UserProfile");
        }

        public ActionResult MyApplications()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

            if (candidate == null)
                return RedirectToAction("Login", "Registration");

            var applications = db.JobApplications
                .Where(a => a.CandidateId == candidate.CandidateId)
                .Select(a => new MyApplicationVm
                {
                    ApplicationId = a.ApplicationId,
                    JobTitle = a.Job.JobTitle,
                    CompanyName = a.Job.Recruiter.CompanyName,
                    Location = a.Job.Location,
                    AppliedDate = a.AppliedDate,
                    Status = a.Status
                })
                .OrderByDescending(a => a.AppliedDate)
                .ToList();

            return View(applications);
        }

        public ActionResult SavedJobs()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

            if (candidate == null)
                return RedirectToAction("Login", "Registration");

            var savedJobs = db.SavedJobs
                .Where(s => s.CandidateId == candidate.CandidateId)
                .Select(s => s.Job)
                .OrderByDescending(j => j.JobId)
                .ToList();

            return View(savedJobs);
        }

        public ActionResult RemoveSavedJob(int jobId)
        {
            int userId = Convert.ToInt32(Session["UserId"]);

            var candidate = db.Candidates.FirstOrDefault(c => c.UserId == userId);

            var saved = db.SavedJobs
                .FirstOrDefault(s => s.JobId == jobId && s.CandidateId == candidate.CandidateId);

            if (saved != null)
            {
                db.SavedJobs.Remove(saved);
                db.SaveChanges();
            }

            return RedirectToAction("SavedJobs");
        }


    }
}