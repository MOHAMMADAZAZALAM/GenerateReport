using DirectShowLib;
using Emgu.CV;
using GenerateReport.Properties;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TheArtOfDev.HtmlRenderer.PdfSharp;
//using Finisar.SQLite;
using System.Text.RegularExpressions;

namespace GenerateReport
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static readonly object _object = new object();
      
        int i = 0;
     
        public string baseUri = "";
     
        System.Timers.Timer loginTimer = new System.Timers.Timer();

        System.Timers.Timer cameraTimer = new System.Timers.Timer();
        public static bool windowOpen = false;
       // private SparkleUpdater _sparkle;
        
        DirectShowLib.DsDevice[] allCameras = null;
       
        //  FaceClient _faceClient;
        //private static readonly ImageEncodingParam[] s_jpegParams = {
        //    new ImageEncodingParam(OpenCvSharp.ImwriteFlags.JpegQuality, 60)
        //};
        public System.Timers.Timer timer = new System.Timers.Timer(1000);
        public static bool cameraON = false;
        private Emgu.CV.VideoCapture capture;
        
        private CancellationTokenSource cameraCaptureCancellationTokenSource;
        public OpenCvSharp.Mat currentImage;
        protected int _numCameras = -1;
        Emgu.CV.Mat _frame;
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int description, int reservedValue);
        private static bool IsInternetAvailable()
        {
            int description;
            return InternetGetConnectedState(out description, 0);
        }
        private bool cameraRunning = false;
        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;
        private const uint SC_CLOSE = 0xF060;
        private int _count = 0;
        //private readonly ToastViewModel notifier;
        string ApiKey;
        const string userRoot = "HKEY_CURRENT_USER";
        const string subkey = "wAnywhereLogin";
        const string keyName = userRoot + "\\" + subkey;
        int validClosing = 0;

        public MainWindow()
        {
            InitializeComponent();
            // StartCameraCapture();
            cameraCaptureCancellationTokenSource = new CancellationTokenSource();
            
           
        }

        int selsctind ;
        private async Task StartCameraCapture()
        {
          
            try
            {
                //CameraTimer_Elapsed();
                //var isConnected = DirectShowLib.DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice).Any(c => c.Name == allCameras[0].Name);
                cameraCaptureCancellationTokenSource = new CancellationTokenSource();
                _frame = new Emgu.CV.Mat();
                var result = await Task.Run(() => CaptureCamera(cameraCaptureCancellationTokenSource.Token), cameraCaptureCancellationTokenSource.Token);
                if (i == 0)
                {
                    if (result == 1)
                    {
                        this.Topmost = true;
                        this.Focus();

                        i = 1;
                        var dialogBoxResult = System.Windows.MessageBox.Show("wAnywhere unable to access webcam. Kindly close all apps currently using your webcam.", "Unable to access WebCam", MessageBoxButton.OK, MessageBoxImage.Error); ;
                        if (dialogBoxResult == MessageBoxResult.OK)
                        {
                            result = 0;
                            this.Topmost = false;
                            //result = await Task.Run(() => CaptureCamera(cameraCaptureCancellationTokenSource.Token), cameraCaptureCancellationTokenSource.Token);
                        }
                        else if (dialogBoxResult == MessageBoxResult.Cancel)
                        {
                            result = 0;
                            this.Topmost = false;

                        }
                    }
                }


            }
            catch (Exception ex)
            {
               MessageBox.Show("Error while Starting Camera Capture. Exception :- " + ex.Message.ToString());
            }
        }
        private async Task<int> CaptureCamera(CancellationToken token)
        {

            try
            { 
                Console.WriteLine("Start Camera Initiated");
                
                capture = new Emgu.CV.VideoCapture(selsctind);
                using (var vc = OpenCvSharp.VideoCapture.FromCamera(selsctind))
                {
                    if (vc.IsOpened())
                    {
                        vc.Dispose();
                        capture.ImageGrabbed += emguCVImageCaptured;
                        cameraRunning = true;
                        capture.Start();
                        return 0;
                    }
                    else
                    {
                        i = 0;
                        cameraTimer = new System.Timers.Timer();
                        cameraTimer.Elapsed += CameraTimer_Elapsed; 
                        cameraTimer.Interval = 1000;
                        cameraTimer.Enabled = true;
                        return 1;
                    }
                }
                

            }
            catch (NullReferenceException ex)
            {
                MessageBox.Show("Null Reference CaptureCamera Exception :- " + ex.Message.ToString(), ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Camera Capture Exception :- {ex.Message.ToString()}", ex.Message);
                return 1;
            }
        }
        private void CameraTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (allCameras.Any())
                {
                    bool isConnected = DirectShowLib.DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice).Any(c => c.Name == allCameras[0].Name);
                    if (isConnected)
                    {
                        using (var vc = OpenCvSharp.VideoCapture.FromCamera(0))
                        {
                            if (vc.IsOpened())
                            {
                                vc.Dispose();
                                cameraTimer.Stop();
                                capture.Stop();
                                capture = new Emgu.CV.VideoCapture();
                                capture.ImageGrabbed += emguCVImageCaptured;
                                cameraRunning = true;
                                capture.Start();

                            }

                        }

                    }
                    else
                    {
                        if (i == 0)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                this.Topmost = true;
                                this.Focus();

                                i = 1;
                                var dialogBoxResult = System.Windows.MessageBox.Show("wAnywhere unable to access webcam. Kindly close all apps currently using your webcam.", "Unable to access WebCam", MessageBoxButton.OK, MessageBoxImage.Error); ;
                                if (dialogBoxResult == MessageBoxResult.OK)
                                {
                                    this.Topmost = false;
                                    //result = await Task.Run(() => CaptureCamera(cameraCaptureCancellationTokenSource.Token), cameraCaptureCancellationTokenSource.Token);
                                }
                                else if (dialogBoxResult == MessageBoxResult.Cancel)
                                {
                                    this.Topmost = false;

                                }
                            });

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception while checking for camera connection");
            }
        }

        private void emguCVImageCaptured(object sender, EventArgs arg)
        {
            try
            {
                Console.WriteLine("Inside emgu CV Image captured");
                capture.FlipHorizontal = true;
                var flipImage = Settings.Default.FlipImageFlag;
                if (flipImage)
                {
                    capture.FlipVertical = true;
                }
                else
                {
                    capture.FlipVertical = false;
                }
                capture.Retrieve(_frame, 0);
                var bitmap = _frame.ToBitmap();

                this.Dispatcher.Invoke(() =>
                {
                    CameraSource.Source = ConvertToBitmapSource(bitmap);
                });
                currentImage = OpenCvSharp.Extensions.BitmapConverter.ToMat(bitmap);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
               MessageBox.Show("Emgu CV Capture Exception ", ex.Message);
            }


        }
        public static BitmapSource ConvertToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }
        public static bool IsBlank(Bitmap bitmap)
        {
            double stdDev = GetStdDev(bitmap);
            return stdDev < 100000;
        }

        /// <summary>
        /// Get the standard deviation of pixel values.
        /// </summary>
        /// <param name="imageFileName">Name of the image file.</param>
        /// <returns>Standard deviation.</returns>
        public static double GetStdDev(Bitmap bitmap)
        {
            double total = 0, totalVariance = 0;
            int count = 0;
            double stdDev = 0;

            // First get all the bytes
            using (Bitmap b = bitmap)
            {
                BitmapData bmData = b.LockBits(new System.Drawing.Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadOnly, b.PixelFormat);
                int stride = bmData.Stride;
                IntPtr Scan0 = bmData.Scan0;
                unsafe
                {
                    byte* p = (byte*)(void*)Scan0;
                    int nOffset = stride - b.Width * 3;
                    for (int y = 0; y < b.Height; ++y)
                    {
                        for (int x = 0; x < b.Width; ++x)
                        {
                            count++;

                            byte blue = p[0];
                            byte green = p[1];
                            byte red = p[2];

                            int pixelValue = System.Drawing.Color.FromArgb(0, red, green, blue).ToArgb();
                            total += pixelValue;
                            double avg = total / count;
                            totalVariance += Math.Pow(pixelValue - avg, 2);
                            stdDev = Math.Sqrt(totalVariance / count);

                            p += 3;
                        }
                        p += nOffset;
                    }
                }

                b.UnlockBits(bmData);
            }

            return stdDev;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            CameraCapture();


        }

        private void CaptureImageButton_Click(object sender, RoutedEventArgs e)
        {
            var capturedImage = _frame.ToBitmap();
            CapturedImage.Source = ConvertToBitmapSource(capturedImage);
        }

        

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            //CameraCapture();
            //DisplayTypeAndAddress();
            //ShowInterfaceSpeedAndQueue();
            //htmltopdf();
            // StartCameraCapture();


            //// We use these three SQLite objects:
            //SQLiteConnection sqlite_conn;
            //SQLiteCommand sqlite_cmd;
            //SQLiteDataReader sqlite_datareader;

            //// create a new database connection:
            //sqlite_conn = new SQLiteConnection("Data Source=database.db;Version=3;New=True;Compress=True;");

            //// open the connection:
            //sqlite_conn.Open();

            //// create a new SQL command:
            //sqlite_cmd = sqlite_conn.CreateCommand();

            //// Let the SQLiteCommand object know our SQL-Query:
            //sqlite_cmd.CommandText = "CREATE TABLE test (id integer primary key, text varchar(100));";

            //// Now lets execute the SQL ;D
            //sqlite_cmd.ExecuteNonQuery();

            //// Lets insert something into our new table:
            //sqlite_cmd.CommandText = "INSERT INTO test (id, text) VALUES (1, 'Test Text 1');";

            //// And execute this again ;D
            //sqlite_cmd.ExecuteNonQuery();

            //// ...and inserting another line:
            //sqlite_cmd.CommandText = "INSERT INTO test (id, text) VALUES (2, 'Test Text 2');";

            //// And execute this again ;D
            //sqlite_cmd.ExecuteNonQuery();

            //// But how do we read something out of our table ?
            //// First lets build a SQL-Query again:
            //sqlite_cmd.CommandText = "SELECT * FROM test";

            //// Now the SQLiteCommand object can give us a DataReader-Object:
            //sqlite_datareader = sqlite_cmd.ExecuteReader();

            //// The SQLiteDataReader allows us to run through the result lines:
            //while (sqlite_datareader.Read()) // Read() returns true if there is still a result line to read
            //{
            //    // Print out the content of the text field:
            //    //System.Console.WriteLine(sqlite_datareader["text"]);
            //    string msg = sqlite_datareader.GetString(1);
            //    MessageBox.Show(msg);
            //}

            //// We are ready, now lets cleanup and close our connection:
            //sqlite_conn.Close();

            //Below the code copy any folder and paste 
            //string appDomainPath = AppDomain.CurrentDomain.BaseDirectory;
            //string path = System.IO.Path.Combine(appDomainPath, "dll","x64");
            //string source = path;
            //string jtr = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //string destination = System.IO.Path.Combine(jtr,"LogsFile/");
            //if (!Directory.Exists(destination))
            //{
            //    Directory.CreateDirectory(destination);
            //}
            //string[] files = Directory.GetFiles(source);
            //// File.Copy(source, destination , true);
            //foreach (string fil in files)
            //{
            //    string filename = System.IO.Path.GetFileName(fil);
            //    File.Copy(fil, destination + filename, true);
            //}


          // string appDomainPath = AppDomain.CurrentDomain.BaseDirectory;
            //string path = System.IO.Path.Combine(appDomainPath);
            //string psths= Directory.get
           // Console.WriteLine(path);
        }

        static List<User> users = new List<User>();

        class User
        {
            public string Testing_Name{ get;  set; }
            public string Report { get;  set; }
        }
        string azaz1 = "ki haal hai bhai";
        public void file()
        {
           // System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            var rnd = new Random();
          
           for (int i=0; i<10;i++)
           
                users.Add(new User()
                {
                    Testing_Name = azaz + rnd.Next(5, 66).ToString(),
                    //Testing_Name = azaz1 + rnd.Next(6, 67).ToString(),
                    Report = "Report" + rnd.Next(5, 66).ToString() + i.ToString()
                });
           
            GeneratePdf();
        }
        string azaz = "Hello azaz bhai";
       private void htmltopdf()
        {
            using (PdfDocument doc = new PdfDocument())
            {
                doc.Info.Title = "StackOverflow Demo PDF";

                //add a page
                PdfPage page = doc.AddPage();
                page.Size = PageSize.A4;

                //fonts and styles
                XFont font = new XFont("Arial", 10, XFontStyle.Regular);
                XSolidBrush brush = new XSolidBrush(XColor.FromArgb(0, 0, 0));

                using (XGraphics gfx = XGraphics.FromPdfPage(page))
                {
                    //write a normal string
                    gfx.DrawString("A normal string written to the PDF.", font, brush, new XRect(15, 15, page.Width, page.Height), XStringFormats.TopLeft);

                    //write the html string to the pdf
                    using (var container = new HtmlContainer())
                    {
                        var pageSize = new XSize(page.Width, page.Height);

                        container.Location = new XPoint(15, 45);
                        container.MaxSize = pageSize;
                        container.PageSize = pageSize;
                        container.SetHtml("This is a <b>HTML</b> string <u>written</u> to the <font color=\"red\">PDF</font>.<br><br><a href=\"http://www.google.nl\">www.google.nl</a>");

                        using (var measure = XGraphics.CreateMeasureContext(pageSize, XGraphicsUnit.Point, XPageDirection.Downwards))
                        {
                            container.PerformLayout(measure);
                        }

                        gfx.IntersectClip(new XRect(0, 0, page.Width, page.Height));

                        container.PerformPaint(gfx);
                    }
                }

                //write the pdf to a byte array to serve as download, attach to an email etc.
                byte[] bin;
                using (MemoryStream stream = new MemoryStream())
                {
                    doc.Save(stream, false);
                    bin = stream.ToArray();
                }
            }
        }
        public void GeneratePdf()
        {
            PdfDocument pdf = new PdfDocument();
            
            pdf.Info.Title = "My First PDF";
            PdfPage pdfPage = pdf.AddPage();
            pdfPage.Size = PageSize.B4;
            // XSize graph = PageSizeConverter.ToSize(PdfSharp.PageSize.A5);
            XGraphics graph = XGraphics.FromPdfPage(pdfPage);
            //Generate Header
            graph.DrawString("Service Health Check Report", new XFont("Arial", 30, XFontStyle.Bold), XBrushes.Black, new XPoint(100, 170));
            graph.DrawLine(new XPen(XColor.FromArgb(50, 30, 200)), new XPoint(99, 180), new XPoint(510, 180));
            XImage image = XImage.FromFile("E:/NewOfficeData/Data/website-logo-white.png");

            graph.DrawImage(image, 100, 0, 380, 60);

            //Generate Table Header
            graph.DrawString("Testing Name", new XFont("Arial",15,XFontStyle.Bold),XBrushes.Black, new XPoint(50,280));
            graph.DrawString("Testing Status", new XFont("Arial", 17, XFontStyle.Bold), XBrushes.Black, new XPoint(250, 280));
            graph.DrawLine(new XPen(XColor.FromArgb(50, 30, 200)), new XPoint(50, 290), new XPoint(550, 290));

            int currentpositionvalue = 303;
            int currentpositionline = 310;
            //if (users.Count<=20)
            //{
            //    for (int i=0; i<users.Count;i++)
            //    {
            //        graph.DrawString(users[i].Testing_Name, new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(50, currentpositionvalue));
            //        graph.DrawString(users[i].Report, new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(250, currentpositionline));
            //        currentpositionvalue += 20;
            //        currentpositionline += 20;
            //    }
            //}
            //else
            //{
                //for (int i = 0; i < 15; i++)
                //{
                    graph.DrawString(users[i].Testing_Name, new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(50, currentpositionvalue));
                    graph.DrawString(users[i].Report, new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(250, currentpositionvalue));
                    graph.DrawLine(new XPen(XColor.FromArgb(50, 30, 200)), new XPoint(50, 290), new XPoint(550, 290));
                    currentpositionvalue += 20;
                    currentpositionline += 20;
                    users.Remove(users[i]);
            //}
            //}


            //XFont font = new XFont("Verdana", 12, XFontStyle.Bold);
            //graph.DrawString("Internet Connectivity", font, XBrushes.Black, new XRect(0, 0, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopLeft);
            //graph.DrawString("Camera connectivity", font, XBrushes.Black, new XRect(0, 0, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopCenter);
            //graph.DrawString("FaceApi Connectivity", font, XBrushes.Black, new XRect(0, 0, pdfPage.Width.Point, pdfPage.Height.Point), XStringFormats.TopRight);
            PdfPage pdfPages = pdf.AddPage();
            XGraphics graphs = XGraphics.FromPdfPage(pdfPages);
            graphs.DrawString("Service Health Check Log File", new XFont("Arial", 30, XFontStyle.Bold), XBrushes.Black, new XPoint(90, 140));
            graphs.DrawLine(new XPen(XColor.FromArgb(50, 30, 200)), new XPoint(89, 150), new XPoint(520, 150));

            //camera log
            graph.DrawString("Testing Name", new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(50, 200));
            graph.DrawString("Testing Name", new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(50, 230));
            graph.DrawString("Testing Name", new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(50, 260));
            graph.DrawString("Testing Name", new XFont("Arial", 15, XFontStyle.Bold), XBrushes.Black, new XPoint(50, 290));
            string pdfFilename = "firstpage.pdf";
            pdf.Save(pdfFilename);
            Process.Start(pdfFilename);

        }

        public static void ShowInterfaceSpeedAndQueue()
        {
            string val = "";
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                IPv4InterfaceStatistics stats = adapter.GetIPv4Statistics();
               // Console.WriteLine(adapter.Description);
              // Console.WriteLine("     Speed .................................: {0}",  adapter.Speed/ 1000000.0);
                //Console.WriteLine("     Output queue length....................: {0}", stats.OutputQueueLength);
               // Console.WriteLine(adapter.Name);
               // Console.WriteLine(adapter.Id);
                //Console.WriteLine(adapter.OperationalStatus);
                Console.WriteLine("Recieve :" + Math.Round(stats.BytesReceived / 1000000.0 ) + "kbps");
                Console.WriteLine("Send"+stats.BytesSent / 1000000);
                //Console.WriteLine(stats.UnicastPacketsReceived);
                Console.WriteLine(stats.UnicastPacketsSent);

            }
        }

        public static void DisplayTypeAndAddress()
        {
            IPGlobalProperties computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine("Interface information for {0}.{1}     ",
                    computerProperties.HostName, computerProperties.DomainName);
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                Console.WriteLine(adapter.Description);
                Console.WriteLine(String.Empty.PadLeft(adapter.Description.Length, '='));
                Console.WriteLine("  Interface type .......................... : {0}", adapter.NetworkInterfaceType);
                Console.WriteLine("  Physical Address ........................ : {0}",
                           adapter.GetPhysicalAddress(),adapter.Name);
                Console.WriteLine("  Is receive only.......................... : {0}", adapter.IsReceiveOnly);
                Console.WriteLine("  Multicast................................ : {0}", adapter.SupportsMulticast);
                Console.WriteLine();
            }
        }

        #region Camera Capture Variables
        private Capture _capture = null; //Camera
        private bool _captureInProgress = false; //Variable to track camera state
        int CameraDevice = 0; //Variable to track camera device selected
        Video_Device[] WebCams; //List containing all the camera available
        #endregion
        #region Camera Settings
        int Brightness_Store = 0;
        int Contrast_Store = 0;
        int Sharpness_Store = 0;
        #endregion

        public void  CameraCapture()
        {
           // InitializeComponent();
            //Slider_Enable(false); //Disable sliders untill capturing

            //-> Find systems cameras with DirectShow.Net dll
            //thanks to carles lloret
            DsDevice[] _SystemCamereas = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
            WebCams = new Video_Device[_SystemCamereas.Length];
            for (int i = 0; i < _SystemCamereas.Length; i++)
            {
                WebCams[i] = new Video_Device(i, _SystemCamereas[i].Name); //fill web cam array
                Console.WriteLine(WebCams[i].ToString());
                lst1.Items.Add(_SystemCamereas[i].Name);
                //selsctind = lst1.Sel;
            }

            Console.WriteLine(selsctind);
            StartCameraCapture();
           
        }

        private void lst1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //If there is already a capture, dispose it
            //if (capture != null)
            //{
            //    capture.Dispose();
            //}
            ////Get the selected camera
            //int selectedDevice = lst1.SelectedIndex;
            //try
            //{
            //    //Create new capture with the selected camera
            //    capture = new Capture(selectedDevice);
            //}
            //catch (Exception excpt)
            //{
            //    MessageBox.Show(excpt.Message);
            //}
            selsctind= lst1.SelectedIndex;
           
        }
    }
}
