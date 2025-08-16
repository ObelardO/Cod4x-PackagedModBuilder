using System;
using System.IO;
using System.IO;
using System.IO.Compression;

namespace Cod4PackagedBuilder
{
    internal class Program
    {
        private static string _workDir = string.Empty;
        private static BuildMode _buildMode;
        private static BuildLang _buildLang;

        private static string _baseDir = string.Empty;
        private static string _releaseDir = string.Empty;

        private const string BaseIwdName = "z_ow_main";

        private static readonly (string subDir, string filePattern)[] BaseAssets =
        {
            ("images", "*.iwi"),
            ("weapons/mp", "*_mp"),
            ("", "mod.arena"),
            ("sound", "*.mp3"),
            ("sound", "*.wav"),
            ("rulesets/openwarfare", "*.gsc"),
            ("rulesets", "leagues.gsc")
        };
            
        public static void Main(string[] args)
        {
            _workDir = Environment.CurrentDirectory;

            for (var i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1 && args[i] == "-workdir")
                {
                    _workDir = args[i + 1];
                }
            }
            
            Console.WriteLine($"Starting in workdir: {_workDir}");
            
            if (!Directory.Exists(_workDir))
            {
                Environment.Exit(1);
                return;
            }
            
            

            SelectMode();
            
            Console.ReadLine();
            
            Environment.Exit(0);
        }

        private static void SelectMode()
        {
            Console.WriteLine("_________________________________________________________________");
            Console.WriteLine(" Please select build mode:");
            Console.WriteLine("\t1. All (base and packages .iwd and .ff files)");
            Console.WriteLine("\t2. Base only (base .iwd and .ff files)");
            Console.WriteLine("\t3. Base IWD (base .iwd files)");
            Console.WriteLine("\t4. Base FF (base .ff file)");
            Console.WriteLine("\t5. Packages only (packages .iwd and .ff files)");
            Console.WriteLine("\t6. Packages IWD (packages .iwd files)");
            Console.WriteLine("\t7. Packages FF (packages .ff file)");
            Console.WriteLine("");
            Console.WriteLine("\t0. Exit");

            var buildModeInput = Console.ReadLine();
            
            if (int.TryParse(buildModeInput, out var buildModeInt))
            {
                if (buildModeInt < 0 || buildModeInt >= (int) BuildMode.Length)
                {
                    InvalidInput();
                    return;
                }

                if (buildModeInt == 0)
                {
                    Environment.Exit(0);
                    return;
                }

                _buildMode = (BuildMode) buildModeInt;

                Console.WriteLine($" Selected mode: {buildModeInt}");
                Console.WriteLine("\n");
                
                SelectLang();
            }
            else
            {
                InvalidInput();
            }

            void InvalidInput()
            {
                Console.WriteLine(" Please enter valid value.");
                Console.WriteLine("\n");

                SelectMode();
            }
        }

        private static void SelectLang()
        {
            Console.WriteLine("_________________________________________________________________");
            Console.WriteLine(" Please choose the language you would like to compile:");
            Console.WriteLine("\t1. English");
            Console.WriteLine("\t2. French");
            Console.WriteLine("\t3. German");
            Console.WriteLine("\t4. Italian");
            Console.WriteLine("\t5. Portuguese");
            Console.WriteLine("\t6. Russian");
            Console.WriteLine("\t7. Spanish");
            Console.WriteLine("");
            Console.WriteLine("\t0. Back");
            
            var buildLangInput = Console.ReadLine();
            
            if (int.TryParse(buildLangInput, out var buildLangInt))
            {
                if (buildLangInt < 0 || buildLangInt >= (int) BuildLang.Length)
                {
                    InvalidInput();
                    return;
                }

                if (buildLangInt == 0)
                {
                    SelectMode();
                    return;
                }

                _buildLang = (BuildLang) buildLangInt;
                
                Console.WriteLine($" Selected language: {_buildLang.ToString()}");
                Console.WriteLine("\n");

                Build();
            }
            else
            {
                InvalidInput();
            }

            void InvalidInput()
            {
                Console.WriteLine(" Please enter valid value.");
                Console.WriteLine("\n");

                SelectLang();
            }
        }

        private static void Build()
        {
            _baseDir = Path.Combine(_workDir, "_Base");
            _releaseDir = Path.Combine(_workDir, "_Release");

            if (!Directory.Exists(_baseDir))
            {
                Environment.Exit(2);
                return;
            }

            if (!Directory.Exists(_releaseDir))
            {
                Directory.CreateDirectory(_releaseDir);
            }
            
            switch (_buildMode)
            {
                case BuildMode.All:

                    BuildIwd(_baseDir, BaseIwdName, BaseAssets);
                    
                    break;
            }
        }

        private static void BuildIwd(string dir, string iwdName, (string subDir, string filePattern)[] assets)
        {
            var iwdPath = Path.Combine(_releaseDir, $"{iwdName}.iwd");
            
            if (!Directory.Exists(dir))
            {
                Environment.Exit(3);
                return;
            }

            if (File.Exists(iwdPath))
            {
                File.Delete(iwdPath);
            }

            foreach (var (subDir, filePattern) in assets)
            {
                AddFilesToIwd(iwdPath, dir, subDir, filePattern);
            }
        }
        
        public static void AddFilesToIwd(string iwdPath, string packDir, string sourceDir, string searchPattern)
        {
            var filesDir = Path.Combine(packDir, sourceDir);

            if (!Directory.Exists(filesDir))
            {
                return;
            }
            
            if (!File.Exists(iwdPath))
            {
                using (var zip = ZipFile.Open(iwdPath, ZipArchiveMode.Create)) { }
            }

            using var archive = ZipFile.Open(iwdPath, ZipArchiveMode.Update);
            
            var files = Directory.GetFiles(filesDir, searchPattern, SearchOption.AllDirectories);

            foreach (var file in files)
            {
                // Path in archive
                string relativePath = Path.GetRelativePath(filesDir, file);
                string entryName = Path.Combine(sourceDir, relativePath).Replace("\\", "/");

                // Remove existed file
                var existing = archive.GetEntry(entryName);
                existing?.Delete();

                // Add new file
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);

                Console.WriteLine($"[OK] {file} → {entryName}");
            }
        }
    }
    
    
    
    
    

    public struct BuildConfig
    {
        public BuildMode Mode;
        public string Language;
    }
    
    public enum BuildMode
    {
        All = 1,
        BaseOnly = 2,
        BaseIwd = 3,
        BaseFastFile = 4,
        PackagesOnly = 5,
        PackagesIwd = 6,
        PackagesFastFile = 7,
        
        Length = 8
    }

    public enum BuildLang
    {
        English = 1,
        French = 2,
        German = 3,
        Italian = 4,
        Portuguese = 5,
        Russian = 6,
        Spanish = 7,
        
        Length = 8
    }
}