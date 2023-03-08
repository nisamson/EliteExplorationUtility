// EliteExplorationUtility - EEU - Ensure.cs
// Copyright (C) 2023 Nick Samson
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Affero General Public License for more details.
// 
// You should have received a copy of the GNU Affero General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyModel;

#pragma warning disable CS8604
#nullable disable

namespace EEU.Utils.Init;

public class Ensure {
#pragma warning disable CS8632
    private static string? CheckLoadable(string package, string library) {
    #pragma warning restore CS8632
        var runtimeLibrary = DependencyContext.Default?.RuntimeLibraries.FirstOrDefault(l => l.Name == package);
        if (runtimeLibrary == null) {
            return null;
        }

        string sharedLibraryExtension;
        string pathVariableName;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            sharedLibraryExtension = ".dll";
            pathVariableName = "PATH";
        } else {
            // NB: Modifying the path at runtime only works on Windows. On Linux and Mac, set LD_LIBRARY_PATH or
            //     DYLD_LIBRARY_PATH before running the app
            return null;
        }

        var candidateAssets = new Dictionary<(string Package, string Asset), int>();
        var rid = RuntimeInformation.RuntimeIdentifier;
        Debug.Assert(DependencyContext.Default != null, "DependencyContext.Default != null");
        var rids = DependencyContext.Default.RuntimeGraph.First(g => g.Runtime == rid).Fallbacks.ToList();
        rids.Insert(0, rid);
        // if (rid.StartsWith("win")) {
        //     var match = Regex.Match(rid, @"win\d*-(?<arch>.*)");
        //     if (match.Success) {
        //         var arch = match.Groups["arch"];
        //         rids.Add($"win-{arch}");
        //     }
        // }

        foreach (var group in runtimeLibrary.NativeLibraryGroups) {
            foreach (var file in group.RuntimeFiles) {
                if (string.Equals(
                        Path.GetFileName(file.Path),
                        library + sharedLibraryExtension,
                        StringComparison.OrdinalIgnoreCase
                    )) {
                    var fallbacks = rids.IndexOf(group.Runtime);
                    if (fallbacks != -1) {
                        candidateAssets.Add((runtimeLibrary.Path!, file.Path), fallbacks);
                    }
                }
            }
        }

        var assetPath = candidateAssets
            .OrderBy(p => p.Value)
            .Select(p => p.Key)
            .FirstOrDefault();

        string assetFullPath = null;
        if (assetPath != default((string Package, string Asset))) {
            string assetDirectory = null;
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, assetPath.Asset))) {
                // NB: Framework-dependent deployments copy assets to the application base directory
                assetDirectory = Path.Combine(
                    AppContext.BaseDirectory,
                    Path.GetDirectoryName(assetPath.Asset.Replace('/', Path.DirectorySeparatorChar))
                );
            } else {
                var probingDirectories = ((string) AppDomain.CurrentDomain.GetData("PROBING_DIRECTORIES"))
                    .Split(Path.PathSeparator);
                foreach (var directory in probingDirectories) {
                    var candidateFullPath = Path.Combine(
                        directory,
                        (assetPath.Package + "/" + assetPath.Asset).Replace('/', Path.DirectorySeparatorChar)
                    );
                    if (File.Exists(candidateFullPath)) {
                        assetFullPath = candidateFullPath;
                    }
                }

                Debug.Assert(assetFullPath != null);

                assetDirectory = Path.GetDirectoryName(assetFullPath);
            }

            Debug.Assert(assetDirectory != null);

            var path = new HashSet<string>(Environment.GetEnvironmentVariable(pathVariableName).Split(Path.PathSeparator));

            if (path.Add(assetDirectory)) {
                Environment.SetEnvironmentVariable(pathVariableName, string.Join(Path.PathSeparator, path));
                return assetFullPath;
            }
        }

        return null;
    }

    public static void Loadable(string package, string library) {
        var res = CheckLoadable(package, library);
        if (res is null) {
            throw new DllNotFoundException($"{library}");
        }
    }
}

#nullable restore
