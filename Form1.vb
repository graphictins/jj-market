Imports System.IO
Imports Microsoft.Web.WebView2.Core

Public Class Form1

#Region "Logic"

    Private rng As New Random
    Private jjcoinPrice As Double = 100.0
    Private prices As New List(Of Double)

    Private Sub Seed()
        For i = 1 To 60
            NextTick()
        Next
    End Sub

    Private Sub NextTick()
        jjcoinPrice += (rng.NextDouble() - 0.5) * 2
        prices.Add(jjcoinPrice)
        If prices.Count > 100 Then prices.RemoveAt(0)
    End Sub

    ' --- html button binding (output-end only: direct webView call is fine) ---
    Public Sub HandleAction(action As String)
        If action = "greet" Then
            webView.CoreWebView2.ExecuteScriptAsync("showResult('Hello from VB!')")
        End If
    End Sub

#End Region

#Region "Driver"

    Private WithEvents webView As New Microsoft.Web.WebView2.WinForms.WebView2 With {.Dock = DockStyle.Fill}
    Private WithEvents ChartTimer As New Timer With {.Interval = 500}
    Private save As SaveData = GameData.Load()

    Public Sub New()
        InitializeComponent()
        Controls.Add(webView)

        If save.Prices IsNot Nothing AndAlso save.Prices.Count > 0 Then
            prices = save.Prices
            jjcoinPrice = prices(prices.Count - 1)
        Else
            Seed()
        End If
    End Sub

    Private Sub ChartTimer_Tick(sender As Object, e As EventArgs) Handles ChartTimer.Tick
        NextTick()
        webView.CoreWebView2.ExecuteScriptAsync($"drawData([{String.Join(",", prices)}])")
        webView.CoreWebView2.ExecuteScriptAsync($"setPrice({jjcoinPrice.ToString(System.Globalization.CultureInfo.InvariantCulture)})")
    End Sub

    ' the html "Say Hello" button comes through here (receive-only)
    Private Sub OnWebMessage(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim action = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson) _
                     .RootElement.GetProperty("action").GetString()
        HandleAction(action)
    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        InitWebView()
    End Sub

    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        save.Prices = prices
        GameData.Save(save)
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

#End Region

End Class