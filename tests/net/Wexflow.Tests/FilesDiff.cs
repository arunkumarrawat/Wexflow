﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Wexflow.Tests
{
    [TestClass]
    public class FilesDiff
    {
        private static readonly string TempFolder = @"C:\Wexflow\Temp\71";

        private static readonly string ExpectedResult =
              "  We the people\r\n"
            + "- of the united states of america\r\n"
            + "+ in order to form a more perfect union\r\n"
            + "  establish justice\r\n"
            + "  ensure domestic tranquility\r\n"
            + "- provide for the common defence\r\n"
            + "+ promote the general welfare and\r\n"
            + "  secure the blessing of liberty\r\n"
            + "  to ourselves and our posterity\r\n"
            + "+ do ordain and establish this constitution\r\n"
            + "+ for the United States of America\r\n"
            + "+ \r\n";

        [TestInitialize]
        public void TestInitialize()
        {
            if (!Directory.Exists(TempFolder))
            {
                _ = Directory.CreateDirectory(TempFolder);
            }

            Helper.DeleteFilesAndFolders(TempFolder);
        }

        [TestCleanup]
        public void TestCleanup()
        {
        }

        [TestMethod]
        public async System.Threading.Tasks.Task FilesDiffTest()
        {
            _ = await Helper.StartWorkflow(71);

            // Check the workflow result
            var files = Directory.GetFiles(TempFolder, "FilesDiff*.diff", SearchOption.AllDirectories);
            Assert.AreEqual(files.Length, 1);

            var content = File.ReadAllText(files[0]);
            Assert.AreEqual(ExpectedResult, content);
        }
    }
}