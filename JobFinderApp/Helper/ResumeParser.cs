using System;
using System.IO;
using System.Drawing;
using System.Web;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using Tesseract;


// Aliases
using ITextPdf = iText.Kernel.Pdf.PdfDocument;


namespace JobFinderApp.Helper
{
    public class ResumeParser
    {
        public static string ExtractText(HttpPostedFileBase file)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext == ".pdf")
            {
                string text = ExtractFromPdf(file);

                System.Diagnostics.Debug.WriteLine("PDF TEXT LENGTH: " + text.Length);

                // 🔥 Improved condition
                if (string.IsNullOrWhiteSpace(text) || text.Length < 50)
                {
                    System.Diagnostics.Debug.WriteLine("Falling back to OCR...");
                    text = ExtractFromImagePdf(file);
                }

                System.Diagnostics.Debug.WriteLine("FINAL TEXT LENGTH: " + text.Length);

                return text;
            }

            if (ext == ".docx")
            {
                return ExtractFromDocx(file);
            }

            return "";
        }

        // ✅ TEXT-BASED PDF
        private static string ExtractFromPdf(HttpPostedFileBase file)
        {
            try
            {
                file.InputStream.Position = 0;

                using (var reader = new PdfReader(file.InputStream))
                using (var pdfDoc = new ITextPdf(reader))
                {
                    string text = "";

                    var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();

                    for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)
                    {
                        text += PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                    }

                    return text;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("PDF READ ERROR: " + ex.Message);
                return "";
            }
        }

        // ✅ IMAGE-BASED PDF (OCR)
        private static string ExtractFromImagePdf(HttpPostedFileBase file)
        {
            string result = "";

            try
            {
                string tempPdf = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
                string outputPrefix = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                // Save PDF
                file.InputStream.Position = 0;
                using (var fs = new FileStream(tempPdf, FileMode.Create))
                {
                    file.InputStream.CopyTo(fs);
                }

                // 🔥 Ghostscript path (adjust if needed)
                string gsPath = @"C:\Program Files\gs\gs10.07.0\bin\gswin64c.exe";

                string args = $"-dNOPAUSE -sDEVICE=png16m -r300 -o \"{outputPrefix}_%03d.png\" \"{tempPdf}\" -dBATCH";

                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = gsPath;
                process.StartInfo.Arguments = args;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.UseShellExecute = false;

                process.Start();
                process.WaitForExit();

                string tessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                var images = Directory.GetFiles(Path.GetTempPath(), Path.GetFileName(outputPrefix) + "*.png");

                using (var engine = new TesseractEngine(tessPath, "eng", EngineMode.Default))
                {
                    foreach (var imgFile in images)
                    {
                        using (var img = (Bitmap)Image.FromFile(imgFile))
                        using (var pix = PixConverter.ToPix(img))
                        using (var page = engine.Process(pix, PageSegMode.Auto))
                        {
                            result += page.GetText();
                        }

                        File.Delete(imgFile);
                    }
                }

                File.Delete(tempPdf);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OCR ERROR: " + ex.Message);
            }

            return result;
        }

        // ✅ OCR ENGINE
        private static string RunOCR(Image img)
        {
            try
            {
                // 🔥 FIXED PATH (Important)
                string tessPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

                System.Diagnostics.Debug.WriteLine("TESS PATH: " + tessPath);

                using (var engine = new TesseractEngine(tessPath, "eng", EngineMode.Default))
                using (var pix = PixConverter.ToPix((Bitmap)img))
                using (var page = engine.Process(pix, PageSegMode.Auto))
                {
                    string text = page.GetText();

                    System.Diagnostics.Debug.WriteLine("OCR TEXT LENGTH: " + text.Length);

                    return text;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("OCR FAILED: " + ex.Message);
                return "";
            }
        }

        // ✅ DOCX
        private static string ExtractFromDocx(HttpPostedFileBase file)
        {
            try
            {
                file.InputStream.Position = 0;

                using (var doc = WordprocessingDocument.Open(file.InputStream, false))
                {
                    return doc.MainDocumentPart.Document.Body.InnerText;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DOCX ERROR: " + ex.Message);
                return "";
            }
        }
    }
}