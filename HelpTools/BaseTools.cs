using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpTools
{
    public class Topic
    {
        public string Type { get; set; } // Table,Page,Report,XmlPort,Article,External - Should be an Enum at some point, not sure yet of values
        public string ID { get; set; }
        public List<Topic> SubTopics { get; set; }
        public string FileTitle { get; set; }
    }
    public class Chapter
    {
        public string No { get; set; }
        public string Title { get; set; }
        public string ChapterBackground { get; set; }
        public Topic Preface { get; set; }
        public List<Topic> Topics { get; set; }
    }
    public class Manual
    {
        public string Title { get; set; }
        public string Author { get; set; }

        public Topic Preface { get; set; }

        public string FrontPageBackground { get; set; }

        public List<Chapter> Chapters { get; set; }
    }
    public class Content
    {
        public string ParentType { get; set; }
        public string ParentID { get; set; }
        public string Type { get; set; }
        public string ID { get; set; }
        public string Language { get; set; }
        public string Version { get; set; }
        public string Caption { get; set; }
        public string Description { get; set; }
        public string AdditionalInfo { get; set; }
        public string Tasks { get; set; }
    }
    public class Article
    {
        public string ID { get; set; }
        public string Title { get; set; }
        public string Language { get; set; }
        public string Version { get; set; }
        public string Text { get; set; }
    }

}
