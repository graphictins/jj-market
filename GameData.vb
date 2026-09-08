Imports System.IO
Imports System.Globalization
Imports System.Linq

Public Class SaveData
    Public Cash As Double = 10200
    Public JJCoin As Double = 10000
    Public Prices As New List(Of Double)
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
            Select Case line.Substring(0, eq).Trim()
                Case "cash" : data.Cash = Double.Parse(line.Substring(eq + 1), NFI)
                Case "jjcoin" : data.JJCoin = Double.Parse(line.Substring(eq + 1), NFI)
                Case "prices"
                    data.Prices = line.Substring(eq + 1).Trim.Trim("[", "]").Split(","c) _
                        .Where(Function(s) s.Trim() <> "") _
                        .Select(Function(s) Double.Parse(s.Trim(), NFI)).ToList()
                Case Else : Continue For
            End Select
        Next
        Return data
    End Function

    Public Shared Sub Save(data As SaveData)
        Directory.CreateDirectory(Path.GetDirectoryName(filePath))
        Dim toml = $"cash = {data.Cash.ToString(NFI)}{vbCrLf}" &
                   $"jjcoin = {data.JJCoin.ToString(NFI)}{vbCrLf}" &
                   "prices = [ " & String.Join(", ", data.Prices.Select(Function(p) p.ToString(NFI))) & " ]"
        File.WriteAllText(filePath, toml)
    End Sub
End Class