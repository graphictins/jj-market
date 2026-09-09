Imports System.IO
Imports System.Linq
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
        For i = 1 To 12000
            NextTick()
        Next
    End Sub

    Private Sub NextTick()
        Dim pull = (targetPrice - jjcoinPrice) * pullStrength
        jjcoinPrice += pull + (rng.NextDouble() - 0.5) * 2 * noiseStrength + NewsDrift()
        prices.Add(jjcoinPrice)
        If prices.Count > 13000 Then prices.RemoveAt(0)
    End Sub

    Private Function FormatWithCommas(n As Double) As String
        Return n.ToString("N0", Global.System.Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Function FmtPrice(n As Double) As String
        Return n.ToString("N2", Global.System.Globalization.CultureInfo.InvariantCulture)
    End Function

    Private Function FmtQty(n As Double) As String
        Return n.ToString("0.##", Global.System.Globalization.CultureInfo.InvariantCulture)
    End Function

    ' Receive-only endpoint for html buttons.
    Public Sub HandleAction(action As String, qty As Double, limit As Double?)
        Select Case action
            Case "bootstrap"
                PushHistory()
                PushPortfolio()
                PushTrades()
                PushOrders()
                PushNews()
            Case "greet"
                webView.CoreWebView2.ExecuteScriptAsync("showResult('Hello from VB!')")
            Case "buy", "sell"
                Dim isBuy = action = "buy"
                If qty > 0 AndAlso limit.HasValue Then
                    PlaceLimitOrder(isBuy, qty, limit.Value)
                ElseIf qty > 0 Then
                    Dim sideTxt = If(isBuy, "Buy", "Sell")
                    If ExecuteOrder(isBuy, qty, jjcoinPrice) Then
                        webView.CoreWebView2.ExecuteScriptAsync(
                            $"showResult('OK: Market {sideTxt} filled {FmtQty(qty)} @ {FmtPrice(jjcoinPrice)}')")
                    Else
                        webView.CoreWebView2.ExecuteScriptAsync(
                            $"showResult('ERR: Not enough {(If(isBuy, "cash", "jjcoin"))}')")
                    End If
                Else
                    Trade(isBuy)
                End If
        End Select
    End Sub

    ' Shared execution path: funds check, account update, history record, push.
    Private Function ExecuteOrder(isBuy As Boolean, quantity As Double, price As Double) As Boolean
        If isBuy Then
            Dim cost = quantity * price
            If cost > save.Cash Then Return False
            save.Cash -= cost
            save.JJCoin += quantity
        Else
            If quantity > save.JJCoin Then Return False
            save.JJCoin -= quantity
            save.Cash += quantity * price
        End If

        save.TradeHistory.Insert(0, New TradeRecord With {
            .Time = DateTime.Now.ToString("MM-dd HH:mm:ss"),
            .Side = If(isBuy, "buy", "sell"),
            .Qty = quantity,
            .Price = price,
            .Total = quantity * price
        })
        If save.TradeHistory.Count > 200 Then save.TradeHistory.RemoveAt(save.TradeHistory.Count - 1)

        PushPortfolio()
        PushTrades()
        Return True
    End Function

    Private Sub PlaceLimitOrder(isBuy As Boolean, quantity As Double, limitPrice As Double)
        save.OpenOrders.Add(New PendingOrder With {
            .Id = save.NextOrderId,
            .IsBuy = isBuy,
            .Quantity = quantity,
            .LimitPrice = limitPrice
        })
        save.NextOrderId += 1
        PushOrders()
        Dim sideTxt = If(isBuy, "Buy", "Sell")
        webView.CoreWebView2.ExecuteScriptAsync($"showResult('OK: {sideTxt} limit @ {FmtPrice(limitPrice)} placed')")
    End Sub

    Private Sub CancelOrder(orderId As Long)
        Dim target = save.OpenOrders.FirstOrDefault(Function(o) o.Id = orderId)
        If target IsNot Nothing Then
            save.OpenOrders.Remove(target)
            PushOrders()
            webView.CoreWebView2.ExecuteScriptAsync($"showResult('OK: Order #{orderId} cancelled')")
        End If
    End Sub

    Private Sub CheckPendingOrders()
        Dim removed As Boolean = False
        For Each o In save.OpenOrders.ToList()
            Dim fill = (o.IsBuy AndAlso jjcoinPrice <= o.LimitPrice) OrElse
                       ((Not o.IsBuy) AndAlso jjcoinPrice >= o.LimitPrice)
            If fill Then
                save.OpenOrders.Remove(o)
                removed = True
                Dim sideTxt = If(o.IsBuy, "Buy", "Sell")
                If ExecuteOrder(o.IsBuy, o.Quantity, o.LimitPrice) Then
                    webView.CoreWebView2.ExecuteScriptAsync(
                        $"showResult('OK: {sideTxt} limit filled {FmtQty(o.Quantity)} @ {FmtPrice(o.LimitPrice)}')")
                End If
            End If
        Next
        If removed Then PushOrders()
    End Sub

    ' Legacy path: quantity entered via WinForms dialog.
    Private Sub Trade(isBuy As Boolean)
        Using dlg As New TradeForm(isBuy, Function() jjcoinPrice)
            If dlg.ShowDialog(Me) = DialogResult.OK Then
                Dim sideTxt = If(isBuy, "Buy", "Sell")
                If ExecuteOrder(isBuy, dlg.Quantity, jjcoinPrice) Then
                    webView.CoreWebView2.ExecuteScriptAsync($"showResult('OK: {sideTxt} {FmtQty(dlg.Quantity)} JJCOIN')")
                Else
                    MessageBox.Show(If(isBuy, "Not enough cash.", "Not enough jjcoin."))
                End If
            End If
        End Using
    End Sub

#End Region

#Region "News"

    Private WithEvents newsTimer As New Timer
    Private newsPressure As Double = 0.0
    Private newsDecay As Double = 0.9
    Private recentNews As New List(Of String)
    Private newsTemplates As New List(Of (headline As String, good As Boolean))

    Public Sub NewsStart()
        FillTemplates()
        RandomizeInterval()
        newsTimer.Start()
    End Sub

    Private Sub FillTemplates()
        newsTemplates = New List(Of (String, Boolean)) From {
            ("JJCoin whale buys $50M of the coin", True),
            ("JJCoin listed on major exchange", True),
            ("Starbucks now accepts JJCoin payments", True),
            ("Unknown billionaire backs JJCoin", True),
            ("JJCoin network upgrade goes live ahead of schedule", True),
            ("Regulator launches investigation into JJCoin", False),
            ("JJCoin suffers 51% attack scare", False),
            ("JJCoin mining farm seized in crackdown", False),
            ("Anonymous hacker dumps JJCoin holdings", False),
            ("JJCoin network upgrade postponed indefinitely", False)
        }
    End Sub

    Private Sub newsTimer_Tick(sender As Object, e As EventArgs) Handles newsTimer.Tick
        GenerateNews()
        RandomizeInterval()
    End Sub

    Private Sub RandomizeInterval()
        Dim wait = 15000 + (rng.Next(-6000, 6001) + rng.Next(-6000, 6001))
        newsTimer.Interval = Math.Max(1000, Math.Min(30000, wait))
    End Sub

    Private Sub GenerateNews()
        Dim template = newsTemplates(rng.Next(newsTemplates.Count))

        Dim strength = 0.5 + rng.NextDouble() * 1.5
        Dim direction = If(template.good, 1.0, -1.0)
        newsPressure = direction * strength

        recentNews.Insert(0, template.headline)
        If recentNews.Count > 3 Then recentNews.RemoveAt(recentNews.Count - 1)

        PushNews()
    End Sub

    Public Function NewsDrift() As Double
        Dim drift = newsPressure
        newsPressure *= newsDecay
        Return drift
    End Function

    Private Sub PushNews()
        webView.CoreWebView2.ExecuteScriptAsync($"setNews({System.Text.Json.JsonSerializer.Serialize(recentNews)})")
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
            ' top up so even the highest timeframe has a full chart immediately
            While prices.Count < 5000
                NextTick()
            End While
        Else
            Seed()
        End If
    End Sub

    Private Sub ChartTimer_Tick(sender As Object, e As EventArgs) Handles ChartTimer.Tick
        NextTick()
        CheckPendingOrders()
        webView.CoreWebView2.ExecuteScriptAsync($"appendPrice({jjcoinPrice.ToString(Global.System.Globalization.CultureInfo.InvariantCulture)})")
        webView.CoreWebView2.ExecuteScriptAsync($"setPrice({jjcoinPrice.ToString(Global.System.Globalization.CultureInfo.InvariantCulture)})")
        PushPortfolio()
    End Sub

    Private Sub PushHistory()
        webView.CoreWebView2.ExecuteScriptAsync($"drawData([{String.Join(",", prices)}])")
    End Sub

    Private Sub PushPortfolio()
        Dim jjcoinValue = save.JJCoin * jjcoinPrice
        webView.CoreWebView2.ExecuteScriptAsync(
            $"setPortfolio('{FormatWithCommas(save.Cash)}', '{FormatWithCommas(jjcoinValue)}', '{FormatWithCommas(save.JJCoin)}')")
    End Sub

    Private Sub PushTrades()
        Dim payload = save.TradeHistory.Select(Function(t) New With {
            .time = t.Time, .side = t.Side, .qty = t.Qty, .price = t.Price, .total = t.Total
        })
        webView.CoreWebView2.ExecuteScriptAsync($"setTrades({System.Text.Json.JsonSerializer.Serialize(payload)})")
    End Sub

    Private Sub PushOrders()
        Dim payload = save.OpenOrders.Select(Function(o) New With {
            .id = o.Id, .side = If(o.IsBuy, "buy", "sell"), .qty = o.Quantity, .limit = o.LimitPrice
        })
        webView.CoreWebView2.ExecuteScriptAsync($"setOrders({System.Text.Json.JsonSerializer.Serialize(payload)})")
    End Sub

    Private Sub OnWebMessage(sender As Object, e As CoreWebView2WebMessageReceivedEventArgs)
        Dim root = System.Text.Json.JsonDocument.Parse(e.WebMessageAsJson).RootElement
        Dim action = root.GetProperty("action").GetString()

        Dim qty As Double = 0
        Dim qtyElem As System.Text.Json.JsonElement
        If root.TryGetProperty("qty", qtyElem) Then qty = qtyElem.GetDouble()

        Dim limit As Double? = Nothing
        Dim limitElem As System.Text.Json.JsonElement
        If root.TryGetProperty("limit", limitElem) Then limit = limitElem.GetDouble()

        If action = "cancel" Then
            Dim idElem As System.Text.Json.JsonElement
            If root.TryGetProperty("id", idElem) Then CancelOrder(idElem.GetInt64())
        Else
            HandleAction(action, qty, limit)
        End If
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
        NewsStart()
    End Sub

#End Region

End Class