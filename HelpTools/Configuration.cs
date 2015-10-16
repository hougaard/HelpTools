using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Soap;
using System.Reflection;
using System.IO;

namespace HelpTools
{
    public static class Configuration
    {
        public static Parameters Parms;
        public static Parameters LoadFromAppConfig()
        {
            return new Parameters()
            {
                content_xml = ConfigurationManager.AppSettings["content-xml"],
                structure_xml = ConfigurationManager.AppSettings["structure-xml"],
                miktex_path = ConfigurationManager.AppSettings["miktex-path"],
                pandoc_path = ConfigurationManager.AppSettings["pandoc-path"],
                template_xml = ConfigurationManager.AppSettings["template-xml"],
                projectname = ConfigurationManager.AppSettings["projectname"],
                helppage_html = ConfigurationManager.AppSettings["helppage-html"],
                input_toc = ConfigurationManager.AppSettings["input-toc"],
                output_toc = ConfigurationManager.AppSettings["output-toc"],
                output_path_helpserver = ConfigurationManager.AppSettings["output-path-helpserver"],
                output_path_manual = ConfigurationManager.AppSettings["output-path-manual"],
                input_markdown_path = ConfigurationManager.AppSettings["input-markdown-path"],
                input_images_path = ConfigurationManager.AppSettings["input-images-path"],
                help_server_image_path = ConfigurationManager.AppSettings["help-server-image-path"],
                image_style_helppage = ConfigurationManager.AppSettings["image-style-helppage"]
            };
        }
        public static Parameters Load(string filename)
        {
            Parameters p = new Parameters();
            PropertyInfo[] properties = typeof(Parameters).GetProperties();
            object[,] a;
            Stream f = File.Open(filename, FileMode.Open);
            SoapFormatter formatter = new SoapFormatter();
            a = formatter.Deserialize(f) as object[,];
            f.Close();
            //if (a.GetLength(0) != properties.Length) return null;
            int i = 0;
            foreach (PropertyInfo property in properties)
            {
                try
                {
                    if (property.Name == (a[i, 0] as string))
                    {
                        property.SetValue(p, a[i, 1]);
                    }
                }
                catch
                {
                }
                i++;
            };
            return p;
        }
    }
    public class Parameters
    {
        public string projectname { get; set; }
        public string structure_xml { get; set; }
        public string template_xml { get; set; }
        public string content_xml { get; set; }
        public string miktex_path { get; set; }
        public string pandoc_path { get; set; }
        public string helppage_html { get; set; }
        public string input_toc { get; set; }
        public string output_toc { get; set; }
        public string output_path_helpserver { get; set; }
        public string output_path_manual { get; set; }
        public string input_markdown_path { get; set; }
        public string input_images_path { get; set; }
        public string help_server_image_path { get; set; }
        public string image_style_helppage { get; set; }
        public bool Save(string filename)
        {
            try
            {
                PropertyInfo[] properties = this.GetType().GetProperties();
                object[,] a = new object[properties.Length, 2];
                int i = 0;
                foreach (PropertyInfo pi in properties)
                {
                    a[i, 0] = pi.Name;
                    a[i, 1] = pi.GetValue(this,null);
                    i++;
                };
                Stream f = File.Open(filename, FileMode.Create);
                SoapFormatter formatter = new SoapFormatter();
                formatter.Serialize(f, a);
                f.Close();
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
    }
}
