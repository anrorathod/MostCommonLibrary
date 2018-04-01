using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace QDevSmartCare.Web.Controllers
{
    public class ProjectCloneController : Controller
    {
        #region Members
        #endregion Members

        #region Constructor
        // GET: ProjectCreate
        public ActionResult Index()
        {
            return View();
        }

        #endregion Constructor

        #region Actions

        [HttpPost]
        public ActionResult Index(ProjectCreationViewModel projectCreationViewModel)
        {
            try
            {
                var parentPath = Path.GetDirectoryName(Path.GetDirectoryName(Server.MapPath("~/"))) + "\\";

                var destinationPath = Path.GetDirectoryName(Path.GetDirectoryName(Server.MapPath("~/"))) + "\\ProjectsCreated\\" + projectCreationViewModel.ProjectName;
                //if (ConfigurationManager.AppSettings["QDevSmartCareLive"] == "true")
                //{
                //var destinationPath = Path.GetDirectoryName(Server.MapPath("~/")) + "\\ProjectsCreated\\" + projectCreationViewModel.ProjectName ;
                //}

                List<string> AllDirectories = new List<string>
                {
                    "Business",
                   // "Common",
                    "Database",
                    "ViewModel",
                    projectCreationViewModel.ProjectType.ToString(),
                };

                foreach (var item in AllDirectories)
                {
                    projectCreationViewModel.Source = parentPath + "QDevSmartCare." + item;
                    var destination = destinationPath + "\\" + projectCreationViewModel.ProjectName + "." + item;

                    Copy(projectCreationViewModel.Source, destination, projectCreationViewModel.ProjectName, projectCreationViewModel.DatabaseConnection);
                }

                var file = new FileInfo(parentPath + "\\QDevSmartCare.sln");
                var combinedPath = Path.Combine(destinationPath, Path.GetFileName(file.Name.Replace("QDevSmartCare", projectCreationViewModel.ProjectName).Replace("Web", "")));

                FileInfo fileDelete = new FileInfo(combinedPath);
                if (fileDelete.Exists)
                {
                    fileDelete.Delete();
                }

                System.IO.File.Copy(file.FullName, combinedPath);

                var fileInfo = new FileInfo(combinedPath);
                changeText("QDevSmartCare", projectCreationViewModel.ProjectName, fileInfo, projectCreationViewModel.DatabaseConnection);
                //System.IO.File.Copy(@"E:\QDevSmartCareLocal\QDevSmartCare.dll", @"E:\Demo\");

                //            foreach (string dirPath in Directory.GetDirectories(@"E:\QDevSmartCareLocal\trunk\QDevSmartCare\bin", "*",
                //SearchOption.AllDirectories))
                //                Directory.CreateDirectory(dirPath.Replace(@"E:\QDevSmartCareLocal\trunk\QDevSmartCare\bin", @"E:\Demo\testbins"));

                //            foreach (string newPath in Directory.GetFiles(@"E:\QDevSmartCareLocal\trunk\QDevSmartCare\bin", "*.*",
                //SearchOption.AllDirectories))
                //                System.IO.File.Copy(newPath, newPath.Replace(@"E:\QDevSmartCareLocal\trunk\QDevSmartCare\bin", @"E:\Demo\testbins"), true);

                var zipPath = destinationPath + ".zip";
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddDirectory(destinationPath);
                    zip.Save(zipPath);
                }

                ViewBag.message = "Project created";
                ModelState.Clear();

                DirectoryInfo fileDeleteFolder = new DirectoryInfo(destinationPath);
                if (fileDeleteFolder.Exists)
                {
                    fileDeleteFolder.Delete(true);
                }

                //LogIntoDatabase(projectCreationViewModel.ProjectName);

                return File(zipPath, "application/zip", Server.UrlEncode(projectCreationViewModel.ProjectName + ".zip"));
                //return View();
            }
            catch (Exception ex)
            {
                return View(projectCreationViewModel);
            }
        }

        #endregion Actions

        #region Methods

        public void Copy(string sourceDir, string targetDir, string projectName, string connectionString)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                List<string> skippingFiles = new List<string>
                {
                    "ProjectCreateController",
                };
                var fileSource = Path.GetFileName(file);
                //if (fileSource.Contains("QDevSmartCare.Commons.dll"))
                //{
                //}
                //else
                //{
                fileSource = fileSource.Replace("QDevSmartCare", projectName);
                //}
                var combinedPath = Path.Combine(targetDir, fileSource);

                if (skippingFiles.Any(a => combinedPath.Contains(a)))
                {
                    //do not copy system files
                }
                else
                {
                    FileInfo fileDelete = new FileInfo(combinedPath);
                    if (fileDelete.Exists)
                    {
                        fileDelete.Delete();
                    }
					
                    System.IO.File.Copy(file, combinedPath);

                    var fileInfo = new FileInfo(combinedPath);
                    //fileInfo.Replace("QDevSmartCare", projectName);

                    changeText("QDevSmartCare", projectName, fileInfo, connectionString);
                }
            }

            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var targetDirectory = Path.Combine(targetDir, Path.GetFileName(directory));

                List<string> skippingDirectories = new List<string>
                {
                    "ProjectCreate",
                  //  "ProjectsCreated",
                    
                };
                if (skippingDirectories.Any(a => targetDirectory.Contains(a)))
                {
                    //skip directories
                }
                else
                {
                    Copy(directory, Path.Combine(targetDir, Path.GetFileName(directory.Replace("QDevSmartCare", projectName))), projectName, connectionString);
                }
            }
        }

        private void LogIntoDatabase(string projectName)
        {
            CreateTableIfNotExists();

            XDocument xmldoc = new XDocument();
            xmldoc = XDocument.Load(Server.MapPath("~/Configuration/Configuration.xml"));
            // create connection and command
            using (SqlConnection cn = new SqlConnection(xmldoc.Descendants("ErrorConnection").FirstOrDefault()?.Value))
            using (SqlCommand cmd = new SqlCommand("ProjectLogInsert", cn))
            {
                var ipAddress = GetIpAddress();
                cmd.CommandType = CommandType.StoredProcedure;
                // define parameters and their values
                cmd.Parameters.AddWithValue("@ProjectName", projectName);
                cmd.Parameters.AddWithValue("@OSName", GetOsName());
                cmd.Parameters.AddWithValue("@BrowserInfo", GetBrowserInformation());
                cmd.Parameters.AddWithValue("@IpAddress", ipAddress);
                cmd.Parameters.AddWithValue("@PCName", Environment.MachineName);

                // open connection, execute INSERT, close connection
                cn.Open();
                cmd.ExecuteNonQuery();
                cn.Close();
            }
        }

        private string GetBrowserInformation()
        {
            var browser = HttpContext.Request.Browser;
            return "Browser Capabilities\n"
                + "Type = " + browser.Type + "\n"
                + "Name = " + browser.Browser + "\n"
                + "Version = " + browser.Version + "\n"
                + "Major Version = " + browser.MajorVersion + "\n"
                + "Minor Version = " + browser.MinorVersion + "\n"
                + "Platform = " + browser.Platform + "\n"
                + "Is Beta = " + browser.Beta + "\n"
                + "Is Crawler = " + browser.Crawler + "\n"
                + "Is AOL = " + browser.AOL + "\n"
                + "Is Win16 = " + browser.Win16 + "\n"
                + "Is Win32 = " + browser.Win32 + "\n"
                + "Supports Frames = " + browser.Frames + "\n"
                + "Supports Tables = " + browser.Tables + "\n"
                + "Supports Cookies = " + browser.Cookies + "\n"
                + "Supports VBScript = " + browser.VBScript + "\n"
                + "Supports JavaScript = " +
                    browser.EcmaScriptVersion.ToString() + "\n"
                + "Supports Java Applets = " + browser.JavaApplets + "\n"
                + "Supports ActiveX Controls = " + browser.ActiveXControls
                      + "\n";
        }

        private string GetIpAddress()
        {
            try
            {
                string hostName = Dns.GetHostName(); // Retrive the Name of HOST
                Console.WriteLine(hostName);
                // Get the IP
                return Dns.GetHostEntry(hostName).AddressList[2].ToString();
            }
            catch
            {
                return "";
            }
        }

        private string GetOsName()
        {
            return Environment.OSVersion.ToString();
        }
        private void CreateTableIfNotExists()
        {
            ExecuteQuery("IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProjectCreateLog' AND xtype='U')CREATE TABLE [dbo].[ProjectCreateLog]( [Id] [int] IDENTITY(1,1) NOT NULL, [ProjectName] [nvarchar](150) NULL, [OSName] [nvarchar](max) NULL, [BrowserInfo] [nvarchar](max) NULL, [CreateDate] [datetime] NULL, [IpAddress] [nvarchar](max) NULL,[PCName] [nvarchar](max) NULL, CONSTRAINT [PK_ProjectCreateLog] PRIMARY KEY CLUSTERED ( [Id] ASC )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY] ) ON [PRIMARY]IF NOT EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND OBJECT_ID = OBJECT_ID('dbo.ProjectLogInsert')) BEGIN exec('create PROCEDURE [dbo].[ProjectLogInsert] @ProjectName nvarchar(max), @OSName nvarchar(max), @BrowserInfo nvarchar(max), @IpAddress nvarchar(max), @PCName nvarchar(max) AS BEGIN INSERT INTO [dbo].[ProjectCreateLog] ([ProjectName] ,[OSName] ,[BrowserInfo] ,[CreateDate] ,[IpAddress],[PCName]) VALUES (@ProjectName ,@OSName ,@BrowserInfo ,getdate() ,@IpAddress,@PCName)end ')END");
        }

        private void ExecuteQuery(string sql)
        {
            XDocument xmldoc = new XDocument();
            xmldoc = XDocument.Load(Server.MapPath("~/Configuration/Configuration.xml"));

            using (SqlConnection theConnection = new SqlConnection(xmldoc.Descendants("ErrorConnection").FirstOrDefault()?.Value))
            using (SqlCommand theCommand = new SqlCommand(sql, theConnection))
            {
                theConnection.Open();
                theCommand.CommandType = CommandType.Text;
                theCommand.ExecuteNonQuery();
            }
        }
        private void changeText(string searchString, string newString, FileInfo fileName, string connectionString)
        {
            //if (fileName.Name.Contains("QDevSmartCare.Commons.dll"))
            //{
            //}
            //else
            //{
            string fileToBeEdited = fileName.FullName; // <== This line was missing
            System.IO.File.SetAttributes(fileName.FullName, System.IO.File.GetAttributes(fileToBeEdited) &
                                                ~FileAttributes.ReadOnly);
            string strFile = System.IO.File.ReadAllText(fileToBeEdited);

            if (strFile.Contains(searchString))
            { // <== replaced newString by searchString

                if (fileName.Name.Contains("Web.config") || fileName.Name.Contains("Configuration.xml"))
                {
                    strFile = strFile.Replace("Data Source=10.2.7.35;Initial Catalog=QDevSmartCare;user id=sa;password=ind@nic701;", "" + connectionString + "");
                }

                strFile = strFile.Replace(searchString, newString);
                strFile = strFile.Replace("<Compile Include=\"Areas\\SuperAdmin\\Controllers\\ProjectCreateController.cs\" />", "");
                //strFile = strFile.Replace(newString + ".dll", "QDevSmartCare.Commons.dll");
                //strFile = strFile.Replace(newString + ", Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL", "QDevSmartCare.Commons, Version=1.0.0.0, Culture=neutral, processorArchitecture=MSIL");

                // strFile = strFile.Replace(newString + ".Commons, Version=1.0.0.0", "QDevSmartCare.Commons, Version=1.0.0.0");
                //strFile = strFile.Replace(@"..\"+ newString + @".Commons\bin\Debug\htc.Commons.dll", @"..\QDevSmartCare.Commons\bin\Debug\QDevSmartCare.Commons.dll");
                //strFile = strFile.Replace(newString + ".Commons.Error", "QDevSmartCare.Commons.Error");
                //strFile = strFile.Replace(newString + ".Commons.EmailSend", "QDevSmartCare.Commons.EmailSend");
                //strFile = strFile.Replace(newString + ".Commons.Cryptography", "QDevSmartCare.Commons.Cryptography");
                //strFile = strFile.Replace(newString+".Commons", "QDevSmartCare.Commons");

                System.IO.File.WriteAllText(fileToBeEdited, strFile);
            }
        }
        //}
        #endregion Methods
    }

    public class ProjectCreationViewModel
    {
        public string Source { get; set; }

        [Required]
        public string Destination { get; set; }
        [Required]
        [MaxLength(150)]
        [Display(Name = "Project Name")]
        public string ProjectName { get; set; }

        [Required]
        [Display(Name = "Database Connection")]
        public string DatabaseConnection { get; set; } = "Data Source=serverName;Initial Catalog=databaseName;user id=userId;password=Password;";

        [Display(Name = "Project Type")]
        public ProjectType ProjectType { get; set; }
    }

    public enum ProjectType
    {
        WebApi = 2,
        Web = 1
    }
}
