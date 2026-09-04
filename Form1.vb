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
End Class
