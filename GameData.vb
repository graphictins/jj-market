Imports System.IO
Imports System.Text
Imports System.Globalization
Imports System.Linq
Imports System.Text.Json

Public Class TradeRecord
    Public Time As String = ""
    Public Side As String = "buy"
    Public Qty As Double = 0
    Public Price As Double = 0
    Public Total As Double = 0
End Class

Public Class PendingOrder
    Public Id As Long = 0
    Public IsBuy As Boolean = True
    Public Quantity As Double = 0
    Public LimitPrice As Double = 0
End Class

Public Class SaveData
    Public Cash As Double = 10200
    Public JJCoin As Double = 10000
    Public Prices As New List(Of Double)
    Public TradeHistory As New List(Of TradeRecord)
    Public OpenOrders As New List(Of PendingOrder)
    Public NextOrderId As Long = 1
End Class

Public Class GameData
    Private Shared ReadOnly home As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    Private Shared ReadOnly filePath As String = Path.Combine(home, ".cache", "jjmarket-data", "save.toml")
    Private Shared ReadOnly NFI As CultureInfo = CultureInfo.InvariantCulture

    Public Shared Function Load() As SaveData
        If Not File.Exists(filePath) Then Return New SaveData
        Dim data As New SaveData
        For Each line In File.ReadAllLines(filePath)
            If line.Trim() = "" Then Continue For
            Dim eq = line.IndexOf("="c)
            If eq < 1 Then Continue For
            Dim raw = line.Substring(eq + 1).Trim()
            Select Case line.Substring(0, eq).Trim()
                Case "cash" : data.Cash = Double.Parse(raw, NFI)
                Case "jjcoin" : data.JJCoin = Double.Parse(raw, NFI)
                Case "nextorderid" : data.NextOrderId = Long.Parse(raw, NFI)
                Case "prices"
                    data.Prices = raw.Trim("[", "]").Split(","c) _
                        .Where(Function(s) s.Trim() <> "") _
                        .Select(Function(s) Double.Parse(s.Trim(), NFI)).ToList()
                Case "history"
                    Try
                        data.TradeHistory = JsonSerializer.Deserialize(Of List(Of TradeRecord))(
                            Encoding.UTF8.GetString(Convert.FromBase64String(raw)))
                    Catch
                    End Try
                Case "orders"
                    Try
                        data.OpenOrders = JsonSerializer.Deserialize(Of List(Of PendingOrder))(
                            Encoding.UTF8.GetString(Convert.FromBase64String(raw)))
                    Catch
                    End Try
                Case Else : Continue For
            End Select
        Next
        Return data
    End Function

    Public Shared Sub Save(data As SaveData)
        Directory.CreateDirectory(Path.GetDirectoryName(filePath))
        Dim histB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data.TradeHistory)))
        Dim ordB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data.OpenOrders)))
        Dim toml = $"cash = {data.Cash.ToString(NFI)}{vbCrLf}" &
                   $"jjcoin = {data.JJCoin.ToString(NFI)}{vbCrLf}" &
                   $"prices = [ {String.Join(", ", data.Prices.Select(Function(p) p.ToString(NFI)))} ]{vbCrLf}" &
                   $"history = {histB64}{vbCrLf}" &
                   $"orders = {ordB64}{vbCrLf}" &
                   $"nextorderid = {data.NextOrderId.ToString(NFI)}"
        File.WriteAllText(filePath, toml)
    End Sub
End Class