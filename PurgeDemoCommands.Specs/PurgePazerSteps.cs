using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TechTalk.SpecFlow;

namespace PurgeDemoCommands.Specs
{
    [Binding]
    public class PurgePazerSteps
    {
        private string _testDataPath;
        public string Arguments { get; set; }
        public int Timeout { get; set; }

        [BeforeScenario()]
        public void BeforeScenario()
        {
            Timeout = 60000;

            string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Environment.CurrentDirectory = directoryName;

            
            _testDataPath = Path.Combine(directoryName, "TestData_"+ TestContext.CurrentContext.Test.Name);
            if (Directory.Exists(_testDataPath))
                Directory.Delete(_testDataPath, true);
            FileHelper.DirectoryCopy(Path.Combine(directoryName, "TestData_Init"), _testDataPath, true);
        }

        [Given(@"the arguments \[(.*)]")]
        public void GivenTheArguments(string arguments)
        {
            arguments = arguments.Replace("TestData", _testDataPath);
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
                path = path.Replace("TestData", _testDataPath);
                Assert.That(File.Exists(path), "File.Exists({0})", path);
            }
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
