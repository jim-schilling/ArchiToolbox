// ImportModel.cs
// 
// Created: 2019-09-19T8:34 PM
// Updated: 2019-09-21T10:52 PM
// 
// Copyright 2019 (c) Jim Schilling
// All Rights Reserved.
// 
// MIT License

#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ArchiToolbox.Logging;
using ArchiToolbox.Utility;

#endregion

namespace ArchiToolbox.Model
{
    public class ImportModel
    {
        private static readonly Logger _logger = new Logger(nameof(ImportModel));

        internal static readonly string ElemModel = "model";

        internal static readonly string ElemDefaults = "defaults";

        internal static readonly string ElemProperties = "properties";

        internal static readonly string ElemProperty = "property";

        internal static readonly string ElemGroup = "group";

        internal static readonly string ElemType = "type";

        internal static readonly string ElemName = "name";

        internal static readonly string ElemIndex = "index";

        internal static readonly string ElemElements = "elements";

        internal static readonly string ElemElement = "element";

        internal static readonly string AttrKey = "key";

        internal static readonly string AttrMask = "mask";

        internal static readonly string AttrDefault = "default";

        internal static readonly string AttrValue = "value";

        internal static readonly string AttrType = "type";

        internal static readonly string AttrGroup = "group";

        internal static readonly string AttrIndex = "index";

        internal static readonly string AttrName = "name";

        internal static readonly string AttrExclude = "exclude";

        public ImportModel(string fileName)
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

                LoadDefaults();

                LoadElements();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ImportModel)}.ctor: {ex.Message}");
                throw;
            }
        }

        internal XElement Xml { get; private set; }

        internal string Index { get; private set; }

        internal List<string> Properties { get; private set; }

        internal List<string> ExcludeProperties { get; private set; }

        internal string Group { get; private set; }

        internal string NameMask { get; private set; }

        internal List<string> NameParts { get; private set; }

        internal string TypeDefault { get; private set; }

        internal string Type { get; private set; }

        internal List<XElement> Elements { get; private set; }

        private void LoadDefaults()
        {
            try
            {
                var defaults = (from a in Xml.Elements(ElemDefaults)
                    select a).Single();

                Properties = (from a in defaults.Elements(ElemProperties)
                    from b in a.Elements(ElemProperty)
                    select (string) b.Attribute(AttrKey)).ToList();

                ExcludeProperties = (from a in defaults.Elements(ElemProperties)
                    from b in a.Elements(ElemProperty)
                    where b.HasAttribute(AttrExclude)
                    select (string) b.Attribute(AttrKey)).ToList();

                Group = (from a in defaults.Elements(ElemGroup)
                    from b in a.Elements(ElemProperty)
                    select (string) b.Attribute(AttrKey)).Single();

                Index = (from a in defaults.Elements(ElemIndex)
                    from b in a.Elements(ElemProperty)
                    select (string) b.Attribute(AttrKey)).Single();

                NameMask = (from a in defaults.Elements(ElemName)
                    select (string) a.Attribute(AttrMask)).Single();

                NameParts = (from a in defaults.Elements(ElemName)
                    from b in a.Elements(ElemProperty)
                    select (string) b.Attribute(AttrKey)).ToList();

                TypeDefault = (from a in defaults.Elements(ElemType)
                    select (string) a.Attribute(AttrDefault)).Single();

                Type = (from a in defaults.Elements(ElemType)
                    from b in a.Elements(ElemProperty)
                    select (string) b.Attribute(AttrKey) ?? string.Empty).Single();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(LoadDefaults)}: {ex.Message}");
                throw;
            }
        }

        private void LoadElements()
        {
            try
            {
                Elements = (from a in Xml.Elements(ElemElements)
                    from b in a.Elements(ElemElement)
                    select b).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(LoadElements)}: {ex.Message}");
                throw;
            }
        }
    }
}