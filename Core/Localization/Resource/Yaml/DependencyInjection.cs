// Decompiled with JetBrains decompiler
// Type: NArchitecture.Core.Localization.Resource.Yaml.DependencyInjection.ServiceCollectionResourceLocalizationManagerExtension
// Assembly: Core.Localization.Resource.Yaml.DependencyInjection, Version=1.0.1.0, Culture=neutral, PublicKeyToken=null
// MVID: 07BCA012-3555-4206-BCF5-E654C1033A4A
// Assembly location: /Users/hgoksal/.nuget/packages/narchitecture.core.localization.resource.yaml.dependencyinjection/1.0.1/lib/net8.0/Core.Localization.Resource.Yaml.DependencyInjection.dll

#nullable enable
using System.Reflection;
using Core.Localization.Abstraction;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Localization.Resource.Yaml;

public static class ServiceCollectionResourceLocalizationManagerExtension
{
  public static IServiceCollection AddYamlResourceLocalization(this IServiceCollection services)
  {
    services.AddScoped<ILocalizationService, ResourceLocalizationManager>((Func<IServiceProvider, ResourceLocalizationManager>) (_ =>
    {
      Dictionary<string, Dictionary<string, string>> resources = new Dictionary<string, Dictionary<string, string>>();
      foreach (string directory in Directory.GetDirectories(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Features")))
      {
        string fileName = Path.GetFileName(directory);
        string path = Path.Combine(directory, "Resources", "Locales");
        if (Directory.Exists(path))
        {
          foreach (string file in Directory.GetFiles(path))
          {
            string withoutExtension = Path.GetFileNameWithoutExtension(file);
            int num = withoutExtension.IndexOf('.');
            string str = withoutExtension;
            int startIndex = num + 1;
            string key = str.Substring(startIndex, str.Length - startIndex);
            if (File.Exists(file))
            {
              if (!resources.ContainsKey(key))
                resources.Add(key, new Dictionary<string, string>());
              resources[key].Add(fileName, file);
            }
          }
        }
      }
      return new ResourceLocalizationManager(resources);
    }));
    return services;
  }
}