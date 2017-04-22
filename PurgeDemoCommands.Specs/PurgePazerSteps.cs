using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using TechTalk.SpecFlow;

namespace PurgeDemoCommands.Specs
{
    [Binding]
    public class PurgePazerSteps
    {
        public string Arguments { get; set; }
        public int Timeout { get; set; }

        private static string TestDataPath
        {
            get
            {
                return (string)ScenarioContext.Current["TestDataPath"];
            }
            set
            {
                ScenarioContext.Current["TestDataPath"] = value;
            }
        }

        [BeforeScenario()]
        public void BeforeScenario()
        {
            Timeout = 60000;

            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.CurrentDirectory = directoryName;

            string testDataPath = Path.Combine(directoryName, "TestData_"+ ScenarioContext.Current.ScenarioInfo.Title.Replace(" ", "_").Replace("'", ""));
            TestDataPath = testDataPath;
            if (Directory.Exists(testDataPath))
                Directory.Delete(testDataPath, true);
            FileHelper.DirectoryCopy(Path.Combine(directoryName, "TestData_Init"), testDataPath, true);
        }

        [Given(@"the arguments \[(.*)]")]
        public void GivenTheArguments(string arguments)
        {
            arguments = ReplaceTestDataPath(arguments);
            Console.WriteLine("arguments: " + arguments);

            Arguments = arguments;
        }

        [Given(@"the timeout (.*)")]
        public void GivenTheTimeout(int timeoutInMilliSeconds)
        {
            Timeout = timeoutInMilliSeconds;
        }
        
        [When(@"I run PurgeDemoComands")]
        public void WhenIRunPurgeDemoComands()
        {
            Console.WriteLine(Environment.CurrentDirectory);
            Process.Start("PurgeDemoCommands.exe", Arguments).WaitForExit(Timeout);
        }
        
        [Then(@"I expect files")]
        public void ThenIExpectFiles(Table table)
        {
            foreach (string[] row in table.AllRows())
            {
                string path = row[0];
                path = ReplaceTestDataPath(path);

                Microsoft.VisualStudio.TestTools.UnitTesting.Assert.IsTrue(File.Exists(path), "File.Exists({0})", path);
            }
        }

        private static string ReplaceTestDataPath(string arguments)
        {
            arguments = arguments.Replace("TestData", TestDataPath);
            return arguments;
        }
    }

    static class FileHelper
    {
        public static void DirectoryCopy(
            string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }


            // Get the file contents of the directory to copy.
            FileInfo[] files = dir.GetFiles();

            foreach (FileInfo file in files)
            {
                // Create the path to the new copy of the file.
                string temppath = Path.Combine(destDirName, file.Name);

                // Copy the file.
                file.CopyTo(temppath, false);
            }

            // If copySubDirs is true, copy the subdirectories.
            if (copySubDirs)
            {

                foreach (DirectoryInfo subdir in dirs)
                {
                    // Create the subdirectory.
                    string temppath = Path.Combine(destDirName, subdir.Name);

                    // Copy the subdirectories.
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}
