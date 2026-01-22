using Xdg.Directories;
using Tomlyn;

public class AppConfig 
{
  public string Path       { get; set; } = null!;
  public string Build      { get; set; } = null!;
  public string Update     { get; set; } = null!;
  public string Export     { get; set; } = null!;
  public string BinaryPath { get; set; } = null!;
  public string InstallCmd { get; set; } = null!;
}

public class Config {
  public Dictionary<string, AppConfig> Program { get; set; } = new(); // init empty to avoid null
}

public class LaunchConfig {
  public bool std_show = false;
  public bool verbose = false;
  public string config_path = "";

  public LaunchConfig(string[] args) {
    ArgParser parser = new ArgParser(this, args);
  }
}

public class ArgParser {
  public ArgParser(LaunchConfig config, string[] args) {
    for (int i = 0; i < args.Length; i++){
      switch (args[i]) {
        case "--std":
          config.std_show = true;
          break;
        case "--show-std":
          config.std_show = true;
          break;
        case "--verbose":
          config.verbose = true;
          break;
        case "--config_path":
          config.config_path = args[++i];
          break;
        case "--help":
          Console.WriteLine("usage: gitter [OPTIONS]\n--std, --show-std\n----> Shows the output from the compile/update command directly from compiler/git/wget...");
          Environment.Exit(0);
          break;
        default:
          break;
      }
    }
  }
}

public class FilePathArranger 
{
  public string ConfigPath { get; private set; }
  public string HomePath { get; private set; }

  public FilePathArranger()
  {
    HomePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    string path = BaseDirectory.ConfigHome;
    path = Path.Join(path, "gitter");

    if (!Directory.Exists(path)) {
      Directory.CreateDirectory(path);
    }

    path = Path.Join(path, "config.toml");

    if (!File.Exists(path)) {
      FileStream fs = File.Create(path); 
      fs.Dispose();
    }

    ConfigPath = path;
  }
}


public class ConfigReader
{
  private FilePathArranger Paths = new FilePathArranger();
  public Config Read;

  public ConfigReader()
  {
    var configContent = File.ReadAllText(Paths.ConfigPath);
    Read = Toml.ToModel<Config>(configContent);
  }
}

public class Runner
{

  protected static (int errCode, string ErrMsg) RunCmd(string cmd, string workingDir, LaunchConfig config) // We return a tuple rather than simply printing the error in case user doesn't care (configurable in the future)
  {
    System.Diagnostics.Process process = new System.Diagnostics.Process();

    process.StartInfo.FileName = "/bin/bash";
    process.StartInfo.Arguments = $"-c \"{cmd}\"";

    process.StartInfo.RedirectStandardError = true;
    process.StartInfo.RedirectStandardOutput = true;

    process.StartInfo.WorkingDirectory = workingDir;
    process.Start();

    string output = process.StandardOutput.ReadToEnd();
    string err = process.StandardError.ReadToEnd();

    process.WaitForExit();

    if(config.std_show)
    {
      Console.Write(output);
      Console.Error.Write(err);
    }

    if (process.ExitCode != 0)
    {
      Console.WriteLine("\n");
      Console.WriteLine("--------------------!!!--------------------");
      return ( process.ExitCode, err );
    }
    else
    {
      return ( process.ExitCode, "" );
    }
  }

  protected static void MoveFile(string path, string target) 
  {
    File.Move(path, target);
  }


  protected static void LinkFile(string binaryPath, string exportDir)
  {
      string linkPath = Path.Join(exportDir, Path.GetFileName(binaryPath));

      if (File.Exists(linkPath) || Directory.Exists(linkPath))
      {
          Console.WriteLine($"Symlink already exists at: {linkPath}");
          return;
      }

      File.CreateSymbolicLink(linkPath, binaryPath);
      Console.WriteLine($"Linked {binaryPath} -> {linkPath}");
  }


  protected static string ReplaceTilda(string path, string homePath)
  {
    if (path.StartsWith("~"))
    {
      path = path.Replace("~", homePath);
      return path;
    }

    return path;
  }

  static int Main(string[] args)
  {
    var config = new ConfigReader(); 
    var paths = new FilePathArranger();

    Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

    LaunchConfig launchConfig = new LaunchConfig(args);

    if (config.Read.Program.Count == 0)
    {
      Console.WriteLine("The Configuration File Is Empty!");
      return 0;
    }

    foreach ( var (app, key) in config.Read.Program) {
      Console.WriteLine($"App       : {app}");
      Console.WriteLine($"Path      : {key.Path}");
      Console.WriteLine($"Build     : {key.Build}");
      Console.WriteLine($"Update    : {key.Update}");
      Console.WriteLine($"Export    : {key.Export}");
      Console.WriteLine($"BinaryPath: {key.BinaryPath}");
      Console.WriteLine($"InstallCmd: {key.InstallCmd}");

      var cleanPath       = ReplaceTilda(key.Path, paths.HomePath);
      var cleanExportPath = ReplaceTilda(key.Export, paths.HomePath);
      if (key.Export == "") {
        cleanExportPath = "";
      }
      var cleanBinaryPath = ReplaceTilda(key.BinaryPath, paths.HomePath);

      var (updateReturn, UpdateErr) = RunCmd(key.Update, cleanPath, launchConfig);

      if (updateReturn == 0){
        Console.WriteLine($"Updated: {app}");
      }
      else if (!launchConfig.std_show){
        Console.WriteLine($"There Was A Problem Updating: {app}\n{UpdateErr}");
        Console.WriteLine("--------------------!!!--------------------");
      }

      var (buildReturn, buildErr) = RunCmd(key.Build, cleanPath, launchConfig);

      if (buildReturn == 0) {
        Console.WriteLine($"Built: {app}");
      }
      else if (!launchConfig.std_show) {
        Console.WriteLine($"There Was A Problem Building: {app}\n{buildErr}");
        Console.WriteLine("--------------------!!!--------------------");
      }

      if (cleanExportPath != "") {
        LinkFile(cleanBinaryPath, cleanExportPath);
      }
      else {
        Console.WriteLine("Provided Export Path Is Empty, Skipping Symlink");
      }

      Console.WriteLine("\n");
    }

    return 0;
  }
}
