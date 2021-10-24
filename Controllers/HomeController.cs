using ImportFileOfxAri.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ImportFileOfxAri.Controllers
{
    public class HomeController : Controller
    {
        private IHostingEnvironment Environment;

        public HomeController(IHostingEnvironment _environment)
        {
            Environment = _environment;
        }

        public IActionResult Index()
        {

            List<Import> imp = new List<Import>();

            imp.Add(new Import()
            {
                DataTarnsacao = "",
                Tipo = "",
                Valor = "",
                Descricao = ""

            });


            return View(imp);
        }



        [HttpPost]
        public IActionResult Index(List<IFormFile> postedFiles)
        {
            string wwwPath = this.Environment.WebRootPath;
            string contentPath = this.Environment.ContentRootPath;

            string path = Path.Combine(this.Environment.WebRootPath, "Uploads");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            List<string> uploadedFiles = new List<string>();
            List<Import> registros = new List<Import>();


            foreach (IFormFile postedFile in postedFiles)
            {
                string fileName = Path.GetFileName(postedFile.FileName);
                using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    postedFile.CopyTo(stream);
                    uploadedFiles.Add(fileName);
                    ViewBag.Message += string.Format("<b>{0}</b> Upload concluido com sucesso.<br />", fileName);

                    stream.Dispose();

                    registros.AddRange(toXElement(Path.Combine(path, fileName)));

                }
            }


            return View(registros);
        }

        public List<Import> toXElement(string pathToOfxFile)
        {
            if (!System.IO.File.Exists(pathToOfxFile))
            {
                throw new FileNotFoundException();
            }


            var tags = from line in System.IO.File.ReadAllLines(pathToOfxFile)
                       where
                       line.Contains("<STMTTRN>") ||
                       line.Contains("<TRNTYPE>") ||
                       line.Contains("<DTPOSTED>") ||
                       line.Contains("<TRNAMT>") ||
                       line.Contains("<MEMO>")
                       select line;


            XElement el = new XElement("root");
            XElement son = null;

            List<Import> import = new List<Import>();
            foreach (var l in tags)
            {
                if (l.IndexOf("<STMTTRN>") != -1)
                {
                    son = new XElement("STMTTRN");


                    el.Add(son);
                    continue;
                }

                var tagName = getTagName(l);
                var elSon = new XElement(tagName);
                elSon.Value = getTagValue(l);



                son.Add(elSon);
            }

            var doc = new XDocument(el);


            List<Import> list = doc.Elements("root").Elements("STMTTRN").Select(sv => new Import()

            {
                DataTarnsacao = new DateTime(Convert.ToInt16(sv.Element("DTPOSTED").Value.Substring(0, 4)), Convert.ToInt16(sv.Element("DTPOSTED").Value.Substring(4, 2)), Convert.ToInt16(sv.Element("DTPOSTED").Value.Substring(6, 2))).ToString("dd/MM/yyyy"),
                Tipo = sv.Element("TRNTYPE").Value,
                Valor = sv.Element("TRNAMT").Value,
                Descricao = sv.Element("MEMO").Value

            }).ToList();


            list = list.Select(i => new { i.DataTarnsacao, i.Tipo, i.Valor, i.Descricao })
               .Distinct().Select(x => new Import { DataTarnsacao = x.DataTarnsacao, Tipo = x.Tipo, Valor = x.Valor, Descricao = x.Descricao }).ToList();



            return list;

        }

        private static string getTagName(string line)
        {
            int pos_init = line.IndexOf("<") + 1;
            int pos_end = line.IndexOf(">");
            pos_end = pos_end - pos_init;
            return line.Substring(pos_init, pos_end);
        }

        private static string getTagValue(string line)
        {
            int pos_init = line.IndexOf(">") + 1;
            string retValue = line.Substring(pos_init).Trim();
            if (retValue.IndexOf("[") != -1)
            {
                retValue = retValue.Substring(0, 8);
            }
            return retValue;
        }





        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }



    }
}
