Imports Microsoft.Extensions.Configuration
Imports Renci.SshNet
Imports System.IO
Imports System.Threading

Public Class SftpUploader
    Private ReadOnly _config As IConfiguration
    Private ReadOnly _logger As FileLogger

    Public Sub New(config As IConfiguration, logger As FileLogger)
        _config = config
        _logger = logger
    End Sub

    Public Async Function UploadAsync(filePath As String, token As CancellationToken) As Task
        Dim host = _config("Sftp:Host")
        Dim username = _config("Sftp:Username")
        Dim password = _config("Sftp:Password")
        Dim port = _config.GetValue(Of Integer)("Sftp:Port", 22)
        Dim uploadPath = If(_config("Sftp:UploadPath"), "/")
        Dim remoteFileName = If(_config("Export:RemoteFile"), Path.GetFileName(filePath))

        If String.IsNullOrWhiteSpace(filePath) OrElse Not File.Exists(filePath) Then
            _logger.Error("❌ File does not exist or path is empty: " & filePath)
            Throw New FileNotFoundException("File does not exist or path is empty.", filePath)
        End If

        Try
            Using client As New SftpClient(host, port, username, password)
                client.Connect()

                If Not client.IsConnected Then
                    Throw New IOException("❌ Could not connect to SFTP server.")
                End If

                Dim remotePath = Path.Combine(uploadPath.TrimEnd("/"c), remoteFileName).Replace("\", "/")

                Using fs As FileStream = File.OpenRead(filePath)
                    client.UploadFile(fs, remotePath)
                End Using

                client.Disconnect()
                _logger.Info($"✅ File uploaded to SFTP: {remotePath}")
            End Using
        Catch ex As Exception
            _logger.Error("❌ SFTP upload failed: " & ex.Message)
            Throw
        End Try

        Await Task.CompletedTask ' Maintain async signature
    End Function
End Class
