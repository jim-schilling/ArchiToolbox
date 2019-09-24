// LogHelper.cs
// 
// Created: 2019-07-27T5:36 PM
// Updated: 2019-08-03T10:08 PM
// 
// Copyright 2019 (c) Jim Schilling
// 
// MIT License

#region

using System;
using System.IO;

#endregion

namespace ArchiToolbox.Logging
{
    public static class LogHelper
    {
        internal static string CreateLogFile()
        {
            var logFile = $"{Path.GetTempPath()}{Guid.NewGuid():D}.log";

            using (File.CreateText(logFile))
            {
            }

            return logFile;
        }
    }
}