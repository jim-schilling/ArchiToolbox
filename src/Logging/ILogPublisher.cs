// ILogPublisher.cs
// 
// Created: 2019-07-27T5:33 PM
// Updated: 2019-08-03T10:08 PM
// 
// Copyright 2019 (c) Jim Schilling
// 
// MIT License

#region

using System;

#endregion

namespace ArchiToolbox.Logging
{
    public interface ILogPublisher : IDisposable
    {
        void Publish(LogEntry entry);
        Guid Id();
    }
}