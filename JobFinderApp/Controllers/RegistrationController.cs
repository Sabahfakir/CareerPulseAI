using JobFinderApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobFinderApp.ViewModels;
using JobFinderApp.Services;



namespace JobFinderApp.Controllers
{
    public class RegistrationController : Controller
    {

        JobFinderDBEntities db = new JobFinderDBEntities();
        private readonly IEmailService _emailService;
       

        // Constructor Injection
        public RegistrationController()
        {
            _emailService = new EmailService(); // Simple DI
        }

        // ================= REGISTER =================

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(RegisterVm model)
        {
            if (ModelState.IsValid)
            {
                // Save User
                User user = new User()
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Password = model.Password, // Later hash this
                    Mobile = model.Mobile,
                    Role = model.Role,
                    IsActive = true,
                    IsDeleted = false
                };

                db.Users.Add(user);
                db.SaveChanges();

                // ================= FILE UPLOAD =================
                string resumePath = "";

                if (model.ResumeFile != null && model.ResumeFile.ContentLength > 0)
                {
                    string fileName = Path.GetFileName(model.ResumeFile.FileName);
                    string extension = Path.GetExtension(fileName);

                    // Unique File Name
                    string newFileName = Guid.NewGuid() + extension;

                    string path = Path.Combine(Server.MapPath("~/Uploads/Resumes/"), newFileName);

                    model.ResumeFile.SaveAs(path);

                    resumePath = "/Uploads/Resumes/" + newFileName;
                    string fullPath = Server.MapPath(resumePath);

                  
                 
                  
                }

                // ================= ROLE BASED INSERT =================

                if (model.Role == "candidate")
                {
                   
                  

                    Candidate c = new Candidate()
                    {
                        UserId = user.UserId,
                        Skills = model.Skills,
                        Experience = model.Experience ?? 0,

                        ResumePath = resumePath
                    };

                    db.Candidates.Add(c);
                }
                else if (model.Role == "recruiter")
                {
                    Recruiter r = new Recruiter()
                    {
                        UserId = user.UserId,
                        CompanyName = model.CompanyName,
                        CompanyWebsite = model.CompanyWebsite,
                        CompanyLocation = model.CompanyLocation
                    };

                    db.Recruiters.Add(r);
                }

                db.SaveChanges();

                return RedirectToAction("Login");
            }

            return View(model);
        }

        // ================= LOGIN =================

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string Email, string Password, string Role)
        {
            var user = db.Users.FirstOrDefault(x =>
                x.Email == Email &&
                x.Password == Password &&
                x.Role == Role &&
                x.IsActive == true &&
                x.IsDeleted == false
            );

            if (user != null)
            {
                Session["UserId"] = user.UserId;
                Session["Role"] = user.Role;
                Session["UserName"] = user.FullName;

                string subject = "Login Notification - CareerPulse AI";
                string body = $@"
<h3>Welcome to CareerPulse AI</h3>
<p>Hello <b>{user.FullName}</b>,</p>
<p>You have successfully logged in as <b>{user.Role}</b>.</p>
<p>Thank you for using our platform.</p>
<br/>
<p>Regards,<br/>CareerPulse AI Team</p>";

                _emailService.SendEmail(user.Email, subject, body);


                if (user.Role == "candidate")
                    return RedirectToAction("Index", "Candidate");

                else if (user.Role == "recruiter")
                    return RedirectToAction("Index", "Recruiter");
            }

            ViewBag.Error = "Invalid Email or Password";
            return View();
        }

        // ================= LOGOUT =================

        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login");
        }
    }
}
