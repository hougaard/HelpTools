using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HelpTools
{
    public static class Data
    {
        public static List<Content> Contents; // XML file from NAV
        public static List<Article> Articles; // XML file from NAV
        public static Manual manual;
        public static void LoadContentXml(string FileName)
        {
            var HelpTextXml = XDocument.Load(FileName);
            Contents = (from _content in HelpTextXml.Element("Help").Element("Contents").Elements("Content")
                        select new Content
                        {
                            ParentType = _content.Element("ParentType").Value,
                            ParentID = _content.Element("ParentID").Value,
                            Type = _content.Element("Type").Value,
                            ID = _content.Element("ID").Value,
                            Language = _content.Element("Language").Value,
                            Version = _content.Element("Version").Value,
                            Caption = _content.Element("Caption").Value,
                            Description = _content.Element("Description").Value,
                            AdditionalInfo = _content.Element("AdditionalInfo").Value,
                            Tasks = _content.Element("Tasks").Value
                        }).ToList();
            Articles = (from article in HelpTextXml.Element("Help").Element("Articles").Elements("Article")
                        select new Article
                        {
                            ID = article.Element("Keyword").Value,
                            Title = article.Element("Title").Value,
                            Language = article.Element("Language").Value,
                            Version = article.Element("Version").Value,
                            Text = article.Element("ArticleText").Value
                        }).ToList();
        }
        public static void LoadManualStructure(string FileName)
        {
            var StructureXml = XDocument.Load(FileName);
            manual = (from _manual in StructureXml.Elements("Manual")
                      select new Manual
                      {
                          Title = _manual.Element("Info").Element("Title").Value,
                          Author = _manual.Element("Info").Element("Author").Value,
                          Preface = new Topic()
                          {
                              Type = _manual.Element("Info").Element("Preface").Attribute("Type").Value,
                              ID = _manual.Element("Info").Element("Preface").Attribute("ID").Value
                          },
                          Chapters = (from _chapter in _manual.Element("Chapters").Elements("Chapter")
                                      let xElement = _chapter.Element("Preface") ?? new XElement("Preface")
                                      select new Chapter
                                      {
                                          No = _chapter.Attribute("No").Value,
                                          Title = _chapter.Attribute("Title").Value,
                                          Preface = new Topic()
                                          {
                                              Type = xElement.Attribute("Type").Value,
                                              ID = xElement.Attribute("ID").Value,
                                              FileTitle =  xElement.Attribute("FileTitle") != null ? xElement.Attribute("FileTitle").Value : ""
                                          },
                                          Topics = (from _topic in _chapter.Element("Topics").Elements("Topic")
                                                    let element = _topic.Element("SubTopics") ?? new XElement("SubTopics")
                                                    select new Topic
                                                    {
                                                        Type = _topic.Attribute("Type").Value,
                                                        ID = _topic.Attribute("ID").Value,
                                                        FileTitle =  _topic.Attribute("FileTitle") != null ? _topic.Attribute("FileTitle").Value : "",
                                                        SubTopics = (from _sub in element.Elements("SubTopic")
                                                                     select new Topic
                                                                     {
                                                                         Type = _sub.Attribute("Type").Value,
                                                                         ID = _sub.Attribute("ID").Value,
                                                                          FileTitle =  _sub.Attribute("FileTitle") != null ? _sub.Attribute("FileTitle").Value : ""
                                                                     }).ToList()
                                                    }).ToList()
                                      }).ToList()
                      }).First() as Manual;
        }
        
    }
    public class ManualBuilder
    {
        public ManualBuilder()
        {
            
        }
        // Content
        private Template Templates;
        public void LoadTemplates(string FileName)
        {
            XDocument TemplateXml = XDocument.Load(FileName);
            Templates = (from _template in TemplateXml.Elements("Template")
                select new Template
                {
                    ChapterBetweenTopics = _template.Element("ChapterBetweenTopics").Value,
                    ChapterHead = _template.Element("ChapterHead").Value,
                    Field = _template.Element("Field").Value,
                    FieldEntry = _template.Element("FieldEntry").Value,
                    ManualEnd = _template.Element("ManualEnd").Value,
                    ManualStart = _template.Element("ManualStart").Value,
                    TableTopicFields = _template.Element("TableTopicFields").Value,
                    TableTopicHead = _template.Element("TableTopicHead").Value,
                    TableTopicIntro = _template.Element("TableTopicIntro").Value,
                    ArticleTopicHead = _template.Element("ArticleTopicHead").Value,
                    ArticleTopicText = _template.Element("ArticleTopicText").Value,
                    //Preface = _template.Element("Preface").Value
                }).First();
        }
        public string GenerateTable(string ID,string SectionType)
        {
            Content table = Data.Contents.Find(m => m.ID == ID);
            List<Content> fields = Data.Contents.FindAll(m => m.ParentType == "Table" && m.ParentID == ID);

            if (table == null)
            {
                // Create placeholder
                table = new Content();
                table.Caption = "TODO: Needs caption for table " + ID;
                table.Description = "TODO: Needs description for table " + ID;
            }

            StringBuilder topic = new StringBuilder();
            topic.Append(Templates.TableTopicHead);
            topic.Replace("$2$", SectionType);
            topic.Replace("$1$", table.Caption.Replace("%", @"\%"));
            topic.Append(Templates.TableTopicIntro);
            topic.Replace("$1$", ConvertMarkdown(table.Description, "latex"));

            StringBuilder fieldlist = new StringBuilder();
            foreach (var field in fields)
            {
                var f = new StringBuilder(Templates.Field);
                f.Replace("$1$", field.Caption.Replace("%",@"\%"));
                f.Replace("$2$", ConvertMarkdown(field.Description, "latex"));
                f.Replace("$3$", field.Caption.Replace("%", @"\%"));
                var fieldentry = new StringBuilder(Templates.FieldEntry);
                fieldentry.Replace("$1$", f.ToString());
                fieldlist.Append(fieldentry);
            }
            var fieldlistsection = new StringBuilder(Templates.TableTopicFields);
            fieldlistsection.Replace("$1$", fieldlist.ToString());

            topic.Append(fieldlistsection);

            return topic.ToString();
        }
        public string GenerateTopic(Topic t)
        {
            Console.WriteLine("Generate Topic {0} ({1})", t.ID,t.Type);
            switch (t.Type)  // Table,Page,Report,XmlPort,Article,External
            {
                case "File":
                    return GenerateFile(t.ID, "section",t.FileTitle) + GenerateSubTopics(t);
                case "Table":
                    return GenerateTable(t.ID,"section") + GenerateSubTopics(t);
                case "Article":
                    return GenerateArticle(t.ID, "section") + GenerateSubTopics(t);
                case "Page":
                    return GeneratePage(t.ID, "section") + GenerateSubTopics(t);
                case "Report":
                    return GenerateReport(t.ID, "section") + GenerateSubTopics(t);
                case "XmlPort":
                    return GenerateXmlPort(t.ID, "section") + GenerateSubTopics(t);
                case "External":
                    return GenerateExternal(t.ID, "section") + GenerateSubTopics(t);
            }
            return "TODO:";
        }
        public string GenerateSubTopics(Topic t)
        {
            StringBuilder s = new StringBuilder();
            foreach (var sub in t.SubTopics)
            {
                Console.WriteLine("Generate SubTopic {0} ({1})", sub.ID, sub.Type);
                switch (sub.Type)  // Table,Page,Report,XmlPort,Article,External
                {
                    case "File":
                        s.Append(GenerateFile(t.ID, "section", t.FileTitle));
                        break;
                    case "Table":
                        s.Append(GenerateTable(sub.ID,"subsection"));
                        break;
                    case "Article":
                        s.Append(GenerateArticle(sub.ID, "subsection"));
                        break;
                    case "Page":
                        s.Append(GeneratePage(sub.ID,"subsection"));
                        break;
                    case "Report":
                        s.Append(GenerateReport(sub.ID, "subsection"));
                        break;
                    case "XmlPort":
                        s.Append(GenerateXmlPort(sub.ID, "subsection"));
                        break;
                    case "External":
                        s.Append(GenerateExternal(sub.ID, "subsection"));
                        break;
                }
            }
            return s.ToString();
        }

        private string GenerateExternal(string p, string SectionType)
        {
            throw new NotImplementedException();
        }

        private string GenerateXmlPort(string p, string SectionType)
        {
            throw new NotImplementedException();
        }

        private string GenerateReport(string p, string SectionType)
        {
            throw new NotImplementedException();
        }

        private string GeneratePage(string p, string SectionType)
        {
            throw new NotImplementedException();
        }
        private string GenerateFile(string FileName, string SectionType,string Title)
        {
            //Article article = Data.Articles.Find(m => m.ID == ID);

            StringBuilder topic = new StringBuilder();
            topic.Append(Templates.ArticleTopicHead);
            topic.Replace("$2$", SectionType);
            topic.Replace("$1$", Title);
            topic.Append(Templates.ArticleTopicText);
            topic.Replace("$1$", ConvertMarkdown(File.ReadAllText(Configuration.Parms.input_markdown_path + FileName), "latex"));
            return topic.ToString();
        }

        private string GenerateArticle(string ID,string SectionType)
        {
            Article article = Data.Articles.Find(m => m.ID == ID);

            if (article == null)
            {
                // Create placeholder
                article = new Article();
                article.Title = "TODO: Needs an article about " + ID;
                article.Text = "TODO: Needs text in article " + ID;
            }

            StringBuilder topic = new StringBuilder();
            topic.Append(Templates.ArticleTopicHead);
            topic.Replace("$2$", SectionType);
            topic.Replace("$1$", article.Title);
            topic.Append(Templates.ArticleTopicText);
            topic.Replace("$1$", ConvertMarkdown(article.Text, "latex"));
            return topic.ToString();
        }

        public string GenerateChapter(Chapter chapter)
        {
            StringBuilder c = new StringBuilder(Templates.ChapterHead);

            Article preface = null;

            if (chapter.Preface.Type == "Article")
            {
                preface = Data.Articles.Find(m => m.ID == chapter.Preface.ID);
                if (preface == null)
                {
                    preface = new Article()
                    {
                        Text = "TODO: Article " + chapter.Preface.ID,
                        Title = "TODO: Article Caption " + chapter.Preface.ID
                    };
                }
            }
            if (chapter.Preface.Type == "File")
            {
                if (chapter.Preface.ID.Length > 3)
                {
                    preface = new Article()
                    {
                        Text = File.ReadAllText(Configuration.Parms.input_markdown_path + chapter.Preface.ID),
                        Title = chapter.Preface.FileTitle
                    };
                }
                else
                {
                    preface = new Article()
                    {
                        Text = "TODO: File",
                        Title = "TODO: File Caption (FileTitle)"
                    };
                }
            }
            c.Replace("$1$", chapter.Title.Replace("%", @"\%"));
            c.Replace("$2$", ConvertMarkdown(preface.Text, "latex"));
            foreach (var t in chapter.Topics)
            {
                c.Append(GenerateTopic(t));
                c.Append(Templates.ChapterBetweenTopics);
            }
            return c.ToString();
        }

        public string GenerateManual()
        {
            var m = new StringBuilder(Templates.ManualStart);
            m.Replace("$1$", Data.manual.Title);
            m.Replace("$2$", Data.manual.Author);
            //m.Replace("$3$", Configuration.Parms.input_images_path.Replace(@"\",@"/"));

            if (!string.IsNullOrEmpty(Data.manual.Preface.Type))
            {
                if (Data.manual.Preface.Type == "Article")
                    m.Append(GenerateArticle(Data.manual.Preface.ID, "chapter*"));
                if (Data.manual.Preface.Type == "File")
                    m.Append(GenerateFile(Data.manual.Preface.ID, "chapter*",Data.manual.Preface.FileTitle));
            }
            foreach (var chapter in Data.manual.Chapters)
            {
                var c = new StringBuilder(GenerateChapter(chapter));
                m.Append(c);
            }
            var e = new StringBuilder(Templates.ManualEnd);
            m.Append(e);

            // \includegraphics{./
            // \includegraphics[width=\linewidth,height=\textheight,keepaspectratio]{./FF_RoleCenter.JPG}

            m.Replace(@"\begin{figure}[htbp]", @"\begin{figure}[h]");

            m.Replace(@"\includegraphics{",
                @"\includegraphics[width=\linewidth,height=\textheight,keepaspectratio]{" + 
                Configuration.Parms.input_images_path.Replace(@"\","/"));
            return m.ToString();
        }
        
        public bool CreatePDFfromLatex(string InputLatexFile) // Output PDF file with same name(.PDF)
        {
            string processName = Configuration.Parms.miktex_path + @"bin\texify.exe";
            string args = String.Format(@"--clean --verbose  --pdf --engine=xetex {0}", InputLatexFile);

            ProcessStartInfo psi = new ProcessStartInfo(processName, args);

            psi.RedirectStandardOutput = true;
            psi.CreateNoWindow = false;
            Process p = new Process();
            p.StartInfo = psi;           
            psi.UseShellExecute = false;
            p.Start();

            string outputString = "";
            
            p.WaitForExit(2000);
            using (System.IO.StreamReader sr = new System.IO.StreamReader(
                p.StandardOutput.BaseStream))
            {

                outputString = sr.ReadToEnd();
            }
            Console.WriteLine(outputString);
            return outputString.IndexOf("Output written on ") != -1;
        }
        public string ConvertMarkdown(string markdown,string output)
        {
            string processName = Configuration.Parms.pandoc_path + @"pandoc.exe";
            string args = String.Format(@"-r markdown -t {0}",output);

            ProcessStartInfo psi = new ProcessStartInfo(processName, args);

            psi.RedirectStandardOutput = true;
            psi.RedirectStandardInput = true;
            psi.CreateNoWindow = true;

            Process p = new Process();
            p.StartInfo = psi;
            psi.UseShellExecute = false;
            p.Start();

            string outputString = "";
            byte[] inputBuffer = Encoding.UTF8.GetBytes(markdown);
            p.StandardInput.BaseStream.Write(inputBuffer, 0, inputBuffer.Length);
            p.StandardInput.Close();

            p.WaitForExit(2000);
            using (System.IO.StreamReader sr = new System.IO.StreamReader(
                p.StandardOutput.BaseStream))
            {

                outputString = sr.ReadToEnd();
            }

            return outputString;
        }
    }

    public class Template
    {
        public string TableTopicHead { get; set; }
        public string TableTopicIntro { get; set; }
        public string TableTopicFields { get; set; }
        public string Field { get; set; }
        public string FieldEntry { get; set; }
        public string ChapterHead { get; set; }
        public string ChapterBetweenTopics { get; set; }
        public string ManualStart { get; set; }
        public string ManualEnd { get; set; }
        public string ArticleTopicHead { get; set; }
        public string ArticleTopicText { get; set; }
        public string Preface { get; set; }
    }
}
