using System;
using System.Diagnostics;
using System.IO;
using System.IO;
using System.IO.Compression;
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
        
        private static string _baseDir = string.Empty;
        private static string _releaseDir = string.Empty;

        private static BuildMode _buildMode;
        private static BuildLang _buildLang;

        private const string BaseIwdName = "z_ow_main";
        private const string BaseSoundsIwdName = "z_ow_sounds";

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

            SelectMode();

            Console.ReadLine();

            Shutdown(BuildResult.Successful);
            return;
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

            switch (_buildMode)
            {
                case BuildMode.All:

                    BuildIwd(_baseDir, BaseIwdName, BaseAssets);
                    BuildIwd(_baseDir, BaseSoundsIwdName, BaseSoundsAssets);

                    PrepareFastFile(_baseDir);

                    BuildFastFile(_baseDir);

                    break;
                
                case BuildMode.BaseFastFile:
                    
                    PrepareFastFile(_baseDir);

                    BuildFastFile(_baseDir);

                    break;
            }
            
            Console.WriteLine(" Mod built successfully.");
            Shutdown(BuildResult.Successful);
        }

        private static void BuildIwd(string dir, string iwdName, (string subDir, string filePattern)[] assets)
        {
            Console.WriteLine($" Building {iwdName}.iwd...");
            
            var iwdPath = Path.Combine(_releaseDir, $"{iwdName}.iwd");

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
            
            Console.WriteLine($" {iwdName}.iwd packed successfully.");
        }

        private static void BuildFastFile(string packDir)
        {
            Console.WriteLine($" Building mod.ff...");
            
            CopyDir.CopyDirectoryFlat(packDir, _toolsZoneSourceDir, "mod.csv", true);
            CopyDir.CopyDirectoryFlat(packDir, Path.Combine(_toolsZoneSourceDir, _buildLang.ToString(), "assetlist"),  "mod_ignore.csv", true);

            Console.WriteLine(" Starting linker...");
                
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
            Console.WriteLine($" Packed ({files.Length}) files to \"\\{sourceDirName}\".    ");
        }

        private static void PrepareFastFile(string packDir)
        {
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

            CopyDir.CopyDirectoryFlat(assetsPath, Path.Combine(_toolsRawDir, assetsTargetDir), "*", true);
        }
        
        private static void Shutdown(BuildResult result)
        {
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine($" Building finished with result: \"{result.ToString()}\".");
            Environment.Exit((int)result);
        }
    }



    class CopyDir
    {
        public static void CopyDirectoryFlat(string sourceDir, string targetDir, string searchPattern, bool overwrite)
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
                File.Copy(file, targetPath, overwrite);
            }
            
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.WriteLine($" Copied ({files.Length}) files to \"\\{targetDirName}\".        ");
        }
        
        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            
            
            
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                var targetPath = Path.Combine(target.FullName, fi.Name);
                fi.CopyTo(targetPath, true);
                Console.WriteLine(@"[OK] Copying {0} > {1}", fi.Name, targetPath);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
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

    public enum BuildResult
    {
        Successful = 0,
        WorkDirNotFound = 1,
        BaseDirNotFound = 2,
        ToolsDirNotFound = 3,
        IwdSourceDirNotFound = 4,
        LinkerFailed = 5
    }
}