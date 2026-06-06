using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobFinderApp.Models;
using JobFinderApp.ViewModels;

namespace JobFinderApp.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin
        JobFinderDBEntities db = new JobFinderDBEntities();
        public ActionResult Dashboard()  
        {
            if (Session["AdminId"] == null)
                return RedirectToAction("Login");

            var vm = new AdminDashboardVm();

            vm.TotalJobs = db.Jobs.Count(j => j.IsDeleted == false);
            vm.TotalCandidates = db.Candidates.Count();
            vm.TotalRecruiters = db.Recruiters.Count();
            vm.TotalApplications = db.JobApplications.Count();

            // Jobs per month
            vm.Months = db.Jobs
                .GroupBy(j => j.CreatedDate)
                .Select(g => g.Key.ToString())
                .ToList();

            vm.JobCounts = db.Jobs
                .GroupBy(j => j.CreatedDate)
                .Select(g => g.Count())
                .ToList();

            // Application status
            vm.StatusLabels = db.JobApplications
                .GroupBy(a => a.Status)
                .Select(g => g.Key)
                .ToList();

            vm.StatusCounts = db.JobApplications
                .GroupBy(a => a.Status)
                .Select(g => g.Count())
                .ToList();

            // Top jobs
            vm.TopJobs = db.JobApplications
                .GroupBy(a => a.Job.JobTitle)
                .Select(g => new TopJobVm
                {
                    JobTitle = g.Key,
                    ApplicationCount = g.Count()
                })
                .OrderByDescending(x => x.ApplicationCount)
                .Take(5)
                .ToList();

            return View(vm);
        }

        public ActionResult CandidateReport()
        {
            return View();
        }
        [HttpGet]
        public JsonResult GetCandidates(string skill, int? minExp, int? maxExp)
        {
            var query = db.Candidates
                .Select(c => new
                {
                    Candidate = c,
                    TotalExperience = db.CandidateExperiences
                        .Where(e => e.Id == c.CandidateId)
                        .Sum(e => (int?)e.Years) ?? 0
                });

            // Filter by skill
            if (!string.IsNullOrEmpty(skill))
            {
                query = query.Where(x => x.Candidate.Skills.Contains(skill));
            }

            // Filter by experience (from experience table)
            if (minExp.HasValue)
            {
                query = query.Where(x => x.TotalExperience >= minExp.Value);
            }

            if (maxExp.HasValue)
            {
                query = query.Where(x => x.TotalExperience <= maxExp.Value);
            }

            var data = query.Select(x => new
            {
                Name = x.Candidate.User.FullName,
                Email = x.Candidate.User.Email,
                Skills = x.Candidate.Skills,
                Experience = x.TotalExperience
            }).ToList();

            return Json(new { data = data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ApplicationReport()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetApplications(string status)
        {
            var query = db.JobApplications.AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(a => a.Status == status);
            }

            var data = query.Select(a => new
            {
                Candidate = a.Candidate.User.FullName,
                Job = a.Job.JobTitle,
                Company = a.Job.Recruiter.CompanyName,
                AppliedDate = a.AppliedDate,
                Status = a.Status
            }).ToList();

            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult JobReport()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetJobs(string type)
        {
            var query = db.Jobs.Where(j => j.IsDeleted == false);

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(j => j.JobType == type);
            }

            var data = query.Select(j => new
            {
                Title = j.JobTitle,
                Company = j.Recruiter.CompanyName,
                Location = j.Location,
                Type = j.JobType,
                Salary = j.SalaryRange
            }).ToList();

            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RecruiterReport()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GetRecruiters(string company)
        {
            var query = db.Recruiters.AsQueryable();

            if (!string.IsNullOrEmpty(company))
            {
                query = query.Where(r => r.CompanyName.Contains(company));
            }

            var data = query.Select(r => new
            {
                Company = r.CompanyName,
                Email = r.User.Email,
                Jobs = db.Jobs.Count(j => j.RecruiterId == r.RecruiterId && j.IsDeleted == false)
            }).ToList();

            return Json(new { data }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Login()
        {
            return View();
        }

        // POST: Login
        [HttpPost]
        public ActionResult Login(Admin model)
        {
            var admin = db.Admins
                          .FirstOrDefault(a => a.Email == model.Email
                                            && a.Password == model.Password);

            if (admin != null)
            {
                Session["AdminId"] = admin.AdminId;
                Session["AdminEmail"] = admin.Email;

                return RedirectToAction("Dashboard");
            }

            ViewBag.Error = "Invalid Email or Password";
            return View();
        }

        
    }
}