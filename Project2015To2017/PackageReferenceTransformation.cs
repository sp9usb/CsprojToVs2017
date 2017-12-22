﻿using Project2015To2017.Definition;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace Project2015To2017
{
    internal sealed class PackageReferenceTransformation : ITransformation
    {
        public Task TransformAsync(XDocument projectFile, DirectoryInfo projectFolder, Project definition)
        {
            var packagesConfig = projectFolder.GetFiles("packages.config", SearchOption.TopDirectoryOnly);
            if (packagesConfig == null || packagesConfig.Length == 0)
            {
                Console.WriteLine("Packages.config file not found.");
                return Task.CompletedTask;
            }

            try
            {
                XDocument document;
                using (var stream = File.Open(packagesConfig[0].FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    document = XDocument.Load(stream);
                }

                var testReferences = Array.Empty<PackageReference>();
                if (definition.Type == ApplicationType.TestProject)
                {
                    testReferences = new[]
                    {
                        new PackageReference { Id = "Microsoft.NET.Test.Sdk", Version = "15.0.0" },
                        new PackageReference { Id = "MSTest.TestAdapter", Version = "1.1.11" },
                        new PackageReference { Id = "MSTest.TestFramework", Version = "1.1.11" }
                    };

                    if (definition.TargetFrameworks.Contains("net40"))
                    {
                        Console.WriteLine($"Warning - target framework net40 is not compatible with the MSTest NuGet packages. Please consider updating the target framework of your test project(s)");
                    }
                }

                XNamespace nsSys = "http://schemas.microsoft.com/developer/msbuild/2003";
                var existingPackageReferences = projectFile.Root.Elements(nsSys + "ItemGroup").Elements(nsSys + "PackageReference").Select(x => new PackageReference
                {
                    Id = x.Attribute("Include").Value,
                    Version = x.Attribute("Version")?.Value ?? x.Element(nsSys + "Version").Value,
                    IsDevelopmentDependency = x.Element(nsSys + "PrivateAssets") != null
                });

                definition.PackageReferences = document.Element("packages").Elements("package").Select(x => new PackageReference
                {
                    Id = x.Attribute("id").Value,
                    Version = x.Attribute("version").Value,
                    IsDevelopmentDependency = x.Attribute("developmentDependency")?.Value == "true"
                })
                .Concat(testReferences)
                .Concat(existingPackageReferences)
                .ToArray();

                foreach (var reference in definition.PackageReferences)
                {
                    Console.WriteLine($"Found nuget reference to {reference.Id}, version {reference.Version}.");
                }
            }
            catch(XmlException e)
            {
                Console.WriteLine($"Got xml exception reading packages.config: " + e.Message);
            }

            return Task.CompletedTask;
        }
    }
}
