using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HelpTools
{
    public class Node
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Page { get; set; }        
    }
    public class HelpFileBuilder
    {
        public HelpFileBuilder()
        {
        }
        public XElement GetNode(string Name, string DisplayName, string Page)
        {
            return new XElement("Node", null, new XAttribute("Name", Name),
                    new XAttribute("DisplayName", DisplayName), new XAttribute("Page", Page));
        }
        public bool GenerateAllContentAsHtml()
        {
            XDocument TOC = XDocument.Load(Configuration.Parms.input_toc);

            XElement root = (from xml2 in TOC.Descendants("Node")
                             where xml2.Attribute("Name").Value == Configuration.Parms.projectname
                             select xml2).FirstOrDefault();
            if (root == null)
            {
                root = GetNode(Configuration.Parms.projectname, Data.manual.Title, Configuration.Parms.projectname + ".htm");
                TOC.Element("Node").Nodes().FirstOrDefault().AddAfterSelf(root);
            }
            root.Nodes().Remove();

            StringBuilder ChapterList = new StringBuilder();
            foreach (var chapter in Data.manual.Chapters)
            {
                root.Add(GenerateChapter(chapter));
                string str = "<a href=\"" + Configuration.Parms.projectname + "_" + chapter.No + ".htm\" xmlns=\"http://ddue.schemas.microsoft.com/authoring/2003/5\">";
                str += chapter.Title + "</a><br>";
                ChapterList.Append(str);
            }

            Article preface = Data.Articles.Find(m => m.ID == Data.manual.Preface.ID);
            if (preface == null)
            {
                preface = new Article()
                {
                    ID = Data.manual.Preface.ID,
                    Text = "TODO: Article " + Data.manual.Preface.ID,
                    Title = "TODO: Article Caption " + Data.manual.Preface.ID
                };
            }
            StringBuilder topic = new StringBuilder(File.ReadAllText(Configuration.Parms.helppage_html));
            topic.Replace("$1$", preface.Title);
            topic.Replace("$1$", preface.Title);
            topic.Replace("$2$", ConvertMarkdown(preface.Text, "html") + ChapterList);
            File.WriteAllText(
                Configuration.Parms.output_path_helpserver + @"\" + 
                Configuration.Parms.projectname + ".htm", topic.ToString());

            TOC.Save(Configuration.Parms.output_toc);
            return false;
        }
        public XElement GenerateChapter(Chapter chapter)
        {
            XElement node = GetNode(Configuration.Parms.projectname + "_" + chapter.No, chapter.Title, Configuration.Parms.projectname + "_" + chapter.No + ".htm");

            StringBuilder c = new StringBuilder(File.ReadAllText(Configuration.Parms.helppage_html));
            Article preface = Data.Articles.Find(m => m.ID == chapter.Preface.ID);
            if (preface == null)
            {
                preface = new Article()
                {
                    ID = chapter.Preface.ID,
                    Text = "TODO: Article " + chapter.Preface.ID,
                    Title = "TODO: Article Caption " + chapter.Preface.ID
                };
            }
            c.Replace("$1$", chapter.Title.Replace("%", @"\%"));
            c.Replace("$1$", chapter.Title.Replace("%", @"\%"));
            c.Replace("$2$", ConvertMarkdown(preface.Text, "html"));
            File.WriteAllText(Configuration.Parms.output_path_helpserver + @"\" +
                Configuration.Parms.projectname + "_" + chapter.No + ".htm", c.ToString());
            foreach (var t in chapter.Topics)
            {
                XElement subnode = GenerateTopic(t);
                node.Add(subnode);
            }
            return node;
        }
        public XElement GenerateTable(Topic t)
        {
            Content table = Data.Contents.Find(m => m.ID == t.ID);
            List<Content> fields = Data.Contents.FindAll(m => m.ParentType == "Table" && m.ParentID == t.ID);

            if (table == null)
            {
                // Create placeholder
                table = new Content();
                table.ID = t.ID;
                table.Caption = "TODO: Needs caption for table " + t.ID;
                table.Description = "TODO: Needs description for table " + t.ID;
            }
            XElement node = GetNode(Configuration.Parms.projectname + "_" + t.ID, table.Caption, "T_" + table.ID + ".htm");

            StringBuilder topic = new StringBuilder(File.ReadAllText(Configuration.Parms.helppage_html));
            topic.Replace("$1$", table.Caption);
            topic.Replace("$1$", table.Caption);
            StringBuilder fieldlist = new StringBuilder();
            foreach (var field in fields)
            {
                // <a href="./T_250.htm" xmlns="http://ddue.schemas.microsoft.com/authoring/2003/5">
                string Fieldstr = "<a href=\"T_" + table.ID + "_" + field.ID +
                               ".htm\" xmlns=\"http://ddue.schemas.microsoft.com/authoring/2003/5\">";
                Fieldstr += field.Caption + "</a><br>";
                fieldlist.Append(Fieldstr);
                StringBuilder FieldPage = new StringBuilder(File.ReadAllText(Configuration.Parms.helppage_html));
                FieldPage.Replace("$1$", field.Caption);
                FieldPage.Replace("$1$", field.Caption);
                FieldPage.Replace("$2$", ConvertMarkdown(field.Description,"html"));
                File.WriteAllText(Configuration.Parms.output_path_helpserver + @"\" +
                "T_" + table.ID + "_" + field.ID + ".htm", FieldPage.ToString());
            }
            topic.Replace("$2$", ConvertMarkdown(table.Description, "html") + fieldlist);

            File.WriteAllText(Configuration.Parms.output_path_helpserver + @"\" +
                "T_" + table.ID + ".htm", topic.ToString());

            GenerateSubTopics(t, node);
            return node;
        }
        public XElement GenerateTopic(Topic t)
        {
            Console.WriteLine("Generate Topic {0} ({1})", t.ID, t.Type);
            switch (t.Type)  // Table,Page,Report,XmlPort,Article,External
            {
                case "Table":
                    return GenerateTable(t);
                case "Article":
                    return GenerateArticle(t);
                case "Page":
                    return GeneratePage(t);
                case "Report":
                    return GenerateReport(t);
                case "XmlPort":
                    return GenerateXmlPort(t);
                case "External":
                    return GenerateExternal(t);
            }
            return null;
        }
        public bool GenerateSubTopics(Topic t,XElement ParentNode)
        {
            if (t.SubTopics == null || t.SubTopics.Count == 0)
                return false;
            StringBuilder s = new StringBuilder();
            foreach (var sub in t.SubTopics)
            {
                Console.WriteLine("Generate SubTopic {0} ({1})", sub.ID, sub.Type);
                switch (sub.Type)  // Table,Page,Report,XmlPort,Article,External
                {
                    case "Table":
                        ParentNode.Add(GenerateTable(sub));
                        break;
                    case "Article":
                        ParentNode.Add(GenerateArticle(sub));
                        break;
                    case "Page":
                        ParentNode.Add(GeneratePage(sub));
                        break;
                    case "Report":
                        ParentNode.Add(GenerateReport(sub));
                        break;
                    case "XmlPort":
                        ParentNode.Add(GenerateXmlPort(sub));
                        break;
                    case "External":
                        ParentNode.Add(GenerateExternal(sub));
                        break;
                }
            }
            return true;
        }
        private XElement GenerateExternal(Topic t)
        {
            throw new NotImplementedException();
        }
        private XElement GenerateXmlPort(Topic t)
        {
            throw new NotImplementedException();
        }
        private XElement GenerateReport(Topic t)
        {
            throw new NotImplementedException();
        }
        private XElement GeneratePage(Topic t)
        {
            throw new NotImplementedException();
        }
        private XElement GenerateArticle(Topic t)
        {
            Article article = Data.Articles.Find(m => m.ID == t.ID);
            if (article == null)
            {
                // Create placeholder
                article = new Article();
                article.Title = "TODO: Needs an article about " + t.ID;
                article.Text = "TODO: Needs text in article " + t.ID;
            }
            XElement node = GetNode(Configuration.Parms.projectname + "_" + t.ID, article.Title, Configuration.Parms.projectname + "_" + t.ID + ".htm");

            StringBuilder topic = new StringBuilder(File.ReadAllText(Configuration.Parms.helppage_html));
            topic.Replace("$1$", article.Title);
            topic.Replace("$1$", article.Title);
            topic.Replace("$2$", ConvertMarkdown(article.Text, "html"));
            File.WriteAllText(Configuration.Parms.output_path_helpserver + @"\" +
                Configuration.Parms.projectname + "_" + t.ID + ".htm", topic.ToString());

            GenerateSubTopics(t,node);
            return node;
        }
        public string ConvertMarkdown(string markdown, string output)
        {
            string processName = Configuration.Parms.pandoc_path + @"pandoc.exe";
            string args = String.Format(@"-r markdown -t {0}", output);

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

            return outputString.Replace("<img src=\"",
                "<img "+ Configuration.Parms.image_style_helppage +" src=\"" + Configuration.Parms.help_server_image_path);
        }

    }
}
