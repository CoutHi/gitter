using Xdg.Directories;
using Tomlyn;

public class AppConfig 
{
  public string Path       { get; set; } = null!;
  public string Build      { get; set; } = null!;
  public string Update     { get; set; } = null!;
  public string Export     { get; set; } = null!;
  public string BinaryPath { get; set; } = null!;
}

public class Config {
  public Dictionary<string, AppConfig> Program { get; set; } = new(); // init empty to avoid null
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

  protected static (int errCode, string ErrMsg) RunBuildCommand(string cmd, string workingDir) // We return a tuple rather than simply printing the error in case user doesn't care (configurable in the future)
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

  protected static (int ErrCode, string ErrMsg) RunUpdate(string cmd, string workingDir) 
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
    if (path.Contains("~"))
    {
      path = path.Replace("~", homePath);
      return path;
    }

    return path;
  }

  static int Main()
  {
    var config = new ConfigReader(); 
    var paths = new FilePathArranger();

    Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

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

      var cleanPath       = ReplaceTilda(key.Path, paths.HomePath);
      var cleanExportPath = ReplaceTilda(key.Export, paths.HomePath);
      var cleanBinaryPath = ReplaceTilda(key.BinaryPath, paths.HomePath);

      var (updateReturn, UpdateErr) = RunUpdate(key.Update, cleanPath);
      if (updateReturn == 0)
      {
        Console.WriteLine($"Updated: {app}");
      }
      else
      {
        Console.WriteLine($"There Was A Problem Updating: {app}\n{UpdateErr}");
        Console.WriteLine("--------------------!!!--------------------");
      }

      var (buildReturn, buildErr) = RunBuildCommand(key.Build, cleanPath);
      if (buildReturn == 0)
      {
        Console.WriteLine($"Built: {app}");
      }
      else
      {
        Console.WriteLine($"There Was A Problem Building: {app}\n{buildErr}");
        Console.WriteLine("--------------------!!!--------------------");
      }

      LinkFile(cleanBinaryPath, cleanExportPath);
      Console.WriteLine("\n");
    }

    return 0;
  }
}
