// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    public class KeySwapConfigurationProvider : IConfigurationProvider
    {
        private readonly IConfigurationProvider configurationProvider;
        private readonly IDictionary<string, string> keysToSwap;
        private readonly IDictionary<string, string[]> keysToSwapByValue;

        public KeySwapConfigurationProvider(
            IConfigurationProvider configurationProvider,
            IDictionary<string, string> keysToSwap)
        {
            this.configurationProvider = configurationProvider ?? throw new ArgumentNullException(nameof(configurationProvider));
            this.keysToSwap = keysToSwap ?? throw new ArgumentNullException(nameof(keysToSwap));
            keysToSwapByValue = IndexByValue(keysToSwap);
        }

        static IDictionary<string, string[]> IndexByValue(IDictionary<string, string> remappedKeys)
            => remappedKeys
                   .GroupBy(s => s.Value, v => v.Key)
                   .ToDictionary(s => s.Key, v => v.ToArray());

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string parentPath)
        {
            var children =
                configurationProvider.GetChildKeys(earlierKeys, parentPath)
                                     .ToArray();

            var mapKeys =
                children.SelectMany(k =>
                {
                    if (keysToSwapByValue.TryGetValue(k, out var v))
                    {
                        return v;
                    }

                    return Array.Empty<string>();
                });

            return children.Concat(mapKeys).Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public IChangeToken GetReloadToken()
            => configurationProvider.GetReloadToken();

        public void Load()
            => configurationProvider.Load();

        string GetKey(string key)
        {
            if (keysToSwap.TryGetValue(key, out var newKey))
            {
                return newKey;
            }

            return key;
        }

        public void Set(string key, string value)
            => configurationProvider.Set(GetKey(key), value);

        public bool TryGet(string key, out string value)
            => configurationProvider.TryGet(GetKey(key), out value);
    }

    public class KeySwapConfigurationSource : IConfigurationSource
    {
        public IConfigurationSource ConfigurationSource { get; set; }
        public IDictionary<string, string> KeysToSwap { get; set; }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new KeySwapConfigurationProvider(ConfigurationSource.Build(builder), KeysToSwap ?? new Dictionary<string, string>());
        }
    }

    public class KeySwapConfigurationBuilder : IConfigurationBuilder
    {
        private readonly IConfigurationBuilder configurationBuilder;
        private readonly IDictionary<string, string> keysToSwap;

        public KeySwapConfigurationBuilder(
            IConfigurationBuilder configurationBuilder,
            IDictionary<string, string> keysToSwap)
        {
            this.keysToSwap = keysToSwap;
            this.configurationBuilder = configurationBuilder;
        }

        public IDictionary<string, object> Properties => configurationBuilder.Properties;

        public IList<IConfigurationSource> Sources => configurationBuilder.Sources;

        public IConfigurationBuilder Add(IConfigurationSource source)
        {
            var s = new KeySwapConfigurationSource { ConfigurationSource = source, KeysToSwap = keysToSwap };
            configurationBuilder.Add(s);
            return this;
        }

        public IConfigurationRoot Build()
            => configurationBuilder.Build();
    }

    public static class KeySwapExtensions
    {
        public static IConfigurationBuilder AddKeySwapper(
            this IConfigurationBuilder configurationBuilder,
            Action<IConfigurationBuilder> configureBuilder,
            IDictionary<string, string> keysToSwap)
        {
            configureBuilder(new KeySwapConfigurationBuilder(configurationBuilder, keysToSwap));

            return configurationBuilder;
        }
    }
}
