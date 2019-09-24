// FileLogPublisher.cs
// 
// Created: 2019-07-27T5:43 PM
// Updated: 2019-08-03T10:08 PM
// 
// Copyright 2019 (c) Jim Schilling
// 
// MIT License

#region

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#endregion

namespace ArchiToolbox.Logging
{
    public class FileLogPublisher : ILogPublisher
    {
        private readonly Guid _id = Guid.NewGuid();

        private readonly ConcurrentQueue<LogEntry> _logEntries;
        
        private readonly int FlushInterval = 60;

        private readonly int SleepInterval = 125;

        private volatile Task _backgroundTask;

        private volatile CancellationTokenSource _cancellationTokenSource;

        private bool _isDisposed;

        public FileLogPublisher(string fileName = null)
        {
            FileName = fileName ?? LogHelper.CreateLogFile();

            try
            {
                using (File.OpenWrite(FileName))
                {
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"{nameof(FileLogPublisher)}.ctor: IO error creating / opening file - '{FileName}'",
                    ex);
            }

            _logEntries = new ConcurrentQueue<LogEntry>();

            _cancellationTokenSource = new CancellationTokenSource();

            _backgroundTask = Task.Factory.StartNew(() =>
            {
                BackgroundTask(_cancellationTokenSource.Token, FileName);
            });
        }

        public string FileName { get; }

        public Guid Id()
        {
            return _id;
        }

        public void Publish(LogEntry entry)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(
                    $"{nameof(FileLogPublisher)}.{nameof(Publish)}: Object already disposed");
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

        private void BackgroundTask(CancellationToken cancellationToken, string fileName)
        {
            try
            {
                var nextFlush = DateTime.UtcNow.AddSeconds(FlushInterval / 2);

                var isFlushed = true;

                using (var stream = new StreamWriter(fileName, true, Encoding.UTF8))
                {
                    while (true)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            stream.Flush();
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        if (_logEntries.TryDequeue(out var entry))
                        {
                            stream.WriteLine(entry.ToString());
                            isFlushed = false;

                            if (DateTime.UtcNow > nextFlush)
                            {
                                stream.Flush();
                                nextFlush = DateTime.UtcNow.AddSeconds(FlushInterval);
                                isFlushed = true;
                            }
                        }
                        else
                        {
                            if (!isFlushed && DateTime.UtcNow > nextFlush)
                            {
                                stream.Flush();
                                nextFlush = DateTime.UtcNow.AddSeconds(FlushInterval);
                                isFlushed = true;
                            }

                            Thread.Sleep(SleepInterval);
                        }
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

            var maxRetries = 30;

            while (_backgroundTask?.Status == TaskStatus.Running && maxRetries-- > 0)
            {
                Thread.Sleep(75);
            }
        }
    }
}