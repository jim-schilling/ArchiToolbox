// ArchiImporter.cs
// 
// Created: 2019-09-19T10:21 PM
// Updated: 2019-09-21T9:58 AM
// 
// Copyright 2019 (c) Jim Schilling
// All Rights Reserved.
// 
// MIT License

#region

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using ArchiToolbox.Logging;

#endregion

namespace ArchiToolbox.Model
{
    public class ArchiImporter : IDisposable
    {
        private static readonly Logger _logger = new Logger(nameof(ArchiImporter));

        private readonly ConcurrentQueue<XElement> _addQueue;

        private readonly ArchiModel _archiModel;

        private readonly ImportModel _importModel;

        private readonly ConcurrentQueue<XElement> _importQueue;

        private readonly ConcurrentQueue<Tuple<XElement, XElement>> _updateQueue;

        private readonly int MaxBackgroundThreads = 3;

        private volatile Task[] _backgroundTasks;

        private volatile CancellationTokenSource[] _cancellationTokenSources;

        private bool _isDisposed;

        public ArchiImporter(ArchiModel archiModel, ImportModel importModel)
        {
            try
            {
                _archiModel = archiModel ?? throw new ArgumentNullException($"{nameof(archiModel)} is null");

                _importModel = importModel ?? throw new ArgumentNullException($"{nameof(importModel)} is null");

                _importQueue = new ConcurrentQueue<XElement>();

                _addQueue = new ConcurrentQueue<XElement>();

                _updateQueue = new ConcurrentQueue<Tuple<XElement, XElement>>();

                _cancellationTokenSources = new CancellationTokenSource[MaxBackgroundThreads];

                _backgroundTasks = new Task[MaxBackgroundThreads];

                for (var i = 0; i < MaxBackgroundThreads; i++)
                {
                    var threadNum = i;

                    _cancellationTokenSources[threadNum] = new CancellationTokenSource();

                    _backgroundTasks[threadNum] = Task.Factory.StartNew(() =>
                    {
                        BackgroundTask(_cancellationTokenSources[threadNum].Token);
                    });
                }

                LoadImportQueue();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ArchiImporter)}.ctor: {ex.Message}");
                throw;
            }
        }

        public int UpdateCount => _updateQueue.Count;

        public int AddCount => _addQueue.Count;

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                var retries = 25;
                var isRunning = false;

                foreach (var cts in _cancellationTokenSources)
                {
                    cts.Cancel();
                }

                do
                {
                    foreach (var task in _backgroundTasks)
                    {
                        if (task.Status == TaskStatus.Running)
                        {
                            isRunning = true;
                            break;
                        }
                    }

                    Thread.Sleep(75);
                } while (retries-- > 0 && isRunning);
            }
        }

        public void WaitOnSync()
        {
            while (_importQueue.Count > 0)
            {
                Thread.Sleep(1000);
            }
        }

        private void LoadImportQueue()
        {
            try
            {
                foreach (var element in _importModel.Elements)
                {
                    _importQueue.Enqueue(element);
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(LoadImportQueue)}: {ex.Message}");
                throw;
            }
        }

        private void BackgroundTask(CancellationToken cancellationToken)
        {
            try
            {
                Thread.Sleep(125);

                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (!_importQueue.TryDequeue(out var importElement))
                    {
                        Thread.Sleep(125);
                        continue;
                    }

                    var type = (string) importElement.Attribute(ImportModel.AttrType);

                    var name = (string) importElement.Attribute(ImportModel.AttrName);

                    var index = (string) importElement.Attribute(ImportModel.AttrIndex);

                    var archiRoot = (from a in _archiModel.Xml.Elements(ArchiModel.ElemFolder)
                        where (string) a.Attribute(ArchiModel.AttrName) == ArchiHelper.GetRootFolderName(type)
                        select a).Single();

                    var archiElements = (from a in archiRoot.Descendants(ArchiModel.ElemElement)
                        from b in a.Elements(ArchiModel.ElemProperty)
                        where (string) b.Attribute(ArchiModel.AttrKey) == _importModel.Index
                              && (string) b.Attribute(ArchiModel.AttrValue) == index
                        select a).ToList();

                    if (archiElements.Count == 0)
                    {
                        _addQueue.Enqueue(importElement);

                        _logger.Info($"Add element detected: {name}");

                        continue;
                    }

                    var importProperties = (from a in importElement.Elements(ImportModel.ElemProperty)
                                            where !_importModel.ExcludeProperties.Contains((string)a.Attribute(ImportModel.AttrKey))
                        select a).ToList();

                    foreach (var archiElement in archiElements)
                    {
                        if ((string) archiElement.Attribute(ArchiModel.AttrName) != name)
                        {
                            _updateQueue.Enqueue(new Tuple<XElement, XElement>(archiElement, importElement));

                            _logger.Info(
                                $"Name change detected: {(string) archiElement.Attribute(ArchiModel.AttrId)} {(string) archiElement.Attribute(ArchiModel.AttrName)}");

                            continue;
                        }

                        foreach (var importProperty in importProperties)
                        {
                            var archiProperty = (from a in archiElement.Elements(ArchiModel.ElemProperty)
                                where (string) a.Attribute(ArchiModel.AttrKey) == (string) importProperty.Attribute(ImportModel.AttrKey)
                                select a).SingleOrDefault();

                            if (archiProperty == null)
                            {
                                _updateQueue.Enqueue(new Tuple<XElement, XElement>(archiElement, importElement));

                                _logger.Info(
                                    $"Property add detected: {(string) archiElement.Attribute(ArchiModel.AttrId)} {(string) archiElement.Attribute(ArchiModel.AttrName)}");

                                break;
                            }

                            if ((string) archiProperty.Attribute(ArchiModel.AttrValue) !=
                                (string) importProperty.Attribute(ImportModel.AttrValue))
                            {
                                _updateQueue.Enqueue(new Tuple<XElement, XElement>(archiElement, importElement));

                                _logger.Info(
                                    $"Property update detected: {(string) archiElement.Attribute(ArchiModel.AttrId)} {(string) archiElement.Attribute(ArchiModel.AttrName)}");

                                break;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(BackgroundTask)}: {ex.Message}");
            }
        }

        private void SyncAdds()
        {
            try
            {
                while (_addQueue.TryDequeue(out var importElement))
                {
                    var archiElement = new XElement(ArchiModel.ElemElement);

                    archiElement.SetAttributeValue(ArchiModel.XsiType,
                        ArchiModel.ArchimatePrefix + (string) importElement.Attribute(ImportModel.AttrType));

                    archiElement.SetAttributeValue(ArchiModel.AttrName, (string) importElement.Attribute(ImportModel.AttrName));

                    archiElement.SetAttributeValue(ArchiModel.AttrId, $"{Guid.NewGuid():D}");

                    var importProperties = (from a in importElement.Elements(ImportModel.ElemProperty)
                        where !_importModel.ExcludeProperties.Contains((string)a.Attribute(ImportModel.AttrKey))
                                            select a).ToList();

                    foreach (var importProperty in importProperties)
                    {
                        var archiProperty = new XElement(ArchiModel.ElemProperty);

                        archiProperty.SetAttributeValue(ArchiModel.AttrKey, (string) importProperty.Attribute(ImportModel.AttrKey));

                        archiProperty.SetAttributeValue(ArchiModel.AttrValue, (string) importProperty.Attribute(ImportModel.AttrValue));

                        archiElement.Add(archiProperty);
                    }

                    var archiRoot = (from a in _archiModel.Xml.Elements(ArchiModel.ElemFolder)
                        where (string) a.Attribute(ArchiModel.AttrName) ==
                              ArchiHelper.GetRootFolderName((string) importElement.Attribute(ImportModel.AttrType))
                        select a).Single();

                    var archiFolder = (from a in archiRoot.Elements(ArchiModel.ElemFolder)
                        where (string) a.Attribute(ArchiModel.AttrName) == (string) importElement.Attribute(ImportModel.AttrGroup)
                        select a).SingleOrDefault();

                    if (archiFolder == null)
                    {
                        archiFolder = new XElement(ArchiModel.ElemFolder);

                        archiFolder.SetAttributeValue(ArchiModel.AttrName, (string) importElement.Attribute(ImportModel.AttrGroup));

                        archiFolder.SetAttributeValue(ArchiModel.AttrId, $"{Guid.NewGuid():D}");

                        archiRoot.Add(archiFolder);

                        _logger.Info($"Add new folder: {(string)archiFolder.Attribute(ArchiModel.AttrName)}");
                    }

                    archiFolder.Add(archiElement);

                    _logger.Info($"Add new element: {(string)archiElement.Attribute(ArchiModel.AttrName)}  {(string) archiElement.Attribute(ArchiModel.AttrId)}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(SyncAdds)}: {ex.Message}");
                throw;
            }
        }

        private void SyncUpdates()
        {
            try
            {
                while (_updateQueue.TryDequeue(out var qElements))
                {
                    var archiElement = qElements.Item1;
                    var importElement = qElements.Item2;

                    if ((string) archiElement.Attribute(ArchiModel.AttrName) != (string) archiElement.Attribute(ImportModel.AttrName))
                    {
                        _logger.Info($"Update element name: new='{(string)importElement.Attribute(ImportModel.AttrName)}' old='{(string)archiElement.Attribute(ArchiModel.AttrName)}'");
                        archiElement.SetAttributeValue(ArchiModel.AttrName, (string) importElement.Attribute(ImportModel.AttrName));
                    }

                    var importProperties = (from a in importElement.Elements(ImportModel.ElemProperty)
                        where !_importModel.ExcludeProperties.Contains((string)a.Attribute(ImportModel.AttrKey))
                                            select a).ToList();

                    var archiProperties = (from a in archiElement.Elements(ArchiModel.ElemProperty)
                        select a).ToList();

                    foreach (var importProperty in importProperties)
                    {
                        var archiProperty = (from a in archiProperties
                            where (string) a.Attribute(ArchiModel.AttrKey) == (string) importProperty.Attribute(ImportModel.AttrKey)
                            select a).SingleOrDefault();

                        if (archiProperty == null)
                        {
                            archiProperty = new XElement(ArchiModel.ElemProperty);

                            archiProperty.SetAttributeValue(ArchiModel.AttrKey, (string)importProperty.Attribute(ImportModel.AttrKey));

                            archiProperty.SetAttributeValue(ArchiModel.AttrValue, (string)importProperty.Attribute(ImportModel.AttrValue) ?? string.Empty);

                            archiElement.Add(archiProperty);

                            _logger.Info($"Add property: '{(string)archiProperty.Attribute(ArchiModel.AttrKey)}' = '{(string)archiProperty.Attribute(ArchiModel.AttrValue)}'");

                            continue;
                        }

                        if (((string) archiProperty.Attribute(ArchiModel.AttrValue) ?? string.Empty) !=
                            ((string) importProperty.Attribute(ImportModel.AttrValue) ?? string.Empty))
                        {
                            _logger.Info($"Update property: '{(string)archiProperty.Attribute(ArchiModel.AttrKey)}' new='{(string)importProperty.Attribute(ImportModel.AttrValue) ?? string.Empty}' old='{(string)archiProperty.Attribute(ArchiModel.AttrValue)}'");

                            archiProperty.SetAttributeValue(ArchiModel.AttrValue, (string)importProperty.Attribute(ImportModel.AttrValue) ?? string.Empty);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(SyncUpdates)}: {ex.Message}");
                throw;
            }
        }

        public void SyncChanges()
        {
            try
            {
                WaitOnSync();

                SyncUpdates();

                SyncAdds();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(SyncChanges)}: {ex.Message}");
                throw;
            }
        }

        public void Save(string fileName)
        {
            try
            {
                _archiModel.Xml.Save(fileName);
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(Save)}: {ex.Message}");
                throw;
            }
        }
    }
}