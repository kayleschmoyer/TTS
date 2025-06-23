Imports System.IO
Imports Microsoft.Extensions.Configuration
Imports Microsoft.Extensions.DependencyInjection
Imports Microsoft.Extensions.Hosting

Public Class Program
    Public Shared Sub Main()
        CreateHostBuilder(Environment.GetCommandLineArgs()).Build().Run()
    End Sub

    Public Shared Function CreateHostBuilder(args As String()) As IHostBuilder
        Return Host.CreateDefaultBuilder(args) _
            .ConfigureAppConfiguration(Sub(context, config)
                                           config.AddJsonFile("appsettings.json", optional:=False, reloadOnChange:=True)
                                           config.AddEnvironmentVariables()
                                       End Sub) _
            .ConfigureServices(Sub(services)
                                   services.AddSingleton(Of FileLogger)()
                                   services.AddHostedService(Of Worker)()
                               End Sub) _
            .UseWindowsService()
    End Function
End Class
