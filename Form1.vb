Imports System.IO
Imports Microsoft.Web.WebView2.Core

Public Class Form1
    Private WithEvents webView As New Microsoft.Web.WebView2.WinForms.WebView2 With {.Dock = DockStyle.Fill}
    Private ReadOnly routes As New Dictionary(Of String, Action(Of String))

    Public Sub New()
        InitializeComponent()
        Controls.Add(webView)

        ' ── Register actions (add new actions here) ──
        routes("greet") = Sub(data)
            CallJS($"showResult('Hello from VB!')")
        End Sub

        InitWebView()
    End Sub

    Private Async Sub InitWebView()
        Await webView.EnsureCoreWebView2Async(Nothing)
        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "app.local",
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web"),
            CoreWebView2HostResourceAccessKind.Allow)
        AddHandler webView.CoreWebView2.WebMessageReceived, AddressOf OnMsg
        webView.CoreWebView2.Navigate("https://app.local/index.html")

        StartJJCoinGraph()
    End Sub

    Private Sub OnMsg(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim root = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson).RootElement
        Dim action = root.GetProperty("action").GetString()

        If routes.ContainsKey(action) Then
            Dim data = ""
            If root.TryGetProperty("data", Nothing) Then
                data = root.GetProperty("data").GetString()
            End If
            routes(action).Invoke(data)
        End If
    End Sub

    Private Sub CallJS(script As String)
        webView.CoreWebView2.ExecuteScriptAsync(script)
    End Sub

    ' ── JJCOIN GRAPH ──────────────────────────────────────
    Private WithEvents jjGraphTimer As New System.Windows.Forms.Timer With {.Interval = 1000}
    Private lastPrice As Decimal = 100.0D

    Private Sub StartJJCoinGraph()
        jjGraphTimer.Start()
    End Sub

    Private Sub jjGraphTimer_Tick(sender As Object, e As EventArgs) Handles jjGraphTimer.Tick
        Static rng As New Random()

        Dim drift As Decimal = (CDec(rng.NextDouble()) - 0.48D) * 4.0D
        Dim openPrice As Decimal = lastPrice
        Dim closePrice As Decimal = openPrice + drift
        Dim highPrice As Decimal = Math.Max(openPrice, closePrice) + CDec(rng.NextDouble()) * 2.0D
        Dim lowPrice As Decimal = Math.Min(openPrice, closePrice) - CDec(rng.NextDouble()) * 2.0D

        CallJS($"pushJJCoinBar({openPrice}, {highPrice}, {lowPrice}, {closePrice})")
        lastPrice = closePrice
    End Sub
    ' ── /JJCOIN GRAPH ─────────────────────────────────────
End Class
