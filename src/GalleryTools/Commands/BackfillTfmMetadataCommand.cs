// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.CommandLineUtils;
using NuGet.Client;
using NuGet.ContentModel;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.RuntimeModel;
using NuGet.Services.Entities;
using NuGetGallery;

namespace GalleryTools.Commands
{
    public sealed class BackfillTfmMetadataCommand : BackfillCommand<Tuple<string, string>>
    {
        protected override string MetadataFileName => "tfmMetadata.txt";
        
        protected override MetadataSourceType SourceType => MetadataSourceType.Entities;
        
        protected override string QueryIncludes => $"{nameof(Package.SupportedFrameworks)}";

        protected override int LimitTo => 100000;

        public static void Configure(CommandLineApplication config)
        {
            Configure<BackfillTfmMetadataCommand>(config);
        }

        protected override Tuple<string, string> ReadMetadata(IList<string> files, Package package)
        {
            // Existing supported frameworks values
            var legacyTfms = string.Empty;
            if (package.SupportedFrameworks != null && package.SupportedFrameworks.Any())
            {
                legacyTfms = string.Join(";", package.SupportedFrameworks.Select(f => f.FrameworkName?.GetShortFolderName() ?? ""));
            }

            // PatternSet-based TFMs
            var patternSetTfms = string.Empty;
            if (files != null && files.Any())
            {
                var contentItemCollection = new ContentItemCollection();
                contentItemCollection.Load(files);
                var runtimeGraph = new RuntimeGraph();
                var conventions = new ManagedCodeConventions(runtimeGraph);
                var runtimeAssembliesPatternSet = conventions.Patterns.RuntimeAssemblies;
                try
                {
                    var groups = contentItemCollection.FindItemGroups(runtimeAssembliesPatternSet).ToList();
                    foreach (var group in groups)
                    {
                        var targetFrameworkMoniker =
                            ((NuGetFramework) group.Properties[
                                ManagedCodeConventions.PropertyNames.TargetFrameworkMoniker]).GetShortFolderName();
                        if (string.IsNullOrEmpty(targetFrameworkMoniker))
                        {
                            continue;
                        }

                        patternSetTfms += patternSetTfms == string.Empty
                            ? targetFrameworkMoniker
                            : $";{targetFrameworkMoniker}";
                    }
                }
                catch (Exception)
                {
                    // fail silently without providing a value
                }
            }

            return Tuple.Create(legacyTfms, patternSetTfms);
        }

        /// <summary>
        /// Takes a file string (e.g. "lib\foo") and returns only the dir below lib, all other cases return null
        /// </summary>
        /// <param name="file">file path to analyze</param>
        private string FileToLibraryFrameworkString(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return null;
            }
            
            var fileDirs = file.Split('/');
            if (fileDirs.Length < 3)
            {
                return null;
            }

            if (!fileDirs[0].Equals("ref", StringComparison.OrdinalIgnoreCase) && !fileDirs[0].Equals("lib", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return fileDirs[1].ToLowerInvariant();
        }

        protected override bool ShouldWriteMetadata(Tuple<string, string> metadata) => true;

        protected override void ConfigureClassMap(PackageMetadataClassMap map)
        {
            map.Map(x => x.Metadata.Item1).Index(3);
            map.Map(x => x.Metadata.Item2).Index(4);
        }

        protected override void UpdatePackage(Package package, Tuple<string, string> metadata)
        {
/*            if (metadata == null || metadata.Length == 0)
            {
                return;
            }
*/
//            package.SupportedFrameworks = metadata.Split(',')
//                .Select(f => new PackageFramework {Package = package, TargetFramework = f}).ToArray();
        }
    }
}
