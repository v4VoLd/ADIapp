using System;
using System.IO;

namespace ADIapp.Helpers;

/// <summary>
/// Centralized logger for the application.
/// Writes to the debug output stream and appends to app.log in the execution directory.
/// </summary>
public static class Logger
{
    private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.log");

    public static void Info(string message) => Log("INFO", message);
    public static void Error(string message, Exception? ex = null) => Log("ERROR", $"{message}{(ex != null ? $"\n{ex}" : "")}");
    public static void Debug(string message) => Log("DEBUG", message);

    private static void Log(string level, string message)
    {
        string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        System.Diagnostics.Debug.WriteLine(formattedMessage);
        
        try
        {
            File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine);
        }
        catch
        {
            // Fail silently to prevent logger crashes from disrupting execution
        }
    }
}
