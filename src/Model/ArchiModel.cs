// ArchiModel.cs
// 
// Created: 2019-09-19T8:42 PM
// Updated: 2019-09-21T10:52 PM
// 
// Copyright 2019 (c) Jim Schilling
// All Rights Reserved.
// 
// MIT License

#region

using System;
using System.IO;
using System.Xml.Linq;
using ArchiToolbox.Logging;

#endregion

namespace ArchiToolbox.Model
{
    public class ArchiModel
    {
        private static readonly Logger _logger = new Logger(nameof(ArchiModel));

        internal static readonly string ElemProperty = "property";

        internal static readonly string ElemElement = "element";

        internal static readonly string ElemFolder = "folder";

        internal static readonly string AttrId = "id";

        internal static readonly string AttrName = "name";

        internal static readonly string AttrType = "type";

        internal static readonly string AttrKey = "key";

        internal static readonly string AttrValue = "value";

        internal static readonly XNamespace XsiTypeNamespace = "http://www.w3.org/2001/XMLSchema-instance";

        internal static readonly XName XsiType = XsiTypeNamespace + AttrType;

        internal static readonly string ArchimatePrefix = "archimate:";

        internal static readonly string DefaultFolderName = $"Import {DateTime.Now:yyyy-MMM-dd}";

        public ArchiModel(string fileName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    throw new ArgumentException($"{nameof(fileName)} is null or whitespace");
                }

                if (!File.Exists(fileName))
                {
                    throw new ArgumentException($"File not found - '{fileName}'");
                }

                Xml = XElement.Load(fileName);
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ArchiModel)}.ctor: {ex.Message}");
                throw;
            }
        }

        internal XElement Xml { get; private set; }
    }
}