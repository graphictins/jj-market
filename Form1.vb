Imports System.IO
Imports Microsoft.Web.WebView2.Core

Public Class Form1

#Region "Logic"

    Private rng As New Random
    Private jjcoinPrice As Double = 100.0
    Private prices As New List(Of Double)

    Private targetPrice As Double = 100.0
    Private noiseStrength As Double = 1.5
    Private pullStrength As Double = 0.03

    Private Sub Seed()
        For i = 1 To 60
            NextTick()
        Next
    End Sub

    Private Sub NextTick()
        Dim pull = (targetPrice - jjcoinPrice) * pullStrength
        jjcoinPrice += pull + (rng.NextDouble() - 0.5) * 2 * noiseStrength
        prices.Add(jjcoinPrice)
        If prices.Count > 100 Then prices.RemoveAt(0)
    End Sub

    Private Function FormatWithCommas(n As Double) As String
        Return n.ToString("N0", Global.System.Globalization.CultureInfo.InvariantCulture)
    End Function

    ' --- html button binding (output-end only: direct webView call is fine) ---
    Public Sub HandleAction(action As String)
        If action = "greet" Then
            webView.CoreWebView2.ExecuteScriptAsync("showResult('Hello from VB!')")
        ElseIf action = "buy" OrElse action = "sell" Then
            Trade(action = "buy")
        End If
    End Sub

    Private Sub Trade(isBuy As Boolean)
        Using dlg As New TradeForm(isBuy, Function() jjcoinPrice)
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Dim quantity = dlg.Quantity
                Dim quantityText = quantity.ToString("0.##", Global.System.Globalization.CultureInfo.InvariantCulture)

                If isBuy Then
                    Dim cost = quantity * jjcoinPrice
                    If cost > save.Cash Then
                        MessageBox.Show($"Not enough cash. You need {FormatWithCommas(cost)}.")
                        Return
                    End If
                    save.Cash -= cost
                    save.JJCoin += quantity
                    webView.CoreWebView2.ExecuteScriptAsync($"showResult('Bought {quantityText} JJCoin')")
                Else
                    If quantity > save.JJCoin Then
                        MessageBox.Show("Not enough jjcoin.")
                        Return
                    End If
                    save.JJCoin -= quantity
                    save.Cash += quantity * jjcoinPrice
                    webView.CoreWebView2.ExecuteScriptAsync($"showResult('Sold {quantityText} JJCoin')")
                End If

                PushPortfolio()
            End If
        End Using
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
        PushPortfolio()
    End Sub

    Private Sub PushPortfolio()
        Dim jjcoinValue = save.JJCoin * jjcoinPrice
        webView.CoreWebView2.ExecuteScriptAsync(
            $"setPortfolio('{FormatWithCommas(save.Cash)}', '{FormatWithCommas(jjcoinValue)}', '{FormatWithCommas(save.JJCoin)}')")
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