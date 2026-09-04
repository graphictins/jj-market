<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer


    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font

        ' --- Size: height = 75% of screen height, width = height * (5/3) ---
        Dim screenBounds As System.Drawing.Rectangle = Screen.PrimaryScreen.WorkingArea
        Dim formHeight As Integer = CInt(screenBounds.Height * 0.75)
        Dim formWidth As Integer = CInt(formHeight * (5.0R / 3.0R))

        Me.ClientSize = New System.Drawing.Size(formWidth, formHeight)

        ' --- Position: centered on screen ---
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen

        Me.Text = "jjmarket"
    End Sub
End Class