using JobFinderApp.Helper;
using JobFinderApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using JobFinderApp.Models;
using JobFinderApp.ViewModels;
using System.IO;

namespace JobFinderApp.Controllers
{
    public class ResumeController : Controller
    {
        // GET: Resume
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public async Task<ActionResult> Analyze(HttpPostedFileBase resume, string jobDescription)
        {
            try
            {
                if (resume == null || string.IsNullOrEmpty(jobDescription))
                    return Json(new { success = false, message = "Invalid input" });

                // ✅ Extract resume text
                string resumeText = ResumeParser.ExtractText(resume);

                if (string.IsNullOrWhiteSpace(resumeText))
                    return Json(new { success = false, message = "Could not read resume. Try another file." });

                // ✅ Limit size
                resumeText = resumeText.Length > 3000 ? resumeText.Substring(0, 3000) : resumeText;

                var gemini = new GeminiService();

                string prompt = $@"
Compare resume with job description.

IMPORTANT:
- Return ONLY valid JSON
- No explanation
- No markdown

Format:
{{
    ""score"": number,
    ""missing_keywords"": [],
    ""matched_skills"": [],
    ""suggestions"": []
}}

Resume:
{resumeText}

Job Description:
{jobDescription}
";

                var raw = await gemini.Generate(prompt);

                if (string.IsNullOrEmpty(raw))
                    return Json(new { success = false, message = "No response from AI" });

                // 🔥 DEBUG LOG
                System.Diagnostics.Debug.WriteLine("RAW RESPONSE: " + raw);

                string text = ExtractGeminiText(raw);

                if (string.IsNullOrEmpty(text))
                {
                    return Json(new
                    {
                        success = false,
                        message = "AI response failed",
                        raw = raw
                    });
                }

                // ✅ Clean response
                text = text
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

                System.Diagnostics.Debug.WriteLine("CLEAN TEXT: " + text);

                ATSResult data;

                try
                {
                    data = JsonConvert.DeserializeObject<ATSResult>(text);
                }
                catch (Exception ex)
                {
                    return Json(new
                    {
                        success = false,
                        message = "AI response parse error",
                        error = ex.Message,
                        raw = text
                    });
                }

                // ✅ Save to DB
                using (var db = new JobFinderDBEntities())
                {
                    if (Session["UserId"] != null)
                    {
                        int userId = Convert.ToInt32(Session["UserId"]);

                        var analysis = new ResumeAnalysi
                        {
                            UserId = userId,
                            Score = data.score,
                            MissingKeywords = string.Join(",", data.missing_keywords ?? new List<string>()),
                            MatchedSkills = string.Join(",", data.matched_skills ?? new List<string>()),
                            Suggestions = string.Join(",", data.suggestions ?? new List<string>()),
                            CreatedDate = DateTime.Now
                        };

                        db.ResumeAnalysis.Add(analysis);
                        db.SaveChanges();
                    }
                }

                return Json(new
                {
                    success = true,
                    score = data.score,
                    missing = data.missing_keywords ?? new List<string>(),
                    matched = data.matched_skills ?? new List<string>(),
                    suggestions = data.suggestions ?? new List<string>()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        private string ExtractGeminiText(string json)
        {
            try
            {
                dynamic obj = JsonConvert.DeserializeObject(json);

                // Case 1: Standard Gemini response
                var text = obj?.candidates?[0]?.content?.parts?[0]?.text;
                if (text != null)
                    return text.ToString();

                // Case 2: Alternate structure
                var altText = obj?.candidates?[0]?.content?.parts?[0];
                if (altText != null)
                    return altText.ToString();

                // Case 3: Direct text fallback
                if (obj?.text != null)
                    return obj.text.ToString();

                return null;
            }
            catch
            {
                return null;
            }
        }

        [HttpGet]
        public ActionResult ResumeBuilder()
        {
            return View();
        }
        [HttpPost]
        public async Task<ActionResult> Generate(ResumeBuilderModel model)
        {
            if (model == null)
                return Json(new { success = false, message = "Invalid data" });

            var gemini = new GeminiService();

            // ✅ Build Experience Text
            var experienceText = model.Experiences != null && model.Experiences.Any()
                ? string.Join("\n\n", model.Experiences.Select(e =>
                    $@"{e.JobTitle} at {e.CompanyName} ({e.Location})
{(e.IsCurrent
            ? $"{e.StartDate:MMM yyyy} - Present"
            : $"{e.StartDate:MMM yyyy} - {e.EndDate:MMM yyyy}")}

Responsibilities:
{e.Description}"
                ))
                : "No experience provided";

            // ✅ Build Education Text
            var educationText = model.EducationList != null && model.EducationList.Any()
                ? string.Join("\n", model.EducationList.Select(e =>
                    $"{e.Degree} - {e.Institution} ({e.Year}) | Score: {e.Score}"
                ))
                : "No education details provided";

            // ✅ Build Project Text
            var projectText = model.Projects != null && model.Projects.Any()
                ? string.Join("\n\n", model.Projects.Select(p =>
                    $@"{p.Title}
Technologies: {p.Technologies}
Description: {p.Description}"
                ))
                : "No projects provided";

            // ✅ FINAL PROMPT
            string prompt = $@"
Create a professional ATS-friendly resume.

Personal Details:
Name: {model.FullName}
Email: {model.Email}
Phone: {model.Phone}
Location: {model.City}, {model.State}, {model.Country}

Professional Summary:
{model.Summary}

Skills:
{string.Join(", ", model.Skills ?? new List<string>())}

Work Experience:
{experienceText}

Education:
{educationText}

Projects:
{projectText}

Rules:
- Use clear section headings
- Convert responsibilities into bullet points
- Keep it ATS-friendly
- Keep formatting clean and professional
- Do not use symbols, icons, or tables
";

            var raw = await gemini.Generate(prompt);

            string text = ExtractGeminiText(raw);

            if (string.IsNullOrEmpty(text))
                return Json(new { success = false, message = "AI failed to generate resume" });

            return Json(new { success = true, resume = text.Trim() });
        }


        //private string ExtractGeminiText(string json)
        //{
        //    try
        //    {
        //        dynamic obj = JsonConvert.DeserializeObject(json);

        //        if (obj == null || obj.candidates == null)
        //            return null;

        //        return obj.candidates[0]?.content?.parts[0]?.text;
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}

        public ActionResult DownloadPdf(string content)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new iText.Kernel.Pdf.PdfWriter(ms);
                var pdf = new iText.Kernel.Pdf.PdfDocument(writer);
                var doc = new iText.Layout.Document(pdf);

                doc.Add(new iText.Layout.Element.Paragraph(content));

                doc.Close();

                return File(ms.ToArray(), "application/pdf", "Resume.pdf");
            }
        }
        [HttpPost]
        public async Task<ActionResult> GenerateSummary(string name, string skills, string experience, string education)
        {
            try
            {
                var gemini = new GeminiService();

                string prompt = $@"
Create a professional ATS-friendly resume summary.

Details:
Name: {name}
Skills: {skills}
Experience: {experience}
Education: {education}

Rules:
Rules:
- 3 to 4 lines only
- ATS optimized (include keywords naturally)
- Use action verbs (Developed, Built, Implemented)
- Focus on skills + impact
- Avoid personal pronouns";

                var raw = await gemini.Generate(prompt);

                string text = ExtractGeminiText(raw);

                if (string.IsNullOrEmpty(text))
                    return Json(new { success = false, message = "AI failed" });

                return Json(new
                {
                    success = true,
                    summary = text.Trim()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public ActionResult SaveResume(ResumeBuilderModel model)
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login", "Registration");

                int userId = Convert.ToInt32(Session["UserId"]);

                using (var db = new JobFinderDBEntities())
                {
                    var resume = new Resume
                    {
                        UserId = userId,
                        FullName = model.FullName,
                        Email = model.Email,
                        Phone = model.Phone,
                        Summary = model.Summary,
                     
                        City = model.City,
                        State = model.State,
                        Country = model.Country,
                        ZipCode = model.ZipCode,
                        DOB = model.DOB,
                        CreatedDate = DateTime.Now
                    };

                    db.Resumes.Add(resume);
                    db.SaveChanges();

                    int resumeId = resume.ResumeId;

                    // ✅ Save Skills
                    if (model.Skills != null)
                    {
                        foreach (var skill in model.Skills)
                        {
                            db.ResumeSkills.Add(new ResumeSkill
                            {
                                ResumeId = resumeId,
                                SkillName = skill
                            });
                        }
                    }

                    // ✅ Save Education
                    if (model.EducationList != null)
                    {
                        foreach (var edu in model.EducationList)
                        {
                            db.ResumeEducations.Add(new ResumeEducation
                            {
                                ResumeId = resumeId,
                                Degree = edu.Degree,
                                Institution = edu.Institution,
                                Year = edu.Year,
                                Score = edu.Score
                            });
                        }
                    }

                    // ✅ Save Projects
                    if (model.Projects != null)
                    {
                        foreach (var p in model.Projects)
                        {
                            db.ResumeProjects.Add(new ResumeProject
                            {
                                ResumeId = resumeId,
                                Title = p.Title,
                                Technologies = p.Technologies,
                                Description = p.Description
                            });
                        }
                    }

                    if (model.Experiences != null)
                    {
                        foreach (var exp in model.Experiences)
                        {
                            db.ResumeExperiences.Add(new ResumeExperience
                            {
                                ResumeId = resumeId,
                                JobTitle = exp.JobTitle,
                                CompanyName = exp.CompanyName,
                                Location = exp.Location,
                                StartDate = exp.StartDate,
                                EndDate = exp.IsCurrent ? null : exp.EndDate,
                                IsCurrent = exp.IsCurrent,
                                Description = exp.Description
                            });
                        }
                    }

                    db.SaveChanges();
                }

                return Json(new { success = true, message = "Resume saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }

        }

        public ActionResult MyResumes()
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            int userId = Convert.ToInt32(Session["UserId"]);

            using (var db = new JobFinderDBEntities())
            {
                var resumes = db.Resumes
                                .Where(r => r.UserId == userId)
                                .OrderByDescending(r => r.CreatedDate)
                                .ToList();

                return View(resumes);
            }
        }

        public ActionResult EditResume(int id)
        {
            if (Session["UserId"] == null)
                return RedirectToAction("Login", "Registration");

            using (var db = new JobFinderDBEntities())
            {
                var resume = db.Resumes.Find(id);

                if (resume == null)
                    return HttpNotFound();

                var model = new ResumeBuilderModel
                {
                    ResumeId = resume.ResumeId,
                    FullName = resume.FullName,
                    Email = resume.Email,
                    Phone = resume.Phone,
                    Summary = resume.Summary,
                    City = resume.City,
                    State = resume.State,
                    Country = resume.Country,
                    ZipCode = resume.ZipCode,
                    DOB = resume.DOB,

                    Skills = db.ResumeSkills
                                .Where(x => x.ResumeId == id)
                                .Select(x => x.SkillName)
                                .ToList(),

                    EducationList = db.ResumeEducations
                                .Where(x => x.ResumeId == id)
                                .Select(x => new EducationModel
                                {
                                    Degree = x.Degree,
                                    Institution = x.Institution,
                                    Year = x.Year,
                                    Score = x.Score
                                }).ToList(),

                    Projects = db.ResumeProjects
                                .Where(x => x.ResumeId == id)
                                .Select(x => new ProjectModel
                                {
                                    Title = x.Title,
                                    Technologies = x.Technologies,
                                    Description = x.Description
                                }).ToList(),

                    Experiences = db.ResumeExperiences
                                .Where(x => x.ResumeId == id)
                                .Select(x => new ExperienceModel
                                {
                                    JobTitle = x.JobTitle,
                                    CompanyName = x.CompanyName,
                                    Location = x.Location,
                                    StartDate = x.StartDate,
                                    EndDate = x.EndDate,
                                    Description = x.Description
                                }).ToList()
                };

                return View(model);
            }
        }

        [HttpPost]
        public ActionResult UpdateResume(ResumeBuilderModel model)
        {
            try
            {
                if (Session["UserId"] == null)
                    return RedirectToAction("Login", "Registration");

                using (var db = new JobFinderDBEntities())
                {
                    var resume = db.Resumes.Find(model.ResumeId);

                    if (resume == null)
                        return HttpNotFound();

                    // ✅ Update main fields
                    resume.FullName = model.FullName;
                    resume.Email = model.Email;
                    resume.Phone = model.Phone;
                    resume.Summary = model.Summary;
                    resume.City = model.City;
                    resume.State = model.State;
                    resume.Country = model.Country;
                    resume.ZipCode = model.ZipCode;
                    resume.DOB = model.DOB;

                    // 🔥 DELETE OLD DATA FIRST
                    db.ResumeSkills.RemoveRange(db.ResumeSkills.Where(x => x.ResumeId == model.ResumeId));
                    db.ResumeEducations.RemoveRange(db.ResumeEducations.Where(x => x.ResumeId == model.ResumeId));
                    db.ResumeProjects.RemoveRange(db.ResumeProjects.Where(x => x.ResumeId == model.ResumeId));
                    db.ResumeExperiences.RemoveRange(db.ResumeExperiences.Where(x => x.ResumeId == model.ResumeId));

                    // ✅ SAVE AGAIN

                    // Skills
                    if (model.Skills != null)
                    {
                        foreach (var skill in model.Skills)
                        {
                            db.ResumeSkills.Add(new ResumeSkill
                            {
                                ResumeId = model.ResumeId,
                                SkillName = skill
                            });
                        }
                    }

                    // Education
                    if (model.EducationList != null)
                    {
                        foreach (var edu in model.EducationList)
                        {
                            db.ResumeEducations.Add(new ResumeEducation
                            {
                                ResumeId = model.ResumeId,
                                Degree = edu.Degree,
                                Institution = edu.Institution,
                                Year = edu.Year,
                                Score = edu.Score
                            });
                        }
                    }

                    // Projects
                    if (model.Projects != null)
                    {
                        foreach (var p in model.Projects)
                        {
                            db.ResumeProjects.Add(new ResumeProject
                            {
                                ResumeId = model.ResumeId,
                                Title = p.Title,
                                Technologies = p.Technologies,
                                Description = p.Description
                            });
                        }
                    }

                    // Experience
                    if (model.Experiences != null)
                    {
                        foreach (var exp in model.Experiences)
                        {
                            db.ResumeExperiences.Add(new ResumeExperience
                            {
                                ResumeId = model.ResumeId,
                                JobTitle = exp.JobTitle,
                                CompanyName = exp.CompanyName,
                                Location = exp.Location,
                                StartDate = exp.StartDate,
                                EndDate = exp.IsCurrent ? null : exp.EndDate,
                                IsCurrent = exp.IsCurrent,
                                Description = exp.Description
                            });
                        }
                    }

                    db.SaveChanges();
                }

                return RedirectToAction("MyResumes");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        public ActionResult DeleteResume(int id)
        {
            using (var db = new JobFinderDBEntities())
            {
                var resume = db.Resumes.Find(id);

                if (resume != null)
                {
                    db.Resumes.Remove(resume);
                    db.SaveChanges();
                }
            }

            return RedirectToAction("MyResumes");
        }

    }
}