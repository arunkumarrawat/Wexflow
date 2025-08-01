﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace Wexflow.NetCore.Tests
{
    [TestClass]
    public class TextsDecryptor
    {
        private static readonly string TextsDecryptorSrcFolder = @"C:\WexflowTesting\TextsDecryptor_src\";
        private static readonly string TextsDecryptorDestFolder = @"C:\WexflowTesting\TextsDecryptor_dest\";
        private static readonly string TextsEncryptorFolder = @"C:\WexflowTesting\TextsEncryptor\";

        [TestInitialize]
        public void TestInitialize()
        {
            Helper.DeleteFiles(TextsDecryptorSrcFolder);
            Helper.DeleteFiles(TextsDecryptorDestFolder);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Helper.DeleteFiles(TextsDecryptorSrcFolder);
            Helper.DeleteFiles(TextsDecryptorDestFolder);
            Helper.DeleteFiles(TextsEncryptorFolder);
        }

        [TestMethod]
        public async System.Threading.Tasks.Task TextsDecryptorTest()
        {
            var files = GetFiles(TextsDecryptorDestFolder);
            Assert.AreEqual(0, files.Length);
            _ = await Helper.StartWorkflow(83);
            _ = await Helper.StartWorkflow(84);
            files = GetFiles(TextsDecryptorDestFolder);
            Assert.AreEqual(2, files.Length);
        }

        private static string[] GetFiles(string dir)
        {
            return Directory.GetFiles(dir);
        }
    }
}
