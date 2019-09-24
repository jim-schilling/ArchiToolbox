// ConsoleLogPublisher.cs
// 
// Created: 2019-07-28T9:08 PM
// Updated: 2019-08-03T10:08 PM
// 
// Copyright 2019 (c) Jim Schilling
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
    public class ConsoleLogPublisher : ILogPublisher
    {
        private readonly Guid _id = Guid.NewGuid();

        private readonly ConcurrentQueue<LogEntry> _logEntries;

        private readonly int SleepInterval = 125;

        private volatile Task _backgroundTask;

        private volatile CancellationTokenSource _cancellationTokenSource;

        private bool _isDisposed;

        public ConsoleLogPublisher()
        {
            _logEntries = new ConcurrentQueue<LogEntry>();

            _cancellationTokenSource = new CancellationTokenSource();

            _backgroundTask = Task.Factory.StartNew(() => { BackgroundTask(_cancellationTokenSource.Token); });
        }

        public Guid Id()
        {
            return _id;
        }

        public void Publish(LogEntry entry)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException($"{nameof(FileLogPublisher)}.{nameof(Publish)}: Object already disposed");
            }

            _logEntries.Enqueue(entry);
        }

        public void Dispose()
        {
            try
            {
                if (!_isDisposed)
                {
                    Shutdown();
                    _isDisposed = true;
                }
            }
            catch
            {
            }
        }

        private void BackgroundTask(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (_logEntries.TryDequeue(out var entry))
                    {
                        Console.WriteLine(entry.ToString());
                    }
                    else
                    {
                        Thread.Sleep(SleepInterval);
                    }
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception)
            {
            }
        }

        private void Shutdown()
        {
            _cancellationTokenSource.Cancel();

            _logEntries.Clear();

            var maxRetries = 20;

            while (_backgroundTask?.Status == TaskStatus.Running && maxRetries-- > 0)
            {
                Thread.Sleep(50);
            }
        }
    }
}