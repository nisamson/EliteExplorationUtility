using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyModel;
using Nullable.Extensions;

#pragma warning disable CS8604
#nullable disable

namespace EEU.Utils.Init;

public class Ensure {
    private static string? CheckLoadable(string package, string library) {
        var runtimeLibrary = DependencyContext.Default?.RuntimeLibraries.FirstOrDefault(l => l.Name == package);
        if (runtimeLibrary == null)
            return null;

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
        if (assetPath != default) {
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
