﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace MasterDevs.ChromeDevTools
{
    public class ChromeProcessFactory : IChromeProcessFactory
    {
        public IDirectoryCleaner DirectoryCleaner { get; set; }
        public string ChromePath { get; }

        //public ChromeProcessFactory(IDirectoryCleaner directoryCleaner, string chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe")
        public ChromeProcessFactory(IDirectoryCleaner directoryCleaner, string chromePath = @"chr\chrome.exe")
        {
            DirectoryCleaner = directoryCleaner;
            ChromePath = chromePath;
        }

        public IChromeProcess Create(int port, bool headless)
        {
            string path = Path.GetRandomFileName();
            //string path = "1111rfdw111111111111111.dhd";
            var directoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), path));
            var remoteDebuggingArg = $"--remote-debugging-port={port}";
            var userDirectoryArg = $"--user-data-dir=\"{directoryInfo.FullName}\"";
            string proxyArgs = $"--proxy-server=\"https://213.226.76.117:8000\"";


            //const string headlessArg = "--disable-gpu";
            const string headlessArg = "--headless --disable-gpu";
            var chromeProcessArgs = new List<string>
            {
                remoteDebuggingArg,
                userDirectoryArg,
                "--bwsi",
                "--no-first-run"
            };
            if (headless)
                chromeProcessArgs.Add(headlessArg);
            chromeProcessArgs.Add(proxyArgs);
            var processStartInfo = new ProcessStartInfo(ChromePath, string.Join(" ", chromeProcessArgs));
            var chromeProcess = Process.Start(processStartInfo);

            string remoteDebuggingUrl = "http://localhost:" + port;
            return new LocalChromeProcess(new Uri(remoteDebuggingUrl), () => DirectoryCleaner.Delete(directoryInfo), chromeProcess);
        }
    }
}