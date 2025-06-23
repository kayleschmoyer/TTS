Imports System.IO

Public Class FileLogger
    Private ReadOnly logPath As String

    Public Sub New()
        Dim logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TireTreadsService", "Logs")
        If Not Directory.Exists(logDir) Then
            Directory.CreateDirectory(logDir)
        End If

        Dim logFileName = $"TireTreadsService_{DateTime.Today:yyyy-MM-dd}.log"
        logPath = Path.Combine(logDir, logFileName)
    End Sub

    Public Sub Info(message As String)
        WriteLog("INFO", message)
    End Sub

    Public Sub Warn(message As String)
        WriteLog("WARN", message)
    End Sub

    Public Sub [Error](message As String)
        WriteLog("ERROR", message)
    End Sub

    Private Sub WriteLog(level As String, message As String)
        Dim fullMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}{Environment.NewLine}"
        Try
            File.AppendAllText(logPath, fullMessage)
        Catch
            ' Optional: fallback to console, or ignore
        End Try
    End Sub
End Class
