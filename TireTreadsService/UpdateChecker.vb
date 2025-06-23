Imports System.Net.Http
Imports System.IO
Imports System.Diagnostics
Imports System.Reflection
Imports System.Text.Json
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.Logging

Public Class UpdateChecker
    Private ReadOnly _config As IConfiguration
    Private ReadOnly _logger As ILogger

    Public Sub New(config As IConfiguration, logger As ILogger)
        _config = config
        _logger = logger
    End Sub

    Public Async Function CheckForUpdateAsync() As Task
        Try
            Dim updateUrl = _config("Updater:VersionManifestUrl")
            If String.IsNullOrWhiteSpace(updateUrl) Then
                _logger.LogInformation("🔁 Skipping update check: no URL configured.")
                Return
            End If

            Using client As New HttpClient()
                Dim json = Await client.GetStringAsync(updateUrl)
                Dim doc = JsonDocument.Parse(json)
                Dim remoteVersion = doc.RootElement.GetProperty("version").GetString()
                Dim installerUrl = doc.RootElement.GetProperty("url").GetString()

                Dim currentVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString()

                If remoteVersion > currentVersion Then
                    _logger.LogInformation($"⬆️ New version found: {remoteVersion} > {currentVersion}")

                    Dim tempInstaller = Path.Combine(Path.GetTempPath(), "TireTreadsServiceInstaller.exe")
                    Dim installerBytes = Await client.GetByteArrayAsync(installerUrl)
                    File.WriteAllBytes(tempInstaller, installerBytes)

                    _logger.LogInformation("✅ Installer downloaded. Running update...")

                    Dim psi As New ProcessStartInfo(tempInstaller)
                    psi.Arguments = "/silent"
                    psi.UseShellExecute = False
                    psi.CreateNoWindow = True
                    Process.Start(psi)

                    ' Optional: Stop the current service so installer can update
                    Environment.Exit(0)
                Else
                    _logger.LogInformation("🟢 No update needed.")
                End If
            End Using
        Catch ex As Exception
            _logger.LogError(ex, "❌ Failed to check for updates.")
        End Try
    End Function
End Class
