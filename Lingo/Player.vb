Imports System.Drawing.Drawing2D

<Serializable()>
Public Class Player
    Friend Name As String
    Friend Balls As Integer
    Friend guess As String
    Friend notes As New List(Of String)
    Friend graphic As Bitmap
    Friend roundresult() As Integer
    Friend allguesses As New List(Of String)
    Friend feedback As String
    Friend position As Point
    Friend size As Size
    Friend hasnotes As Boolean
    Friend ischamp As Boolean

    Public Sub New(username As String)
        Me.Name = username
        Me.Balls = 0
        Me.guess = "@@@@@"
        Me.roundresult = {0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        Me.feedback = ""
        Me.hasnotes = False
        If Me.Name.ToUpper() = My.Settings.champion.ToUpper() Then Me.ischamp = True Else Me.ischamp = False
        updategraphic()
    End Sub
    Friend Sub updategraphic(Optional ByVal result As String = "     ", Optional ByVal guessed As Boolean = False)
        Dim b As New Bitmap(800, 300)
        Using g As Graphics = Graphics.FromImage(b)
            g.SmoothingMode = Drawing2D.SmoothingMode.HighQuality
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
            Using p As New Pen(Brushes.Black, 5)
                Using sf As New StringFormat
                    sf.Alignment = StringAlignment.Center
                    sf.LineAlignment = StringAlignment.Near
                    Dim r As New Rectangle(30, 170, 740, 130)
                    Dim r2 As New Rectangle(30, 40, 740, 60)
                    Dim r3 As New Rectangle(30, 105, 740, 60)
                    Dim gp As New GraphicsPath()
                    Dim gp2 As New GraphicsPath()
                    Using f As FontFamily = New FontFamily("Boomer Tantrum")
                        If (Form1.roundnum > 0 AndAlso roundresult(Form1.roundnum - 1) = -1) OrElse result = "\\\\\" Then
                            g.DrawImage(My.Resources.playerBGred, 0, 0, 800, 300)
                            'If roundresult(Form1.roundnum - 1) = 0 Then
                            '    roundresult(Form1.roundnum - 1) = -1
                            'End If
                            gp.AddString("Not A Word...", f, FontStyle.Regular, g.DpiY * 48 / 72, r, sf)
                        ElseIf result = "!!!!!" Or (Form1.roundnum > 0 AndAlso roundresult(Form1.roundnum - 1) >= 1) Then
                            g.DrawImage(My.Resources.playerBGgreen, 0, 0, 800, 300)
                            If roundresult(Form1.roundnum - 1) = 0 Then
                                Me.Balls += Form1.numballs
                                roundresult(Form1.roundnum - 1) = 2
                            End If
                            gp.AddString("Congratulations!", f, FontStyle.Regular, g.DpiY * 48 / 72, r, sf)
                            If roundresult(Form1.roundnum - 1) = 2 Then
                                Dim gp3 As New GraphicsPath()
                                gp3.AddArc(5, 5, 290, 290, 180, 90)
                                gp3.AddArc(505, 5, 290, 290, 270, 90)
                                gp3.AddArc(505, 5, 290, 290, 0, 90)
                                gp3.AddArc(5, 5, 290, 290, 90, 90)
                                gp3.CloseFigure()
                                gp3.AddArc(30, 30, 240, 240, 180, 90)
                                gp3.AddArc(530, 30, 240, 240, 270, 90)
                                gp3.AddArc(530, 30, 240, 240, 0, 90)
                                gp3.AddArc(30, 30, 240, 240, 90, 90)
                                gp3.CloseFigure()
                                g.FillPath(Brushes.Yellow, gp3)
                                roundresult(Form1.roundnum - 1) = 1
                            End If
                        Else
                            g.DrawImage(My.Resources.playerBG4, 0, 0, 800, 300)
                        End If
                        '
                        gp.AddString(Me.Name, f, FontStyle.Regular, g.DpiY * 48 / 72, r2, sf)
                        gp.AddString(Me.Balls.ToString + " Balls", f, FontStyle.Regular, g.DpiY * 48 / 72, r3, sf)
                        sf.LineAlignment = StringAlignment.Far
                        g.DrawPath(p, gp)
                        g.DrawPath(p, gp2)
                        Select Case guessed AndAlso roundresult(Form1.roundnum - 1) = 0
                            Case True
                                g.FillPath(Brushes.Yellow, gp)
                            Case False
                                g.FillPath(Brushes.White, gp)
                        End Select
                    End Using
                End Using
                If result = "\\\\\" Or guessed Then
                    If ischamp Then g.DrawImage(My.Resources.Lingo_Crown_No_Text, New Rectangle(-2, -2, 100, 100))
                    Me.graphic = b
                    Exit Sub
                End If
                For i As Integer = 0 To 4
                    Select Case result(i)
                        Case "!"
                            If result = "!!!!!" Or (Form1.roundnum > 0 AndAlso roundresult(Form1.roundnum - 1) = 1) Then Exit For
                            g.DrawImage(My.Resources.greenrr, New Rectangle(125 * i + 100, 170, 100, 100))
                        Case "?"
                            g.DrawImage(My.Resources.yellowball, New Rectangle(125 * i + 100, 170, 100, 100))
                        Case "/"
                            g.DrawImage(My.Resources.redx, New Rectangle(125 * i + 100, 170, 100, 100))
                    End Select
                Next
                If ischamp Then g.DrawImage(My.Resources.Lingo_Crown_No_Text, New Rectangle(-2, -2, 100, 100))
            End Using
        End Using
        Me.graphic = b
    End Sub
    Friend Sub setfeedback(ByVal mode As String)
        Try
            Select Case mode
                Case "whisper"
                    Dim outputstring As String = "Previous guesses: "
                    For Each g As String In Me.allguesses
                        If g <> "" Then
                            Select Case Form1.wordlist.Contains(g.ToUpper())
                                Case True
                                    Dim temp As String = Form1.getLingoResult(Form1.Label4.Text, g)
                                    temp = temp.Replace("!", "🔲")
                                    temp = temp.Replace("?", "◯")
                                    temp = temp.Replace("/", "☒")
                                    outputstring += g.ToUpper + " - " + temp + "     "
                                Case False
                                    outputstring += g.ToUpper + " - " + "NOT A WORD" + "     "
                            End Select
                        End If
                    Next
                    Me.feedback = outputstring
                Case "chat"
                    Dim outputstring As String = Me.Name + ", " + "your last on-screen feedback was: "
                    Dim g As String = Me.allguesses.Last
                    Select Case Form1.wordlist.Contains(g.ToUpper())
                        Case True
                            Dim temp As String = Form1.getLingoResult(Form1.Label4.Text, g)
                            temp = temp.Replace("!", "🔲")
                            temp = temp.Replace("?", "◯")
                            temp = temp.Replace("/", "☒")
                            outputstring += temp
                        Case False
                            outputstring += "NOT A WORD"
                    End Select
                    Me.feedback = outputstring
            End Select
        Catch
            'no feedback, most likely
        End Try
    End Sub
End Class
