using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using Fluent.Net;
using Fluent.Net.RuntimeAst;
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
                return messageId;

            if (!TryGetString(messageId, out var msg))
            {
                Logger.WarningS("Loc", $"Unknown messageId ({_defaultCulture.IetfLanguageTag}): {messageId}");
                msg = messageId;
            }

            return msg;
        }

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value)
        {
            if (!TryGetNode(messageId, out var context, out var node))
            {
                value = null;
                return false;
            }

            value = context.Format(node, null, null);
            return true;
        }

        public string GetString(string messageId, params (string, object)[] args0)
        {
            if (_defaultCulture == null)
                return messageId;

            if (!TryGetString(messageId, out var msg, args0))
            {
                Logger.WarningS("Loc", $"Unknown messageId ({_defaultCulture.IetfLanguageTag}): {messageId}");
                msg = messageId;
            }

            return msg;
        }

        public bool TryGetString(string messageId, [NotNullWhen(true)] out string? value, params (string, object)[] args0)
        {
            if (!TryGetNode(messageId, out var context, out var node))
            {
                value = null;
                return false;
            }

            var args = new Dictionary<string, object>();
            foreach (var (k, v) in args0)
            {
                args.Add(k, v);
            }

            value = context.Format(node, args, null);
            return false;
        }

        private bool TryGetNode(
            string messageId,
            [NotNullWhen(true)] out MessageContext? ctx,
            [NotNullWhen(true)] out Node? node)
        {
            if (_defaultCulture == null)
            {
                ctx = null;
                node = null;
                return false;
            }

            ctx = _contexts[_defaultCulture];
            string? attribName = null;

            if (messageId.Contains('.'))
            {
                var split = messageId.Split('.');
                messageId = split[0];
                attribName = split[1];
            }

            var message = ctx.GetMessage(messageId);

            if (message == null)
            {
                node = null;
                return false;
            }

            if (attribName != null)
            {
                if (!message.Attributes.TryGetValue(attribName, out var attrib))
                {
                    node = null;
                    return false;
                }

                node = attrib;
            }
            else
            {
                node = message;
            }

            return true;
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
                new MessageContextOptions {UseIsolating = false}
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

        private static void _loadFromFile(IResourceManager resourceManager, ResourcePath filePath,
            MessageContext context)
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
