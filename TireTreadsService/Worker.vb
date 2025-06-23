Imports Microsoft.Extensions.Hosting
Imports Microsoft.Extensions.Configuration
Imports System.Net.Http
Imports System.IO
Imports System.Diagnostics
Imports System.Reflection
Imports System.Text.Json
Imports System.Threading

Public Class Worker
    Inherits BackgroundService

    Private ReadOnly _logger As FileLogger
    Private ReadOnly _config As IConfiguration
    Private ReadOnly _exporter As InventoryExporter
    Private ReadOnly _uploader As SftpUploader
    Private ReadOnly _intervalMinutes As Integer

    Private Const ServiceName As String = "TireTreadsService"
    Private Const VersionUrl As String = "https://kayleklipboard.github.io/TireTreadsServiceUpdates/version.json"

    Public Sub New(logger As FileLogger, config As IConfiguration)
        _logger = logger
        _config = config

        _exporter = New InventoryExporter(config, logger)
        _uploader = New SftpUploader(config, logger)

        _intervalMinutes = config.GetValue(Of Integer)("Worker:IntervalMinutes", 1440)
        If _intervalMinutes <= 0 Then
            _intervalMinutes = 1440
            _logger.Warn("⚠️ Invalid interval found in config; defaulting to 1440 minutes (24 hours).")
        End If
    End Sub

    Protected Overrides Async Function ExecuteAsync(stoppingToken As CancellationToken) As Task
        _logger.Info($"🚀 TireTreadsService started. Will repeat every {_intervalMinutes} minutes.")

        While Not stoppingToken.IsCancellationRequested
            Try
                ' ⬆️ Check for update before export
                If Await CheckForUpdateAsync() Then
                    Return ' Exiting because service will be restarted by update script
                End If

                _logger.Info("📦 Starting export and upload cycle...")

                Dim file As String = Await _exporter.CreateExportAsync(stoppingToken)
                If Not String.IsNullOrEmpty(file) Then
                    Await _uploader.UploadAsync(file, stoppingToken)
                    _logger.Info("✅ Export/upload cycle completed.")
                Else
                    _logger.Warn("⚠️ No file was generated. Skipping upload.")
                End If
            Catch ex As Exception
                _logger.Error("❌ Export or upload process failed: " & ex.Message)
            End Try

            Try
                _logger.Info($"⏳ Waiting {_intervalMinutes} minute(s) until next run...")
                Await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken)
            Catch ex As TaskCanceledException
                Exit While
            End Try
        End While

        _logger.Info("🛑 Worker stopping gracefully.")
    End Function

    Private Async Function CheckForUpdateAsync() As Task(Of Boolean)
        Try
            Using client As New HttpClient()
                Dim json = Await client.GetStringAsync(VersionUrl)
                Dim doc = JsonDocument.Parse(json)
                Dim remoteVersion = doc.RootElement.GetProperty("version").GetString()
                Dim installerUrl = doc.RootElement.GetProperty("url").GetString()

                Dim currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()

                If IsNewerVersion(remoteVersion, currentVersion) Then
                    _logger.Info($"🆕 Update found: {remoteVersion} > {currentVersion}")
                    Await DownloadAndInstallUpdateAsync(installerUrl)
                    Return True
                End If
            End Using
        Catch ex As Exception
            _logger.Warn("⚠️ Failed to check for update: " & ex.Message)
        End Try

        Return False
    End Function

    Private Function IsNewerVersion(remote As String, local As String) As Boolean
        Try
            Dim remoteVer = New Version(remote)
            Dim localVer = New Version(local)
            Return remoteVer > localVer
        Catch
            Return False
        End Try
    End Function

    Private Async Function DownloadAndInstallUpdateAsync(url As String) As Task
        Try
            Dim exePath = Assembly.GetExecutingAssembly().Location
            Dim exeDir = Path.GetDirectoryName(exePath)
            Dim newFile = Path.Combine(exeDir, "TireTreadsService_new.exe")
            Dim batchFile = Path.Combine(exeDir, "update_service.bat")

            _logger.Info("⬇️ Downloading new version...")
            Using client As New HttpClient()
                Dim data = Await client.GetByteArrayAsync(url)
                File.WriteAllBytes(newFile, data)
            End Using

            _logger.Info("🧠 Preparing update script...")

            Dim batContents As String = $"
@echo off
timeout /t 5 /nobreak
sc stop {ServiceName}
timeout /t 5 /nobreak
copy /y ""{Path.GetFileName(newFile)}"" ""{Path.GetFileName(exePath)}""
del ""{Path.GetFileName(newFile)}""
sc start {ServiceName}
"

            File.WriteAllText(batchFile, batContents)

            _logger.Info("🔄 Launching update and exiting service...")
            Process.Start(New ProcessStartInfo With {
                .FileName = batchFile,
                .WorkingDirectory = exeDir,
                .CreateNoWindow = True,
                .WindowStyle = ProcessWindowStyle.Hidden
            })

            Environment.Exit(0)
        Catch ex As Exception
            _logger.Error("❌ Failed to download/apply update: " & ex.Message)
        End Try
    End Function
End Class
