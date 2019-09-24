// LogEntry.cs
// 
// Created: 2019-07-27T5:34 PM
// Updated: 2019-08-03T10:08 PM
// 
// Copyright 2019 (c) Jim Schilling
// 
// MIT License

#region

using System;
using System.Threading;

#endregion

namespace ArchiToolbox.Logging
{
    public struct LogEntry
    {
        public DateTime Timestamp;
        public int ThreadId;
        public string Source;
        public string Message;
        public LogType Type;

        public LogEntry(LogType type, string source, string message)
        {
            Timestamp = DateTime.Now;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            Source = source ?? string.Empty;
            Message = message ?? string.Empty;
            Type = type;
        }

        public override string ToString()
        {
            return
                $"[{Timestamp:s} {Enum.GetName(typeof(LogType), Type)} ({ThreadId})] {Source ?? string.Empty} :: {Message ?? string.Empty}";
        }
    }
}