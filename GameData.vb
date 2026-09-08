Imports System.IO
Imports System.Text.Json

Public Class SaveData
    Public Cash As Double = 10200
    Public JJCoin As Double = 10000
    Public Prices As New List(Of Double)
End Class

Public Class GameData
    Private Shared ReadOnly home As String = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)

    Public Shared ReadOnly Dir As String = Path.Combine(home, ".cache", "jjmarket-data")

    Public Shared Sub Ensure()
        Directory.CreateDirectory(Dir)
    End Sub

    Public Shared Function Load() As SaveData
        Dim filePath As String = Path.Combine(Dir, "save.json")
        If Not File.Exists(filePath) Then Return New SaveData
        Return JsonSerializer.Deserialize(Of SaveData)(File.ReadAllText(filePath))
    End Function

    Public Shared Sub Save(data As SaveData)
        Ensure()
        File.WriteAllText(Path.Combine(Dir, "save.json"),
                          JsonSerializer.Serialize(data, New JsonSerializerOptions With {.WriteIndented = True}))
    End Sub
End Class