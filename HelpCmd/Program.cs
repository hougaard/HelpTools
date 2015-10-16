using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Compilation;
using System.Web.UI.WebControls;
using HelpTools;
using System.Runtime.InteropServices;
using System.Web;
using System.Drawing.Imaging;
using System.Drawing;

namespace HelpTest
{
    class Program
    {
        public static void BuildManual()
        {
           
            ManualBuilder m = new ManualBuilder();
            m.LoadTemplates(HelpTools.Configuration.Parms.template_xml);

            File.WriteAllText(HelpTools.Configuration.Parms.projectname + ".tex",m.GenerateManual());
            m.CreatePDFfromLatex(HelpTools.Configuration.Parms.projectname + ".tex");

            File.Copy(
                HelpTools.Configuration.Parms.projectname + ".pdf",
                HelpTools.Configuration.Parms.output_path_manual + @"\" +
                HelpTools.Configuration.Parms.projectname + ".pdf",true);
        }

        public static void BuildHelpFile()
        {
            HelpFileBuilder h = new HelpFileBuilder();
            h.GenerateAllContentAsHtml();
        }
        private static void Main(string[] args)
        {
            /*
            ScreenShot ss = new ScreenShot();
            ss.StartNAV("eh-x1t:7546", "ff81_local", "Foqus Finance R8 Development");
            Bitmap b = ss.GenerateScreenShot(6030550);
            b.Save("temp1.png",ImageFormat.Png);
            b = ss.GenerateScreenShot(6030551);
            b.Save("temp2.png", ImageFormat.Png);
            */

            HelpTools.Configuration.Parms = HelpTools.Configuration.LoadFromAppConfig();
            
            Data.LoadContentXml(HelpTools.Configuration.Parms.content_xml);
            Data.LoadManualStructure(HelpTools.Configuration.Parms.structure_xml);

            Console.WriteLine("Building manual");
            try
            {
                BuildManual();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);                    
            }
            Console.WriteLine("Building helpserver files");
            try
            {
                BuildHelpFile();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            Console.WriteLine("Done");
        }
    }
}
