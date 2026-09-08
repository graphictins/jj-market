Imports System.ComponentModel
Imports System.Drawing
Imports System.Globalization

Public Class TradeForm
    Inherits Form

    Private lblPrice As New Label
    Private lblQuantity As New Label
    Private txtQuantity As New TextBox
    Private WithEvents btnOk As New Button
    Private btnCancel As New Button
    Private WithEvents priceTimer As New Timer With {.Interval = 500}
    Private priceGetter As Func(Of Double)

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Quantity As Double

    Public Sub New(isBuy As Boolean, priceGetter As Func(Of Double))
        Me.priceGetter = priceGetter

        Text = If(isBuy, "Buy JJCoin", "Sell JJCoin")
        FormBorderStyle = FormBorderStyle.FixedDialog
        StartPosition = FormStartPosition.CenterParent
        ClientSize = New Size(280, 160)
        MinimizeBox = False
        MaximizeBox = False

        lblPrice.Text = "Price: " & priceGetter().ToString("N2", CultureInfo.InvariantCulture)
        lblPrice.Location = New Point(16, 16)
        lblPrice.AutoSize = True

        lblQuantity.Text = "Quantity:"
        lblQuantity.Location = New Point(16, 44)
        lblQuantity.AutoSize = True

        txtQuantity.Location = New Point(96, 40)
        txtQuantity.Width = 120

        btnOk.Text = If(isBuy, "Buy", "Sell")
        btnOk.Location = New Point(96, 84)
        btnOk.Width = 80

        btnCancel.Text = "Cancel"
        btnCancel.DialogResult = DialogResult.Cancel
        btnCancel.Location = New Point(184, 84)
        btnCancel.Width = 80

        AcceptButton = btnOk
        CancelButton = btnCancel

        Controls.Add(lblPrice)
        Controls.Add(lblQuantity)
        Controls.Add(txtQuantity)
        Controls.Add(btnOk)
        Controls.Add(btnCancel)

        priceTimer.Start()
    End Sub

    Private Sub priceTimer_Tick(sender As Object, e As EventArgs) Handles priceTimer.Tick
        lblPrice.Text = "Price: " & priceGetter().ToString("N2", CultureInfo.InvariantCulture)
    End Sub

    Private Sub TradeForm_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        priceTimer.Stop()
    End Sub

    Private Sub btnOk_Click(sender As Object, e As EventArgs) Handles btnOk.Click
        Dim qty As Double
        If Double.TryParse(txtQuantity.Text, qty) AndAlso qty > 0 Then
            Quantity = qty
            DialogResult = DialogResult.OK
            Close()
        Else
            MessageBox.Show("Enter a quantity greater than 0.")
        End If
    End Sub

End Class