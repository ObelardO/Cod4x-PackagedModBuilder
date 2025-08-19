using System.Diagnostics;
using System.IO.Compression;
using System.Text;

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
        private static bool _useScriptsPacking = true;

        private const string BaseIwdName = "main";
        private const string BaseSoundsIwdName = "sounds";

        private static string _assetsListFileName = "mod.csv";
        private static string _assetsListFilePath = string.Empty;
        
        private static string _ignoredAssetsListFileName = "mod_ignore.csv";
        private static string _ignoredAssetsListFilePath = string.Empty;
        
        
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

        private static readonly string[] ScriptAssetsTypes = {"gsc", "gsx"};

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
                        case "-packgsc": bool.TryParse(argValue, out _useScriptsPacking); break;
                    }
                }
            }

            _baseDir = Path.Combine(_workDir, "_Base");
            _releaseDir = Path.Combine(_workDir, "Release");
            
            _toolsRawDir = Path.Combine(_toolsDir, "raw");
            _toolsZoneDir = Path.Combine(_toolsDir, "zone");
            _toolsZoneSourceDir = Path.Combine(_toolsDir, "zone_source");
            _toolsBinDir = Path.Combine(_toolsDir, "bin");
            
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Starting...");
            Console.WriteLine($" Working directory: {_workDir}");
            Console.WriteLine($" ModTools directory: {_toolsDir}");
            Console.WriteLine($" Use scripts packing: {_useScriptsPacking}");
            
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
                _assetsListFilePath = Path.Combine(_toolsZoneSourceDir, _assetsListFileName);
                _ignoredAssetsListFilePath = Path.Combine(_toolsZoneSourceAssetsDir, _ignoredAssetsListFileName);
                
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

            if (File.Exists(_assetsListFilePath))
            {
                File.Delete(_assetsListFilePath);
            }
            
            if (File.Exists(_ignoredAssetsListFilePath))
            {
                File.Delete(_ignoredAssetsListFilePath);
            }
            
            if (Directory.Exists(_releaseDir))
            {
                Directory.Delete(_releaseDir,true);  
                Directory.CreateDirectory(_releaseDir);
            }

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
                    
                    LoadPackages();
                    
                    BuildPackagesIwd();
                    
                    PrepareFastFile(_baseDir, "Base");
                    PreparePackagesFastFile();
                    
                    BuildFastFile(_baseDir);
                    
                    break;
                
                case BuildMode.PackagesIwd:
                    
                    LoadPackages();
                    
                    BuildPackagesIwd();
                    
                    break;
                
                case BuildMode.PackagesFastFile:
                    
                    LoadPackages();
                    
                    PrepareFastFile(_baseDir, "Base");
                    PreparePackagesFastFile();
                    
                    BuildFastFile(_baseDir);
                    
                    break;
            }
            
            Console.WriteLine(" Building configs...");
            BuildConfig(_baseDir, "openwarfare");
            BuildPackagesConfig();
            
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

            //ExcludeScriptAssets();
            
            Console.WriteLine(" Starting linker...");
            
            var linker = new Process();
            linker.StartInfo.FileName = Path.Combine(_toolsBinDir, "linker_pc.exe");
            linker.StartInfo.Arguments = $"-language {_buildLang} -compress -cleanup mod";
            linker.StartInfo.WorkingDirectory = Path.Combine(_toolsBinDir);
            linker.StartInfo.UseShellExecute = false;
            linker.StartInfo.RedirectStandardOutput = true; 
            linker.Start();

            string output = linker.StandardOutput.ReadToEnd();
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
            Console.Write(new string(' ', 80));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($" Packed ({files.Length}) files to \"\\{sourceDirName}\".");
        }

        private static void PrepareFastFile(string packDir, string packName, bool usePackScriptsPacking = true)
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Move \"{packName}\" assets ...");
            
            var useScriptsPacking = _useScriptsPacking && usePackScriptsPacking;
            var scriptsTargetDir = useScriptsPacking ? _toolsRawDir : _releaseDir;
            
            Console.WriteLine($" Scripts packing is \"{useScriptsPacking}\"");
            
            // Move assets
            CopyAssets(packDir, "shock", _toolsRawDir);
            CopyAssets(packDir, "sound", _toolsRawDir);
            CopyAssets(packDir, "soundaliases", _toolsRawDir);
            CopyAssets(packDir, "ui_mp", _toolsRawDir);
            CopyAssets(packDir, "xmodel", _toolsRawDir);
            CopyAssets(packDir, "xmodelparts", _toolsRawDir);
            CopyAssets(packDir, "xmodelsurfs", _toolsRawDir);
            CopyAssets(packDir, "fx", _toolsRawDir);
            CopyAssets(packDir, "materials", _toolsRawDir);
            CopyAssets(packDir, "vision", _toolsRawDir);
            CopyAssets(packDir, "xanim", _toolsRawDir);
            CopyAssets(packDir, "mp", _toolsRawDir);
            CopyAssets(packDir, Path.Combine("locals", _buildLang.ToString()), _toolsRawDir,_buildLang.ToString());

            // Move scripts data directly to release directory
            CopyAssets(packDir, "scriptdata", _releaseDir);
            
            // Move scripts (directly to release if scripts packing disabled)
            CopyAssets(packDir, "maps", scriptsTargetDir);
            CopyAssets(packDir, "openwarfare", scriptsTargetDir);
            CopyAssets(packDir, "scripts", scriptsTargetDir);

            // Scripts assets list validator method
            Func<string[], string[]> scriptAssetsValidator = useScriptsPacking
                // return source list if script packing enabled
                ? assetsList => assetsList
                // or return list without scripts 
                : assetsList => AssetsListValidator(assetsList, ScriptAssetsTypes);
 
            // Copy assets list with merging (all packages and base combined) and validation (skip scripts if needed) 
            CopyDir.CopyDirectoryFlat(packDir, _toolsZoneSourceDir, _assetsListFileName, CopyDir.CopyMode.Merging, scriptAssetsValidator);
            CopyDir.CopyDirectoryFlat(packDir, _toolsZoneSourceAssetsDir,  _ignoredAssetsListFileName, CopyDir.CopyMode.Merging, scriptAssetsValidator);
            
            Console.WriteLine(" Assets moved successfully.");
        }

        private static string[] AssetsListValidator(string[] assetsList, string[] excludedAssetsTypes)
        {
            var validAssetsList = assetsList.ToArray();
            var validAssetsCount = validAssetsList.Length;
            
            foreach (var excludedType in excludedAssetsTypes)
            {
                validAssetsList = validAssetsList.Where(a => !a.TrimEnd(' ').ToLower().EndsWith(excludedType)).ToArray();
            }
            
            var skippedAssetsCount = validAssetsCount - validAssetsList.Length;

            if (skippedAssetsCount > 0)
            {
                Console.SetCursorPosition(50, Console.CursorTop);
                Console.Write($"Skipped ({skippedAssetsCount}) assets.");
            }

            return validAssetsList;
        }

        private static void CopyAssets(string packDir, string assetsSourceDir, string targetDir, string assetsTargetDir = "")
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

            CopyDir.CopyDirectoryFlat(assetsPath, Path.Combine(targetDir, assetsTargetDir), "*");
        }

        private static void BuildConfig(string packDir, string configName)
        {
            var configFileName = $"{configName}.cfg";

            var configFilePath = Path.Combine(packDir, "configs", configFileName);

            if (!File.Exists(configFilePath))
            {
                Console.WriteLine($" WARNING! Can't find config {configFilePath}");
                return;
            }
            
            // Move configs directly to release directory
            CopyAssets(packDir, "configs", _releaseDir);
            
            File.AppendAllLines(Path.Combine(_releaseDir, "autoexec.cfg"), new []
            {
                $"exec configs\\{configFileName}"
            });
        }

        private static void BuildPackagesConfig()
        {
            foreach (var packManifest in _packagesList.Where(pack => !string.IsNullOrEmpty(pack.ConfigName)))
            {
                BuildConfig(packManifest.PackDir, packManifest.ConfigName);
            }
        }
        
        private static void Shutdown(BuildResult result, string description = "")
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Building finished with result: \"{result.ToString()}\"{(string.IsNullOrEmpty(description) ? "" : $" ({description})")}.");
            Environment.Exit((int)result);
        }

        #region Packages

        private const string PackagesConfigName = "packages.txt";
        private const string PackagesInitScriptName = "_packagesinit.gsc";
        
        private static PackManifest[] _packagesList = Array.Empty<PackManifest>();
        
        public static void LoadPackages()
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Loading packages...");

            var packListFile = Path.Combine(_workDir, PackagesConfigName);

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

                var packManifest = new PackManifest
                {
                    PackDir = packDir,
                    UseScriptsPacking = true
                };

                if (!TryGetPackConfigValue(packConfig, "name", out packManifest.PackName) ||
                    !TryGetPackConfigValue(packConfig, "iwd", out packManifest.IwdName))
                {
                    Shutdown(BuildResult.PackageConfigWrongFormat);
                    return;
                }

                if (TryGetPackConfigValue(packConfig, "packgsc", out var useScriptsPackingValue))
                {
                    bool.TryParse(useScriptsPackingValue, out packManifest.UseScriptsPacking);
                }

                TryGetPackConfigValue(packConfig, "gsc", out packManifest.ScriptName);
                TryGetPackConfigValue(packConfig, "cfg", out packManifest.ConfigName);

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
            
            var initScriptPath = Path.Combine(_baseDir, "openwarfare", PackagesInitScriptName);
            var initScriptBuilder = new StringBuilder();

            initScriptBuilder.Append("//_packagesinit generated. Do not touch!!\n");
            initScriptBuilder.Append("init()\n");
            initScriptBuilder.Append("{\n");

            foreach (var packManifest in _packagesList.Where(pack => !string.IsNullOrEmpty(pack.ScriptName)))
            {
                initScriptBuilder.Append($"\tthread scripts\\{packManifest.ScriptName}::init();\n");
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
                PrepareFastFile(packManifest.PackDir, packManifest.PackName, packManifest.UseScriptsPacking);
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
        
        public static void CopyDirectoryFlat(string sourceDir, string targetDir, string searchPattern, CopyMode copyMode = CopyMode.Overwrite, Func<string[], string[]>? mergeValidator = null)
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
                        MergeFile(file, targetPath, mergeValidator);
                        break;
                }
            }
            
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', 50));
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($" Copied ({files.Length}) files to \"\\{targetDirName}\".");
        }
        
        public static void MergeFile(string sourcePath, string targetPath, Func<string[], string[]>? mergeValidator)
        {
            // If there is not validation function
            mergeValidator ??= _ => _;
            
            var allLines = new HashSet<string>();
            var uniqueLines = new List<string>();

            if (File.Exists(targetPath))
            {
                // Read lines from target file
                foreach (var line in File.ReadLines(targetPath))
                {
                    if (allLines.Add(line))
                        uniqueLines.Add(line);
                }

                uniqueLines.Add($"\n# -- merged from \"{sourcePath}\" -- #\n");
            }

            // Read lines from source file
            foreach (var line in mergeValidator(File.ReadLines(sourcePath).ToArray()))
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
        public string ConfigName;

        public bool UseScriptsPacking;
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