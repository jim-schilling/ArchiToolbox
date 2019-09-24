// Logger.cs
// 
// Created: 2019-08-06T10:00 PM
// Updated: 2019-08-12T5:28 PM
// 
// Copyright 2019 (c) Jim Schilling
// All Rights Reserved.
// 
// MIT License

#region

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace ArchiToolbox.Logging
{
    public class Logger
    {
        private static readonly ConcurrentDictionary<Guid, ILogPublisher> _logPublishers;

        private static readonly Task _backgroundTask;

        private static readonly CancellationTokenSource _cancellationTokenSource;

        private static readonly ConcurrentQueue<LogEntry> _logEntries;

        private static bool _isShutdown;

        private readonly string _source;

        static Logger()
        {
            _logPublishers = new ConcurrentDictionary<Guid, ILogPublisher>();

            _isShutdown = false;

            _logEntries = new ConcurrentQueue<LogEntry>();

            _cancellationTokenSource = new CancellationTokenSource();

            _backgroundTask = Task.Factory.StartNew(() => { BackgroundTask(_cancellationTokenSource.Token); });
        }

        public Logger(string source)
        {
            _source = source ?? throw new ArgumentNullException($"{nameof(Logger)}.ctor: {nameof(source)} is null");
        }

        private static void BackgroundTask(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (_logPublishers.Count > 0 && _logEntries.TryDequeue(out var entry))
                    {
                        foreach (var publisher in _logPublishers.Values)
                        {
                            publisher.Publish(entry);
                        }
                    }
                    else
                    {
                        Thread.Sleep(125);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        public static Guid AddPublisher(ILogPublisher logPublisher)
        {
            if (logPublisher == null)
            {
                throw new ArgumentNullException(
                    $"{nameof(Logger)}.{nameof(AddPublisher)}: {nameof(logPublisher)} is null");
            }

            var id = logPublisher.Id();

            if (!_logPublishers.ContainsKey(id))
            {
                _logPublishers.TryAdd(id, logPublisher);
            }

            return id;
        }

        public static void Shutdown()
        {
            _isShutdown = true;

            var stop = DateTime.UtcNow.AddMilliseconds(1250);

            while (DateTime.UtcNow < stop)
            {
            }

            var publishers = _logPublishers.Values;

            foreach (var publisher in publishers)
            {
                publisher.Dispose();
            }

            _logPublishers.Clear();

            _cancellationTokenSource.Cancel();

            stop = DateTime.UtcNow.AddSeconds(5);

            while (_backgroundTask.Status == TaskStatus.Running && DateTime.UtcNow < stop)
            {
                Thread.Sleep(75);
            }
        }

        private static void Publish(LogEntry entry)
        {
            if (!_isShutdown)
            {
                _logEntries.Enqueue(entry);
            }
        }

        public void Publish(LogType type, string message)
        {
            Publish(new LogEntry(type, _source, message));
        }

        public void Error(string message)
        {
            Publish(LogType.Error, message);
        }

        public void Error(Exception exception)
        {
            Publish(LogType.Error, exception.Message);
        }

        public void Debug(string message)
        {
            Publish(LogType.Debug, message);
        }

        public void Debug(Exception exception)
        {
            Publish(LogType.Debug, exception.Message);
        }

        public void Info(string message)
        {
            Publish(LogType.Info, message);
        }

        public void Info(Exception exception)
        {
            Publish(LogType.Info, exception.Message);
        }

        public void Warn(string message)
        {
            Publish(LogType.Warn, message);
        }

        public void Warn(Exception exception)
        {
            Publish(LogType.Warn, exception.Message);
        }
    }
}