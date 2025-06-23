Imports Microsoft.Extensions.Configuration
Imports System.Data
Imports System.Data.SqlClient
Imports System.IO
Imports System.Text
Imports System.Threading

Public Class InventoryExporter
    Private ReadOnly _config As IConfiguration
    Private ReadOnly _logger As FileLogger

    Public Sub New(config As IConfiguration, logger As FileLogger)
        _config = config
        _logger = logger
    End Sub

    Public Async Function CreateExportAsync(token As CancellationToken) As Task(Of String)
        Dim connStr = _config.GetConnectionString("Sql")
        Dim exportDir = _config("Export:Directory")
        Dim remoteName = _config("Export:RemoteFileName")
        If String.IsNullOrEmpty(remoteName) Then remoteName = "Stock_19.csv"

        Dim companyIds = LoadCompanyIds(connStr)
        If companyIds Is Nothing OrElse companyIds.Count = 0 Then
            _logger.Warn("⚠️ No valid companies found for export.")
            Return String.Empty
        End If

        Dim manufacturers = _config.GetSection("Export:Manufacturers").Get(Of List(Of String))()
        If manufacturers Is Nothing OrElse manufacturers.Count = 0 Then
            _logger.Info("ℹ️ No manufacturer filter provided — loading all from DB.")
            manufacturers = LoadAllManufacturers(connStr)
        End If

        Directory.CreateDirectory(exportDir)
        Dim filePath = Path.Combine(exportDir, remoteName)

        Try
            Using conn As New SqlConnection(connStr),
                  cmd As New SqlCommand(SqlQueries.GetInventoryQuery(companyIds, manufacturers), conn)
                Await conn.OpenAsync(token)

                Dim table As New DataTable()
                Using adapter As New SqlDataAdapter(cmd)
                    adapter.Fill(table)
                End Using

                If table.Rows.Count = 0 Then
                    _logger.Info("No inventory data to export.")
                    Return String.Empty
                End If

                Using writer As New StreamWriter(filePath, False, Encoding.UTF8)
                    Await writer.WriteLineAsync("STORE_ID,BRAND_NAME,ITEM_CODE,STOCK,PRICE,FET")
                    For Each row As DataRow In table.Rows
                        Dim line = String.Join(","c, {
                            row("STORE_ID").ToString(),
                            row("BRAND_NAME").ToString(),
                            row("ITEM_CODE").ToString(),
                            String.Format("{0:0.##}", row("STOCK")),
                            String.Format("{0:0.####}", row("PRICE")),
                            String.Format("{0:0.####}", row("FET"))
                        })
                        Await writer.WriteLineAsync(line)
                    Next
                End Using

                _logger.Info($"📦 Exported {table.Rows.Count} inventory items to: {filePath}")
            End Using

            _config("Export:GeneratedFile") = filePath
            _config("Export:RemoteFile") = remoteName

            _logger.Info("✅ Export process completed successfully.")
            Return filePath

        Catch ex As Exception
            _logger.Error("❌ Failed to create export: " & ex.Message)
            Throw
        End Try
    End Function

    Private Function LoadCompanyIds(connStr As String) As List(Of String)
        ' 1. Try loading from config first
        Dim fromConfig = _config.GetSection("Export:IncludedCompanies").Get(Of List(Of String))()
        If fromConfig IsNot Nothing AndAlso fromConfig.Any() Then
            _logger.Info("📋 Loaded company list from config.")
            Return fromConfig
        End If

        ' 2. Else, load all valid shops from DB (excluding 9998)
        _logger.Info("📡 Loading all company numbers from database (excluding 9998)...")
        Dim companies As New List(Of String)

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand("SELECT COMPANY_NUMBER FROM COMPANY WHERE COMPANY_NUMBER <> '9998'", conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            companies.Add(reader("COMPANY_NUMBER").ToString())
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            _logger.Error("❌ Failed to load company IDs: " & ex.Message)
        End Try

        Return companies
    End Function

    Private Function LoadAllManufacturers(connStr As String) As List(Of String)
        Dim manufacturers As New List(Of String)

        Try
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand(SqlQueries.GetManufacturers, conn)
                    Using reader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim name As String = reader("DESCRIPTION").ToString().Trim()
                            If Not String.IsNullOrWhiteSpace(name) Then
                                manufacturers.Add(name)
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            _logger.Error("❌ Failed to load manufacturer descriptions: " & ex.Message)
        End Try

        Return manufacturers
    End Function
End Class
