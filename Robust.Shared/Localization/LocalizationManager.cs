using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Fluent.Net;
using JetBrains.Annotations;
using Robust.Shared.ContentPack;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.Localization
{
    internal sealed class LocalizationManager : ILocalizationManagerInternal
    {
        private readonly Dictionary<CultureInfo, MessageContext> _contexts = new();

        private CultureInfo? _defaultCulture;

        public string GetString(string messageId)
        {
            if (_defaultCulture == null)
            {
                return messageId;
            }
            var context = _contexts[_defaultCulture];
            var message = context.GetMessage(messageId);

            if (message == null)
            {
                Logger.WarningS("Loc", $"Unknown messageId ({_defaultCulture.IetfLanguageTag}): {messageId}");
                return messageId;
            }

            return context.Format(message, null, null);
        }

        public string GetString(string messageId, params (string, object)[] args0)
        {
            if (_defaultCulture == null)
            {
                return messageId;
            }
            var context = _contexts[_defaultCulture];
            var message = context.GetMessage(messageId);
            var args = new Dictionary<string, object>();
            foreach (var vari in args0)
            {
                args.Add(vari.Item1, vari.Item2);
            }

            if (message == null)
            {
                Logger.WarningS("Loc", $"Unknown messageId ({_defaultCulture.IetfLanguageTag}): {messageId}");
                return messageId;
            }

            return context.Format(message, args, null);
        }

        /// <summary>
        /// Remnants of the old Localization system.
        /// It exists to prevent source errors and allow existing game text to *mostly* work
        /// </summary>
        [Obsolete]
        [StringFormatMethod("text")]
        public string GetString(string text, params object[] args)
        {
            return string.Format(text, args);
        }

        public CultureInfo? DefaultCulture
        {
            get => _defaultCulture;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (!_contexts.ContainsKey(value))
                {
                    throw new ArgumentException("That culture is not yet loaded and cannot be used.", nameof(value));
                }

                _defaultCulture = value;
                CultureInfo.CurrentCulture = value;
                CultureInfo.CurrentUICulture = value;
            }
        }

        public void LoadCulture(IResourceManager resourceManager, CultureInfo culture)
        {
            var context = new MessageContext(
                culture.Name,
                new MessageContextOptions { UseIsolating = false }
            );

            _contexts.Add(culture, context);

            _loadData(resourceManager, culture, context);
            if (DefaultCulture == null)
            {
                DefaultCulture = culture;
            }
        }

        public void AddLoadedToStringSerializer(IRobustMappedStringSerializer serializer)
        {
            /*
             * TODO: need to expose Messages on MessageContext in Fluent.NET
            serializer.AddStrings(StringIterator());

            IEnumerable<string> StringIterator()
            {
                foreach (var context in _contexts.Values)
                {
                    foreach (var (key, translations) in _context)
                    {
                        yield return key;

                        foreach (var t in translations)
                        {
                            yield return t;
                        }
                    }
                }
            }
            */
        }

        private static void _loadData(IResourceManager resourceManager, CultureInfo culture, MessageContext context)
        {
            // Load data from .ftl files.
            // Data is loaded from /Locale/<language-code>/*

            var root = new ResourcePath($"/Locale/{culture.IetfLanguageTag}/");

            foreach (var file in resourceManager.ContentFindFiles(root))
            {
                var ftlFile = root / file;
                _loadFromFile(resourceManager, ftlFile, context);
            }
        }

        private static void _loadFromFile(IResourceManager resourceManager, ResourcePath filePath, MessageContext context)
        {
            using (var fileStream = resourceManager.ContentFileRead(filePath))
            using (var reader = new StreamReader(fileStream, EncodingHelpers.UTF8))
            {
                var errors = context.AddMessages(reader);
                foreach (var error in errors)
                {
                    Logger.WarningS("Loc", error.Message);
                }
            }
        }
    }
}
