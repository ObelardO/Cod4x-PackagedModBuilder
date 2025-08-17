using System;
using System.Diagnostics;
using System.IO;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.VisualBasic;

namespace Cod4PackagedBuilder
{
    internal class Program
    {
        private static string _workDir = string.Empty;
        private static string _toolsDir = string.Empty;
        
        private static string _toolsRawDir = string.Empty;
        private static string _toolsZoneDir = string.Empty;
        private static string _toolsZoneSourceDir = string.Empty;
        private static string _toolsBinDir = string.Empty;
        private static string _toolsZoneSourceAssetsDir = string.Empty;
        
        private static string _baseDir = string.Empty;
        private static string _releaseDir = string.Empty;

        private static BuildMode _buildMode = BuildMode.All;
        private static BuildLang _buildLang = BuildLang.English;

        private const string BaseIwdName = "main";
        private const string BaseSoundsIwdName = "sounds";

        private static string _assetsListFileName = "mod.csv";
        private static string _ignoredAssetsListFileName = "mod_ignore.csv";
        
        private static readonly (string subDir, string filePattern)[] BaseAssets =
        {
            ("images", "*.iwi"),
            ("weapons/mp", "*_mp"),
            ("", "mod.arena"),
            ("rulesets/openwarfare", "*.gsc"),
            ("rulesets", "leagues.gsc")
        };
        
        private static readonly (string subDir, string filePattern)[] BaseSoundsAssets =
        {
            ("sound", "*.mp3"),
            ("sound", "*.wav")
        };
        
        private static readonly (string subDir, string filePattern)[] PackAssets = 
        {
            ("images", "*.iwi"),
            ("weapons/mp", "*_mp"),
            //("", "mod.arena"),
            ("rulesets/openwarfare", "*.gsc"),
            ("rulesets", "leagues.gsc"),
            ("sound", "*.mp3"),
            ("sound", "*.wav"),
        };

        public static void Main(string[] args)
        {
            _workDir = Environment.CurrentDirectory;
            _toolsDir = Path.Combine(_workDir, "..", "..");
            
            for (var i = 0; i < args.Length; i++)
            {
                if (i < args.Length - 1)
                {
                    var argValue = args[i + 1];
                    
                    switch (args[i].ToLower())
                    {
                        case "-workdir": _workDir = argValue; break;
                        case "-toolsdir": _toolsDir = argValue; break;
                    }
                }
            }

            _baseDir = Path.Combine(_workDir, "_Base");
            _releaseDir = Path.Combine(_workDir, "_Release");
            
            _toolsRawDir = Path.Combine(_toolsDir, "raw");
            _toolsZoneDir = Path.Combine(_toolsDir, "zone");
            _toolsZoneSourceDir = Path.Combine(_toolsDir, "zone_source");
            _toolsBinDir = Path.Combine(_toolsDir, "bin");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Starting in workdir: {_workDir}");

            if (!Directory.Exists(_workDir))
            {
                Shutdown(BuildResult.WorkDirNotFound);
                return;
            }

            if (!Directory.Exists(_toolsDir))
            {
                Shutdown(BuildResult.ToolsDirNotFound);
                return;
            }

            SelectMode();

            Console.ReadLine();

            Shutdown(BuildResult.Successful);
        }

        private static void SelectMode()
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(" Please select build mode:");
            Console.WriteLine("\t1. All (base and packages .iwd and .ff files)");
            Console.WriteLine("\t2. Base only (base .iwd and .ff files)");
            Console.WriteLine("\t3. Base IWD (base .iwd files)");
            Console.WriteLine("\t4. Base FF (base .ff file)");
            Console.WriteLine("\t5. Packages only (packages .iwd and .ff files)");
            Console.WriteLine("\t6. Packages IWD (packages .iwd files)");
            Console.WriteLine("\t7. Packages FF (packages .ff file)");
            Console.Write("\n");
            Console.WriteLine("\t0. Exit");

            var buildModeInput = Console.ReadLine();

            if (int.TryParse(buildModeInput, out var buildModeInt))
            {
                if (buildModeInt < 0 || buildModeInt >= (int)BuildMode.Length)
                {
                    InvalidInput();
                    return;
                }

                if (buildModeInt == 0)
                {
                    Environment.Exit(0);
                    return;
                }

                _buildMode = (BuildMode)buildModeInt;

                Console.WriteLine($" Selected mode: {buildModeInt}\n");

                SelectLang();
            }
            else
            {
                InvalidInput();
            }

            void InvalidInput()
            {
                Console.WriteLine(" Please enter valid value.\n");
                
                SelectMode();
            }
        }

        private static void SelectLang()
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(" Please choose the language you would like to compile:");
            Console.WriteLine("\t1. English");
            Console.WriteLine("\t2. French");
            Console.WriteLine("\t3. German");
            Console.WriteLine("\t4. Italian");
            Console.WriteLine("\t5. Portuguese");
            Console.WriteLine("\t6. Russian");
            Console.WriteLine("\t7. Spanish");
            Console.Write("\n");
            Console.WriteLine("\t0. Back");

            var buildLangInput = Console.ReadLine();

            if (int.TryParse(buildLangInput, out var buildLangInt))
            {
                switch (buildLangInt)
                {
                    case < 0 or >= (int)BuildLang.Length:
                        InvalidInput();
                        return;
                    case 0:
                        SelectMode();
                        return;
                }

                _buildLang = (BuildLang)buildLangInt;
                _toolsZoneSourceAssetsDir = Path.Combine(_toolsZoneSourceDir, _buildLang.ToString(), "assetlist");
                
                Console.WriteLine($" Selected language: {_buildLang.ToString()}\n");
                
                Build();
            }
            else
            {
                InvalidInput();
            }

            void InvalidInput()
            {
                Console.WriteLine(" Please enter valid value.\n");
                
                SelectLang();
            }
        }

        private static void Build()
        {
            if (!Directory.Exists(_baseDir))
            {
                Shutdown(BuildResult.BaseDirNotFound);
                return;
            }

            if (!Directory.Exists(_releaseDir))
            {
                Directory.CreateDirectory(_releaseDir);
            }
            
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(" Building mod...");

            File.Delete(Path.Combine(_toolsZoneSourceDir, _assetsListFileName));
            File.Delete(Path.Combine(_toolsZoneSourceAssetsDir, _ignoredAssetsListFileName));
            
            switch (_buildMode)
            {
                case BuildMode.All:
                    
                    LoadPackages();
                    
                    BuildIwd(_baseDir, BaseIwdName, BaseAssets);
                    BuildIwd(_baseDir, BaseSoundsIwdName, BaseSoundsAssets);
                    BuildPackagesIwd();
                    
                    PrepareFastFile(_baseDir, "Base");
                    PreparePackagesFastFile();
                    
                    BuildFastFile(_baseDir);

                    break;
                
                case BuildMode.BaseOnly:
                    
                    BuildIwd(_baseDir, BaseIwdName, BaseAssets);
                    BuildIwd(_baseDir, BaseSoundsIwdName, BaseSoundsAssets);

                    PrepareFastFile(_baseDir, "Base");

                    BuildFastFile(_baseDir);
                    
                    break;
                
                case BuildMode.BaseIwd:
                    
                    BuildIwd(_baseDir, BaseIwdName, BaseAssets);
                    BuildIwd(_baseDir, BaseSoundsIwdName, BaseSoundsAssets);
                    
                    break;
                
                case BuildMode.BaseFastFile:
                    
                    PrepareFastFile(_baseDir, "Base");

                    BuildFastFile(_baseDir);

                    break;
                case BuildMode.PackagesOnly:
                    break;
                case BuildMode.PackagesIwd:
                    break;
                case BuildMode.PackagesFastFile:
                    break;
                case BuildMode.Length:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Console.WriteLine(" Mod built successfully.");
            Shutdown(BuildResult.Successful);
        }
        
        private static void BuildIwd(string dir, string iwdName, (string subDir, string filePattern)[] assets)
        {
            iwdName = $"z_ow_{iwdName}.iwd";
            
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Building {iwdName}...");
            
            var iwdPath = Path.Combine(_releaseDir, iwdName);

            if (!Directory.Exists(dir))
            {
                Shutdown(BuildResult.IwdSourceDirNotFound);
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
            
            Console.WriteLine($" {iwdName} packed successfully.");
        }

        private static void BuildFastFile(string packDir)
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(" Building mod.ff...");
            //Console.WriteLine(" Starting linker...");
                
            var linker = new Process();
            linker.StartInfo.FileName = Path.Combine(_toolsBinDir, "linker_pc.exe");
            linker.StartInfo.Arguments = $"-language {_buildLang} -compress -cleanup mod"; // optional
            linker.StartInfo.WorkingDirectory = Path.Combine(_toolsBinDir);
            linker.StartInfo.UseShellExecute = false;
            linker.StartInfo.RedirectStandardOutput = true; // optional, if you want to read output
            linker.Start();

            string output = linker.StandardOutput.ReadToEnd(); // optional
            linker.WaitForExit(); // Waits for the process to finish

            Console.Write($" {output}");
            
            int exitCode = linker.ExitCode;

            if (exitCode != 0)
            {
                Console.WriteLine($" Linker failed with exit code {exitCode}.");
                Shutdown(BuildResult.LinkerFailed);
                return;
            }

            Console.WriteLine(" Linker finished successfully.");

            File.Copy(Path.Combine(_toolsZoneDir, _buildLang.ToString(), "mod.ff"), Path.Combine(_releaseDir, "mod.ff"), true);

            Console.WriteLine(" FastFile built successfully.");
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
            
            var sourceDirName = ".";
                
            if (!string.IsNullOrEmpty(sourceDir)) sourceDirName = new DirectoryInfo(sourceDir).Name;
            
            using var archive = ZipFile.Open(iwdPath, ZipArchiveMode.Update);

            var files = Directory.GetFiles(filesDir, searchPattern, SearchOption.AllDirectories);

            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                // Path in archive
                var relativePath = Path.GetRelativePath(filesDir, file);
                var entryName = Path.Combine(sourceDir, relativePath).Replace("\\", "/");

                // Remove existed file
                var existing = archive.GetEntry(entryName);
                existing?.Delete();

                // Add new file
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.Optimal);

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($" Packing ({i+1} / {files.Length}) from \"\\{sourceDirName}\"...");

                //Thread.Sleep(1);
            }
            
            Console.SetCursorPosition(0, Console.CursorTop);
            //Console.Write(new string(' ', Console.WindowWidth));
            //Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($" Packed ({files.Length}) files to \"\\{sourceDirName}\".");
        }

        private static void PrepareFastFile(string packDir, string packName)
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Move \"{packName}\" assets ...");
            
            CopyDirToRaw(packDir, "maps");
            CopyDirToRaw(packDir, "mp");
            CopyDirToRaw(packDir, "shock");
            CopyDirToRaw(packDir, "sound");
            CopyDirToRaw(packDir, "soundaliases");
            CopyDirToRaw(packDir, "ui_mp");
            CopyDirToRaw(packDir, "xmodel");
            CopyDirToRaw(packDir, "xmodelparts");
            CopyDirToRaw(packDir, "xmodelsurfs");
            CopyDirToRaw(packDir, "fx");
            CopyDirToRaw(packDir, "materials");
            CopyDirToRaw(packDir, "vision");
            CopyDirToRaw(packDir, "xanim");

            CopyDirToRaw(packDir, Path.Combine("locals", _buildLang.ToString(), _buildLang.ToString()));  

            CopyDirToRaw(packDir, "openwarfare");
            
            CopyDir.CopyDirectoryFlat(packDir, _toolsZoneSourceDir, _assetsListFileName, CopyDir.CopyMode.Merging);
            CopyDir.CopyDirectoryFlat(packDir, _toolsZoneSourceAssetsDir,  _ignoredAssetsListFileName, CopyDir.CopyMode.Merging);
            
            Console.WriteLine(" Assets moved successfully.");
        }

        private static void CopyDirToRaw(string packDir, string assetsSourceDir, string assetsTargetDir = "")
        {
            var assetsPath = Path.Combine(packDir, assetsSourceDir);

            if (!Directory.Exists(assetsPath))
            {
                return;
            }

            if (string.IsNullOrEmpty(assetsTargetDir))
            {
                assetsTargetDir = assetsSourceDir;
            }

            CopyDir.CopyDirectoryFlat(assetsPath, Path.Combine(_toolsRawDir, assetsTargetDir), "*");
        }

        private static void Shutdown(BuildResult result, string description = "")
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Building finished with result: \"{result.ToString()}\"{(string.IsNullOrEmpty(description) ? "" : $" ({description})")}.");
            Environment.Exit((int)result);
        }

        #region Packages

        private static string _packagesConfigName = "packages.txt";
        
        private static PackManifest[] _packagesList = Array.Empty<PackManifest>();

        private static string _packagesInitScriptName = "_packagesinit.gsc";
        
        public static void LoadPackages()
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Loading packages...");

            var packListFile = Path.Combine(_workDir, _packagesConfigName);

            if (!File.Exists(packListFile))
            {
                Shutdown(BuildResult.PackagesListNotFound, packListFile);
                return;
            }

            var configLines = File.ReadAllLines(packListFile);

            var loadedPackagesList = new List<PackManifest>();
            
            foreach (var configLine in configLines)
            {
                if (configLine.StartsWith("//")) continue;
                
                Console.WriteLine($" Loading package: \"{configLine}\"...");
                
                var packDir = Path.Combine(_workDir, $"_Package.{configLine}");
                var packConfigFile = Path.Combine(packDir, "package.txt");

                if (!Directory.Exists(packDir))
                {
                    Shutdown(BuildResult.PackageDirNotFound, packDir);
                    return;
                }

                if (!File.Exists(packConfigFile))
                {
                    Shutdown(BuildResult.PackageConfigNotFound, packConfigFile);
                    return;
                }

                var packConfig = File.ReadAllLines(packConfigFile);

                var packManifest = new PackManifest {PackDir = packDir};

                if (!TryGetPackConfigValue(packConfig, "name", out packManifest.PackName) ||
                    !TryGetPackConfigValue(packConfig, "iwd", out packManifest.IwdName) ||
                    !TryGetPackConfigValue(packConfig, "gsc", out packManifest.ScriptName ))
                {
                    Shutdown(BuildResult.PackageConfigWrongFormat);
                    return;
                }
                
                loadedPackagesList.Add(packManifest);
            }

            _packagesList = loadedPackagesList.ToArray();
            
            Console.WriteLine($" Loaded {_packagesList.Length} packages successfully.");

            MakePackagesInitScript();
        }

        private static void MakePackagesInitScript()
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Init packages code generation...");
            
            var initScriptPath = Path.Combine(_baseDir, "openwarfare", _packagesInitScriptName);
            var initScriptBuilder = new StringBuilder();

            initScriptBuilder.Append("//_packagesinit generated. Do not touch!!\n");
            initScriptBuilder.Append("init()\n");
            initScriptBuilder.Append("{\n");

            foreach (var packManifest in _packagesList)
            {
                initScriptBuilder.Append($"\tthread openwarfare\\{packManifest.ScriptName}::init();\n");
            }

            initScriptBuilder.Append("}\n");


            File.WriteAllText(initScriptPath, initScriptBuilder.ToString(), new UTF8Encoding(false));
            
            Console.WriteLine($" Init packages code generated successfully.");
        }

        private static bool TryGetPackConfigValue(string[] config, string key, out string value)
        {
            value = string.Empty;

            foreach (var configLine in config)
            {
                if (!configLine.Contains('=')) continue;

                var splitConfigLine = configLine.Split("=");

                if (key.Equals(splitConfigLine[0], StringComparison.InvariantCultureIgnoreCase))
                {
                    value = splitConfigLine[1];
                    return true;
                }
            }
            
            return false;
        }
        
        private static void BuildPackagesIwd()
        {
            foreach (var package in _packagesList)
            {
                BuildIwd(package.PackDir, package.IwdName, PackAssets);
            }
        }

        private static void PreparePackagesFastFile()
        {
            foreach (var packManifest in _packagesList)
            {
                PrepareFastFile(packManifest.PackDir, packManifest.PackName);
            }
        }

        #endregion
    }
    
    class CopyDir
    {
        public enum CopyMode
        {
            Overwrite = 1,
            DoNotOverwrite = 2,
            Merging = 3
        }
        
        public static void CopyDirectoryFlat(string sourceDir, string targetDir, string searchPattern, CopyMode copyMode = CopyMode.Overwrite)
        {
            // Получаем все файлы сразу, включая вложенные
            var files = Directory.GetFiles(sourceDir, searchPattern, SearchOption.AllDirectories);
            
            var sourceDirName = new DirectoryInfo(sourceDir).Name;
            var targetDirName = new DirectoryInfo(targetDir).Name;
            
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                // Получаем относительный путь относительно корневой папки
                var relativePath = Path.GetRelativePath(sourceDir, file);
                
                // Строим путь в папке назначения
                var targetPath = Path.Combine(targetDir, relativePath);

                // Создаём подкаталоги, если нужно
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($" Copying ({i+1} / {files.Length}) from \"\\{sourceDirName}\"...");
                
                // Копируем файл

                switch (copyMode)
                {
                    case CopyMode.Overwrite: 
                        File.Copy(file, targetPath, true);      
                        break;
                    
                    case CopyMode.DoNotOverwrite:
                        if (!File.Exists(targetPath)) File.Copy(file, targetPath, false);
                        break;
                    
                    case CopyMode.Merging:
                        MergeFile(file, targetPath);
                        break;
                }
            }
            
            Console.SetCursorPosition(0, Console.CursorTop);
            //Console.Write(new string(' ', Console.WindowWidth));
            //Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($" Copied ({files.Length}) files to \"\\{targetDirName}\".");
        }
        
        public static void MergeFile(string sourcePath, string targetPath)
        {
            // Just copy file if nothing to merge
            if (!File.Exists(targetPath))
            {
                File.Copy(sourcePath, targetPath);
                return;
            }
            
            var allLines = new HashSet<string>();
            var uniqueLines = new List<string>();

            // Read lines from target file
            foreach (var line in File.ReadLines(targetPath))
            {
                if (allLines.Add(line))
                    uniqueLines.Add(line);
            }

            uniqueLines.Add($"\n# -- merged from \"{sourcePath}\" -- #\n");
            
            // Read lines from source file
            foreach (var line in File.ReadLines(sourcePath))
            {
                if (allLines.Add(line))
                    uniqueLines.Add(line);
            }

            // Write all combined lines
            File.WriteAllLines(targetPath, uniqueLines);
        }
    }
    
    public struct PackManifest
    {
        public string PackName;
        public string PackDir;
        public string ScriptName;
        public string IwdName;
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

    public enum BuildResult
    {
        Successful = 0,
        WorkDirNotFound = 1,
        BaseDirNotFound = 2,
        ToolsDirNotFound = 3,
        IwdSourceDirNotFound = 4,
        LinkerFailed = 5,
        
        PackagesListNotFound = 6,
        PackageDirNotFound = 7,
        PackageConfigNotFound = 8,
        PackageConfigWrongFormat = 9
    }
}