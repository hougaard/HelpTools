using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HelpTools;
using Microsoft.Win32;
using System.Threading;

namespace HelpGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Configuration.Parms = new Parameters();
            ConfigBox.DataContext = Configuration.Parms;
            AbortButton.Visibility = Visibility.Collapsed;
            Progress.Visibility = Visibility.Collapsed;
        }
        private string CurrentLoadedConfig = "";
        private void LoadButton_OnClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "*.xml|*.xml";
            if (ofd.ShowDialog() == true)
            {
                CurrentLoadedConfig = ofd.FileName;
                Configuration.Parms = Configuration.Load(CurrentLoadedConfig);
                ConfigBox.DataContext = null;
                ConfigBox.DataContext = Configuration.Parms;
                ConfigBox.Header = "Config: " + System.IO.Path.GetFileName(CurrentLoadedConfig);
            }
        }
        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "*.xml|*.xml";
            if (sfd.ShowDialog() == true)
            {
                CurrentLoadedConfig = sfd.FileName;
                Configuration.Parms.Save(CurrentLoadedConfig);
                ConfigBox.Header = "Config: " + System.IO.Path.GetFileName(CurrentLoadedConfig);
            }
        }

        private StringWriter tw;
        private AbortableBackgroundWorker bw;
        private void RunManual_OnClick(object sender, RoutedEventArgs e)
        {
            bw = new AbortableBackgroundWorker();
            bw.DoWork += BackgroundMakeManual;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            Progress.Visibility = Visibility.Visible;
            ConfigBox.IsEnabled = false; 
            AbortButton.Visibility = Visibility.Visible;
            bw.RunWorkerAsync();
        }

        private void BackgroundMakeManual(object sender, DoWorkEventArgs doWorkEventArgs)
        {        
            tw = new StringWriter();
            Console.SetOut(tw);

            Data.LoadContentXml(Configuration.Parms.content_xml);
            Data.LoadManualStructure(Configuration.Parms.structure_xml);

            ManualBuilder m = new ManualBuilder();
            m.LoadTemplates(HelpTools.Configuration.Parms.template_xml);

            File.WriteAllText(HelpTools.Configuration.Parms.projectname + ".tex", m.GenerateManual());
            m.CreatePDFfromLatex(HelpTools.Configuration.Parms.projectname + ".tex");

            File.Copy(
                HelpTools.Configuration.Parms.projectname + ".pdf",
                HelpTools.Configuration.Parms.output_path_manual + @"\" +
                HelpTools.Configuration.Parms.projectname + ".pdf", true);
        }

        private void RunHelpServer_OnClick(object sender, RoutedEventArgs e)
        {
            bw = new AbortableBackgroundWorker();
            bw.DoWork += BackgroundMakeHelpFile;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
            Progress.Visibility = Visibility.Visible;
            ConfigBox.IsEnabled = false; 
            bw.RunWorkerAsync();
        }

        void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Progress.Visibility = Visibility.Collapsed;
            Log.ItemsSource = null;
            Log.ItemsSource = tw.ToString().Split('\n');
            ConfigBox.IsEnabled = true;
            AbortButton.Visibility = Visibility.Collapsed;
        }
        private void BackgroundMakeHelpFile(object sender, DoWorkEventArgs doWorkEventArgs)
        {
            tw = new StringWriter();
            Console.SetOut(tw);
            Data.LoadContentXml(Configuration.Parms.content_xml);
            Data.LoadManualStructure(Configuration.Parms.structure_xml);
            HelpFileBuilder h = new HelpFileBuilder();
            h.GenerateAllContentAsHtml();
        }

        private void AbortButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (bw != null && bw.IsBusy)
            {
                bw.Abort();
                bw.Dispose();
                Progress.Visibility = Visibility.Collapsed;
                Log.ItemsSource = null;
                Log.ItemsSource = tw.ToString().Split('\n');
                ConfigBox.IsEnabled = true;
                AbortButton.Visibility = Visibility.Collapsed;
            }
        }
    }
    public class AbortableBackgroundWorker : BackgroundWorker
    {

        private Thread workerThread;

        protected override void OnDoWork(DoWorkEventArgs e)
        {
            workerThread = Thread.CurrentThread;
            try
            {
                base.OnDoWork(e);
            }
            catch (ThreadAbortException)
            {
                e.Cancel = true; //We must set Cancel property to true!
                Thread.ResetAbort(); //Prevents ThreadAbortException propagation
            }
        }


        public void Abort()
        {
            if (workerThread != null)
            {
                workerThread.Abort();
                workerThread = null;
            }
        }
    }
}
