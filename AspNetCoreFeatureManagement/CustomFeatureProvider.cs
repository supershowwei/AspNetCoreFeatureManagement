using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.FeatureManagement;

namespace AspNetCoreFeatureManagement
{
    public class CustomFeatureProvider : IFeatureDefinitionProvider
    {
        private const string CacheKey = "CustomFeatureProvider";
        private const string FeatureFiltersSectionName = "EnabledFor";
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMemoryCache memoryCache;
        private readonly SemaphoreSlim locker = new SemaphoreSlim(1, 1);
        private readonly ConcurrentDictionary<string, FeatureDefinition> definitions;
        private IConfiguration configuration;

        public CustomFeatureProvider(IHttpClientFactory httpClientFactory, IMemoryCache memoryCache)
        {
            this.httpClientFactory = httpClientFactory;
            this.memoryCache = memoryCache;

            this.definitions = new ConcurrentDictionary<string, FeatureDefinition>();
        }

        public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
        {
            await this.TryReloadConfiguration();

            var definition = this.definitions.GetOrAdd(featureName, name => this.ReadFeatureDefinition(name));

            return definition;
        }

        public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
        {
            await this.TryReloadConfiguration();

            foreach (var featureSection in this.GetFeatureDefinitionSections())
            {
                yield return this.definitions.GetOrAdd(featureSection.Key, _ => this.ReadFeatureDefinition(featureSection));
            }
        }

        private async Task TryReloadConfiguration()
        {
            if (!this.memoryCache.TryGetValue(CacheKey, out _))
            {
                await this.locker.WaitAsync();

                if (this.memoryCache.TryGetValue(CacheKey, out _))
                {
                    this.locker.Release();
                }
                else
                {
                    using (var httpClient = this.httpClientFactory.CreateClient())
                    {
                        var response = await httpClient.GetStreamAsync("http://localhost:5000/feature-management-settings");

                        this.configuration = new ConfigurationBuilder().AddJsonStream(response).Build();
                    }

                    this.definitions.Clear();

                    this.memoryCache.Set(CacheKey, new object(), TimeSpan.FromMinutes(1));

                    this.locker.Release();
                }
            }
        }

        private FeatureDefinition ReadFeatureDefinition(string featureName)
        {
            var configurationSection = this.GetFeatureDefinitionSections().FirstOrDefault(section => section.Key.Equals(featureName, StringComparison.OrdinalIgnoreCase));

            if (configurationSection == null)
            {
                return null;
            }

            return this.ReadFeatureDefinition(configurationSection);
        }

        private FeatureDefinition ReadFeatureDefinition(IConfigurationSection configurationSection)
        {
            var enabledFor = new List<FeatureFilterConfiguration>();

            var val = configurationSection.Value;

            if (string.IsNullOrEmpty(val))
            {
                val = configurationSection[FeatureFiltersSectionName];
            }

            if (!string.IsNullOrEmpty(val) && bool.TryParse(val, out bool result) && result)
            {
                enabledFor.Add(new FeatureFilterConfiguration { Name = "AlwaysOn" });
            }
            else
            {
                var filterSections = configurationSection.GetSection(FeatureFiltersSectionName).GetChildren();

                foreach (var filterSection in filterSections)
                {
                    if (int.TryParse(filterSection.Key, out int i) && !string.IsNullOrEmpty(filterSection[nameof(FeatureFilterConfiguration.Name)]))
                    {
                        enabledFor.Add(
                            new FeatureFilterConfiguration
                            {
                                Name = filterSection[nameof(FeatureFilterConfiguration.Name)],
                                Parameters = filterSection.GetSection(nameof(FeatureFilterConfiguration.Parameters))
                            });
                    }
                }
            }

            return new FeatureDefinition { Name = configurationSection.Key, EnabledFor = enabledFor };
        }

        private IEnumerable<IConfigurationSection> GetFeatureDefinitionSections()
        {
            const string FeatureManagementSectionName = "FeatureManagement";

            if (this.configuration.GetChildren().Any(s => s.Key.Equals(FeatureManagementSectionName, StringComparison.OrdinalIgnoreCase)))
            {
                return this.configuration.GetSection(FeatureManagementSectionName).GetChildren();
            }
            else
            {
                return this.configuration.GetChildren();
            }
        }
    }
}