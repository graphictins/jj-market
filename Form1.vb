Imports System.IO
Imports Microsoft.Web.WebView2.Core

Public Class Form1
    Private WithEvents webView As New Microsoft.Web.WebView2.WinForms.WebView2 With {.Dock = DockStyle.Fill}
    Private WithEvents ChartTimer As New Timer With {.Interval = 500}

    Private rng As New Random
    Private price As Double = 100.0
    Private prices As New List(Of Double)

    Public Sub New()
        InitializeComponent()
        Controls.Add(webView)

        For i = 1 To 60
            price += (rng.NextDouble() - 0.5) * 2
            prices.Add(price)
        Next
    End Sub

    Private Sub ChartTimer_Tick(sender As Object, e As EventArgs) Handles ChartTimer.Tick
        price += (rng.NextDouble() - 0.5) * 2
        prices.Add(price)
        If prices.Count > 100 Then prices.RemoveAt(0)
        webView.CoreWebView2.ExecuteScriptAsync($"drawData([{String.Join(",", prices)}])")
    End Sub

    ' the html "Say Hello" button comes through here
    Private Sub OnWebMessage(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim action = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson) _
                     .RootElement.GetProperty("action").GetString()
        If action = "greet" Then
            webView.CoreWebView2.ExecuteScriptAsync("showResult('Hello from VB!')")
        End If
    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        InitWebView()
    End Sub

    Private Async Sub InitWebView()
        Await webView.EnsureCoreWebView2Async(Nothing)
        AddHandler webView.CoreWebView2.WebMessageReceived, AddressOf OnWebMessage
        With webView.CoreWebView2
            .SetVirtualHostNameToFolderMapping("app.local",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"),
                CoreWebView2HostResourceAccessKind.Allow)
            .Navigate("https://app.local/index.html")
        End With
        ChartTimer.Start()
    End Sub
End Class