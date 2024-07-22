Imports TwitchLib.Client
Imports TwitchLib.Client.Enums
Imports TwitchLib.Client.Events
Imports TwitchLib.Client.Extensions
Imports TwitchLib.Client.Models
Imports TwitchLib.Communication.Events
Imports TwitchLib.Api
Imports System.Drawing.Drawing2D
Imports Microsoft.VisualBasic.FileIO
Imports System.Text
Imports System.Timers
Imports System.Runtime.Serialization
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports System.ComponentModel

Public Class Form1
    Public g As Graphics
    Public client As TwitchClient
    Public wordlist1, wordlist2 As List(Of String)
    Public gamemode As GameModes
    Dim gametimer As Timer
    Dim gametime As Integer
    Dim scoringcompleted As Boolean
    Friend wordlist As New List(Of String)
    Public players As New List(Of Player)
    Public numballs As Integer = 0
    Public roundnum As Integer = 0
    Public ballmultiplier As Integer = 1
    Dim channel As String

    Enum GameModes
        registration
        guessing
        waiting
        results
    End Enum
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        If My.Settings.channel = "" Then
            channel = InputBox("What is the name of your channel?  Omit the 'twitch.tv/' part.")
            My.Settings.channel = channel
        Else
            channel = My.Settings.channel
        End If
        gametimer = New Timer(1000)
        AddHandler gametimer.Elapsed, New ElapsedEventHandler(AddressOf GameTimer_Tick)
        wordlist = My.Resources.LingoWords.Split(vbCrLf.ToCharArray, StringSplitOptions.RemoveEmptyEntries).ToList
        Dim r As New Random
        For i As Integer = 0 To 999
            ListBox1.Items.Add(wordlist(r.Next(wordlist.Count - 1)))
        Next
        Dim credentials As New ConnectionCredentials("kouragethecowardlybot", "mui2jnpzbi4ne7uohndwz5j0scbpym")
        client = New TwitchClient()
        client.Initialize(credentials, channel)
        AddHandler client.OnJoinedChannel, AddressOf OnJoinedChannel
        AddHandler client.OnMessageReceived, AddressOf OnMessageReceived
        AddHandler client.OnWhisperReceived, AddressOf OnWhisperReceived
        AddHandler client.OnConnected, AddressOf Client_OnConnected
        AddHandler client.OnDisconnected, AddressOf Client_OnDisconnected
        AddHandler client.OnReconnected, AddressOf Client_OnReconnected
        AddHandler client.OnLeftChannel, AddressOf Client_onLeftChannel
        AddHandler client.OnError, AddressOf Client_onError
        Try
            client.Connect()
        Catch
            MsgBox("Can't connect.  Close and try again.")
            Me.Invoke(Sub() dumpdata())
            Me.Invoke(Sub() Me.Close())
        End Try
        Dim screennumber As Integer = 0
        If My.Settings.screennumber = -1 Then
            screennumber = InputBox("Which monitor should the public display use?  Typically, 0 is your 'main' display and 1,2,etc. are additional displays.  It is recommended to have the public display set up on a separate monitor from your main one.")
            My.Settings.screennumber = screennumber
        Else
            screennumber = My.Settings.screennumber
        End If
        PublicDisplay.Location = Screen.AllScreens(screennumber).Bounds.Location
        PublicDisplay.Size = Screen.AllScreens(screennumber).Bounds.Size
        PublicDisplay.Show()
    End Sub

    Private Sub Client_onError(sender As Object, e As OnErrorEventArgs)
        Debug.WriteLine(e.Exception.Message)
    End Sub

    Private Sub Client_onLeftChannel(sender As Object, e As OnLeftChannelArgs)
        Me.Invoke(Sub() TextBox3.Text = "Left Channel")
    End Sub

    Private Sub Client_OnConnected(ByVal sender As Object, ByVal e As OnConnectedArgs)
        Debug.WriteLine($"Connected to {e.AutoJoinChannel}")
        Me.Invoke(Sub() TextBox2.Text = "Connected")
        Debug.WriteLine("Connected")
    End Sub
    Private Sub Client_OnReconnected(ByVal sender As Object, ByVal e As OnReconnectedEventArgs)
        Me.Invoke(Sub() TextBox2.Text = "Reconnected")
        Debug.WriteLine("Reconnected")
    End Sub
    Private Sub Client_OnDisconnected(ByVal sender As Object, ByVal e As OnDisconnectedEventArgs)
        Me.Invoke(Sub() TextBox2.Text = "Disconnected")
        Debug.WriteLine("Disconnected")
        Dim credentials As New ConnectionCredentials("kouragethecowardlybot", "mui2jnpzbi4ne7uohndwz5j0scbpym")
        'Dim credentials As New ConnectionCredentials("liquid_kourage", "j1kiijo0ymyef61xq6nbvr9jsw7f7i")
        client = New TwitchClient()
        client.Initialize(credentials, channel)
        AddHandler client.OnJoinedChannel, AddressOf OnJoinedChannel
        AddHandler client.OnMessageReceived, AddressOf OnMessageReceived
        AddHandler client.OnWhisperReceived, AddressOf OnWhisperReceived
        AddHandler client.OnConnected, AddressOf Client_OnConnected
        AddHandler client.OnDisconnected, AddressOf Client_OnDisconnected
        AddHandler client.OnReconnected, AddressOf Client_OnReconnected
        AddHandler client.OnLeftChannel, AddressOf Client_onLeftChannel
        Try
            client.Connect()
        Catch
            MsgBox("Can't connect.  Close and try again.")
            Me.Invoke(Sub() dumpdata())
            Me.Invoke(Sub() Me.Close())
        End Try
    End Sub

    Private Sub dumpdata()
        Dim formatter As IFormatter = New BinaryFormatter()
        Dim stream As Stream = New FileStream("lingosave.bin", FileMode.Create, FileAccess.Write, FileShare.None)
        formatter.Serialize(stream, players)
        stream.Close()
    End Sub

    Private Sub OnJoinedChannel(ByVal sender As Object, ByVal e As OnJoinedChannelArgs)
        client.SendMessage(e.Channel, "Lingo is LIVE!  To sign up: Using either the Twitch chat or a whisper to KourageTheCowardlyBot, type the word '!in'!")
        If gamemode = Nothing Then gamemode = GameModes.registration
    End Sub
    Private Sub OnMessageReceived(ByVal sender As Object, ByVal e As OnMessageReceivedArgs)
        Select Case True
            Case e.ChatMessage.Message.ToLower = "!in"
                Me.Invoke(Sub() registerplayer(e.ChatMessage.Username))
            Case e.ChatMessage.Message.ToLower = "!out"
                Me.Invoke(Sub() unregisterplayer(e.ChatMessage.Username))
                'Case gamemode = GameModes.guessing
                '    If System.Text.RegularExpressions.Regex.IsMatch(e.ChatMessage.Message, "^[A-Za-z]{5}$") Then
                '        client.SendMessage(e.ChatMessage.Channel, "/delete " + e.ChatMessage.Id)
                '        Me.Invoke(Sub() updateplayerguess(e.ChatMessage.Username, e.ChatMessage.Message))
                '        Me.Invoke(Sub() lockinplayerguess(e.ChatMessage.Username))
                '    End If
            Case e.ChatMessage.Message.ToLower = "!feedback"
                Me.Invoke(Sub() sendfeedback(e.ChatMessage.Username, client, "chat"))
        End Select
    End Sub

    Private Sub registerplayer(username As String)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then Exit Sub
        Dim p As New Player(username)
        players.Add(p)
        players.Sort(Function(x, y) x.Name.CompareTo(y.Name))
        drawalluserresults()
        ListBox2.Items.Add(username + " - ")
        ListBox3.Items.Add(username + " - ")
    End Sub
    Private Sub unregisterplayer(username As String)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            players.Remove(p)
        End If
        drawalluserresults()
        ListBox2.Items.Remove(username + " - ")
        ListBox3.Items.Remove(username + " - ")
    End Sub
    Private Sub updatescore(username As String, score As Integer)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            p.Balls = score
            p.updategraphic()
            drawuserresult(p)
        End If
    End Sub

    Private Sub OnWhisperReceived(ByVal sender As Object, ByVal e As OnWhisperReceivedArgs)
        Select Case True
            Case e.WhisperMessage.Message.ToLower = "!in"
                'client.SendWhisper(e.WhisperMessage.Username, "Your entry is confirmed.")
                Me.Invoke(Sub() registerplayer(e.WhisperMessage.Username))
            Case e.WhisperMessage.Message.ToLower = "!out"
                Me.Invoke(Sub() unregisterplayer(e.WhisperMessage.Username))
            Case e.WhisperMessage.Message.ToLower = "!feedback"
                Me.Invoke(Sub() sendfeedback(e.WhisperMessage.Username, client, "whisper"))
            Case gamemode = GameModes.guessing
                If System.Text.RegularExpressions.Regex.IsMatch(e.WhisperMessage.Message, "^[A-Za-z]{5}$") Then
                    Me.Invoke(Sub() updateplayerguess(e.WhisperMessage.Username, e.WhisperMessage.Message))
                    Me.Invoke(Sub() lockinplayerguess(e.WhisperMessage.Username))
                    Dim stillwaiting As Boolean = False
                    For Each p As Player In players
                        'need to limit this to players who have not already gotten the word right
                        If (p.guess = "" AndAlso p.roundresult(roundnum - 1) < 1) OrElse p.guess = "@@@@@" Then stillwaiting = True
                    Next
                    If Not stillwaiting Then Me.Invoke(Sub() Button1.PerformClick())
                Else
                    Me.Invoke(Sub() updateplayernotes(e.WhisperMessage.Username, e.WhisperMessage.Message))
                End If
                'Case gamemode = GameModes.results
                '    Dim match As Predicate(Of Player) = Function(pl) pl.Name = e.WhisperMessage.Username
                '    If players.Exists(match) Then
                '        Dim p As Player = players.Find(match)
                '        If p.guess = "" Then
                '            If System.Text.RegularExpressions.Regex.IsMatch(e.WhisperMessage.Message, "^[A-Za-z]{5}$") Then
                '                Me.Invoke(Sub() updateplayerguess(e.WhisperMessage.Username, e.WhisperMessage.Message))
                '                Me.Invoke(Sub() lockinplayerguess(e.WhisperMessage.Username))

                '                Dim beenguessed As Boolean = False
                '                If wordlist.Contains(p.guess.ToUpper) AndAlso p.roundresult(roundnum - 1) = 0 Then
                '                        p.updategraphic(getLingoResult(Label4.Text, p.guess))
                '                    ElseIf p.guess <> "@@@@@" AndAlso p.roundresult(roundnum - 1) = 0 Then
                '                        If p.guess = "" Then p.updategraphic("     ") Else p.updategraphic("\\\\\")
                '                    End If
                '                    If beenguessed = False AndAlso p.roundresult(roundnum - 1) >= 1 Then beenguessed = True
                '                p.allguesses.Add(p.guess)
                '                drawalluserresults()

                '            Else
                '                Me.Invoke(Sub() updateplayernotes(e.WhisperMessage.Username, e.WhisperMessage.Message))
                '            End If
                '        End If
                '    End If
            Case Else
                Me.Invoke(Sub() updateplayernotes(e.WhisperMessage.Username, e.WhisperMessage.Message))
        End Select
    End Sub

    Private Sub sendfeedback(username As String, client As TwitchClient, mode As String)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            p.setfeedback(mode)
            Select Case mode
                Case "whisper"
                    client.SendWhisper(username, p.feedback)
                Case "chat"
                    client.SendMessage(channel, p.feedback)
            End Select
        End If
    End Sub

    Private Sub lockinplayerguess(username As String)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            p.updategraphic("     ", True)
            drawuserresult(p)
        End If
    End Sub

    Private Sub updateplayerguess(username As String, message As String)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            p.guess = message
            For i As Integer = 0 To ListBox2.Items.Count - 1
                If ListBox2.Items(i).StartsWith(username + " - ") Then
                    ListBox2.Items(i) = username + " - " + message.ToUpper
                    Exit For
                End If
            Next
        End If
    End Sub
    Private Sub updateplayernotes(username As String, message As String)
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            p.notes.Add(message)
            For i As Integer = 0 To ListBox3.Items.Count - 1
                If ListBox3.Items(i).StartsWith(username + " - ") Then
                    ListBox3.Items(i) = username + " - " + message
                    Exit For
                End If
            Next
            p.hasnotes = True
        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        gamemode = GameModes.results
        Dim beenguessed As Boolean = False
        For Each p As Player In players
            If wordlist.Contains(p.guess.ToUpper) AndAlso p.roundresult(roundnum - 1) = 0 Then
                p.updategraphic(getLingoResult(Label4.Text, p.guess))
            ElseIf p.guess <> "@@@@@" AndAlso p.roundresult(roundnum - 1) = 0 Then
                If p.guess = "" Then p.updategraphic("     ") Else p.updategraphic("\\\\\")
            End If
            If beenguessed = False AndAlso p.roundresult(roundnum - 1) >= 1 Then beenguessed = True
            p.allguesses.Add(p.guess)
        Next
        drawalluserresults()

        If numballs = 6 * ballmultiplier Then numballs = 5 * ballmultiplier
        If beenguessed Then numballs -= 1 * ballmultiplier
        If numballs >= 2 * ballmultiplier Then
            While client.JoinedChannels.Count = 0
                client.Connect()
                Threading.Thread.Sleep(5000)
            End While
            Me.Invoke(Sub() client.SendMessage(client.JoinedChannels(0), "Time's up!  You now have 45 seconds to look at your feedback.  To see this at any time, type !feedback in chat."))
            gametime = 45
            gametimer.Start()
        Else
            gametimer.Stop()
        End If
    End Sub

    Private Sub drawalluserresults()
        Using g As Graphics = PublicDisplay.PictureBox1.CreateGraphics
            g.DrawImage(My.Resources.lingobg11, 0, 0, 1920, 1080)
            Using sf As New StringFormat
                sf.Alignment = StringAlignment.Center
                Dim r2 As New Rectangle(0, 0, 1920, 60)
                Dim r3 As New Rectangle(0, 1020, 1920, 60)
                Dim gp As New GraphicsPath()
                Using f As FontFamily = New FontFamily("Boomer Tantrum")
                    If roundnum > 0 Then gp.AddString("This guess worth " + numballs.ToString + " balls", f, FontStyle.Regular, g.DpiY * 48 / 72, r2, sf)
                    If gamemode = GameModes.results Then
                        gp.AddString("Here are the results.  DO NOT GUESS NOW!", f, FontStyle.Regular, g.DpiY * 48 / 72, r3, sf)
                    Else
                        If roundnum > 0 Then gp.AddString("Word #" + roundnum.ToString + ", first letter is " + Label4.Text(0).ToString.ToUpper, f, FontStyle.Regular, g.DpiY * 48 / 72, r3, sf)
                    End If
                    sf.LineAlignment = StringAlignment.Far
                    g.DrawPath(Pens.Black, gp)
                    g.FillPath(Brushes.White, gp)
                End Using
            End Using
            Dim num As Integer = players.Count
            If num > 0 Then
                Dim numcolumns As Integer = Math.Ceiling((num * 0.8) ^ 0.5)
                Dim numrows As Integer = Math.Ceiling(num / numcolumns)
                Dim unitwidth As Integer = Math.Floor(1900 / (numcolumns + 1))
                Dim gapwidth As Integer = 0
                If numcolumns > 1 Then gapwidth = Math.Floor(unitwidth / (2 * (numcolumns - 1)))
                Dim temp As Integer = unitwidth
                unitwidth += gapwidth * (numcolumns - 1) / numcolumns
                Dim unitheight As Integer = Math.Floor(unitwidth * 3 / 8)
                Dim gapheight As Integer = 0
                If numrows > 1 Then gapheight = Math.Floor(unitheight / (8 * (numrows - 1)))
                Dim extra As Integer = Math.Max(0, Math.Floor((1900 - (unitwidth * numcolumns) - (gapwidth * (numcolumns - 1))) / 2))
                Dim eytra As Integer = Math.Max(0, Math.Floor((880 - (unitheight * numrows) - (gapheight * (numrows - 1))) / 2))
                For x As Integer = 0 To numcolumns - 1
                    For y As Integer = 0 To numrows - 1
                        Try
                            players.Item(numcolumns * y + x).position = New Point((x * (unitwidth + gapwidth)) + 10 + extra, (y * (unitheight + gapheight)) + 100 + eytra)
                            players.Item(numcolumns * y + x).size = New Size(unitwidth, unitheight)
                            g.DrawImage(players.Item(numcolumns * y + x).graphic, (x * (unitwidth + gapwidth)) + 10 + extra, (y * (unitheight + gapheight)) + 100 + eytra, unitwidth, unitheight)
                        Catch
                        End Try
                    Next
                Next
            End If
        End Using
        Using g As Graphics = PublicDisplay.PictureBox2.CreateGraphics
            g.DrawImage(My.Resources.hack2, 0, 0)
        End Using
    End Sub
    Private Sub drawuserresult(p As Player)
        Using g As Graphics = PublicDisplay.PictureBox1.CreateGraphics
            g.DrawImage(p.graphic, p.position.X, p.position.Y, p.size.Width, p.size.Height)
            'PublicDisplay.PictureBox1.Invalidate(New Rectangle(p.position.X, p.position.Y, p.size.Width, p.size.Height))
        End Using
    End Sub
    Private Sub ListBox1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseDoubleClick
        Label4.Text = ListBox1.SelectedItem
    End Sub

    Private Sub ListBox1_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseDown
        If e.Button = MouseButtons.Right Then
            wordlist.Remove(ListBox1.SelectedItem)
            ListBox1.Items.Remove(ListBox1.SelectedItem)

        End If
    End Sub
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        roundnum += 1
        gamemode = GameModes.guessing
        numballs = 6 * ballmultiplier
        For Each p As Player In players
            p.updategraphic("     ", False)
            p.guess = ""
            p.allguesses.Clear()
        Next
        drawalluserresults()
        For i As Integer = 0 To ListBox2.Items.Count - 1
            ListBox2.Items(i) = ListBox2.Items(i).ToString.Split(" - ")(0) + " - "
        Next
        While client.JoinedChannels.Count = 0
            client.Connect()
            Threading.Thread.Sleep(5000)
        End While
        Me.Invoke(Sub() client.SendMessage(client.JoinedChannels(0), "Round " + roundnum.ToString + " has started!  The first letter is " + Label4.Text.Chars(0) + ".  You have 90 seconds to submit your guess via whisper!"))
        gametime = 90
        gametimer.Start()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        gamemode = GameModes.guessing
        For Each p As Player In players
            p.guess = ""
            p.updategraphic("     ", False)
        Next
        drawalluserresults()
        For i As Integer = 0 To ListBox2.Items.Count - 1
            ListBox2.Items(i) = ListBox2.Items(i).ToString.Split(" - ")(0) + " - "
        Next
        While client.JoinedChannels.Count = 0
            client.Connect()
            Threading.Thread.Sleep(5000)
        End While
        Me.Invoke(Sub() client.SendMessage(client.JoinedChannels(0), "Round " + roundnum.ToString + " continues for " + numballs.ToString + " balls!  The first letter is " + Label4.Text.Chars(0) + ".  You have 90 seconds to submit your guess via whisper!"))
        gametime = 90
        gametimer.Start()
    End Sub

    Private Sub GameTimer_Tick(sender As Object, e As ElapsedEventArgs)
        gametime -= 1
        Select Case True
            Case gametime = 30 AndAlso gamemode = GameModes.guessing
                If Not (client.JoinedChannels.Count = 0) Then client.SendMessage(client.JoinedChannels(0), "30 seconds to go...")
            Case gametime = 10 AndAlso gamemode = GameModes.guessing
                If Not (client.JoinedChannels.Count = 0) Then client.SendMessage(client.JoinedChannels(0), "LAST CHANCE, 10 seconds...")
                'Case gametime = 100
                'client.SendMessage(client.JoinedChannels(0), "10 seconds left...")
                'Case gametime = 50
                'client.SendMessage(client.JoinedChannels(0), "5 seconds...")
        End Select
        If gametime <= 0 Then
            sender.Stop()
            If gamemode = GameModes.guessing Then
                Me.Invoke(Sub() Button1.PerformClick())
            ElseIf gamemode = GameModes.results Then
                Me.Invoke(Sub() Button3.PerformClick())
            End If
        End If
        Me.Invoke(Sub() updatetimer())

    End Sub

    Private Sub updatetimer()
        Dim bmp As New Bitmap(200, 100)
        Using g As Graphics = PublicDisplay.PictureBox2.CreateGraphics
            g.DrawImage(My.Resources.hack2, 0, 0)
            Using sf As New StringFormat
                sf.LineAlignment = StringAlignment.Center
                Dim r2 As New Rectangle(50, 0, 100, 100)
                Dim gp As New GraphicsPath()
                Using f As FontFamily = New FontFamily("Boomer Tantrum")
                    gp.AddString(gametime.ToString, f, FontStyle.Regular, g.DpiY * 40 / 72, r2, sf)
                    g.FillPath(Brushes.Yellow, gp)
                End Using
            End Using
            g.DrawImage(bmp, 0, 0)
            g.DrawRectangle(Pens.Yellow, New Rectangle(0, 0, bmp.Width, bmp.Height))
        End Using
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        For Each p As Player In players
            p.Balls += 1
            p.updategraphic()
            'drawuserresult(p)
        Next
        drawalluserresults()
    End Sub

    Friend Function getLingoResult(target As String, guess As String) As String
        If target.ToUpper = guess.ToUpper Then Return "!!!!!"
        Dim temptarget As New StringBuilder(target.ToUpper, 5)
        Dim tempguess As New StringBuilder(guess.ToUpper, 5)
        For i As Integer = 0 To 4
            If target.ToUpper()(i) = guess.ToUpper()(i) Then
                temptarget(i) = "."
                tempguess(i) = "!"
            End If
        Next
        For i As Integer = 0 To 4
            If temptarget.ToString.Contains(tempguess(i)) Then
                For j As Integer = 0 To 4
                    If temptarget(j) = tempguess(i) Then
                        temptarget(j) = "."
                        Exit For
                    End If
                Next
                tempguess(i) = "?"
            ElseIf tempguess(i) <> "!" Then
                tempguess(i) = "/"
            End If
        Next
        Return tempguess.ToString
    End Function

    Private Sub ListBox2_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox2.MouseDown
        ListBox2.SelectedIndex = ListBox2.IndexFromPoint(e.X, e.Y)
        If ListBox2.SelectedIndex = -1 Then Exit Sub
        Dim username As String = ListBox2.SelectedItem.ToString.Substring(0, ListBox2.SelectedItem.ToString.IndexOf(" - "))
        If e.Button = MouseButtons.Right Then
            Select Case MessageBox.Show("Are you sure you want to remove this player?  This cannot be undone.", "Remove player?", MessageBoxButtons.YesNo) = DialogResult.Yes
                Case True
                    unregisterplayer(username)
                    drawalluserresults()
                Case False
            End Select
        ElseIf e.Button = MouseButtons.Left Then
            Dim temp As String = InputBox("Update Score")
            If temp <> "" Then
                Try
                    Dim newscore As Integer = CInt(temp)
                    updatescore(username, newscore)
                Catch
                    MsgBox("Score must be a number.")
                End Try
            End If
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        Dim formatter As IFormatter = New BinaryFormatter()
        Dim stream As Stream = New FileStream("lingosave.bin", FileMode.Open, FileAccess.Read, FileShare.Read)
        players = formatter.Deserialize(stream)
        stream.Close()
        drawalluserresults()
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        dumpdata()
    End Sub

    Private Sub ListBox3_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBox3.MouseDown
        If e.Button = MouseButtons.Left Then
            ListBox3.SelectedIndex = ListBox3.IndexFromPoint(e.X, e.Y)
            If ListBox3.SelectedIndex = -1 Then Exit Sub
            Dim username As String = ListBox3.SelectedItem.ToString.Substring(0, ListBox3.SelectedItem.ToString.IndexOf(" - "))
            Dim match As Predicate(Of Player) = Function(pl) pl.Name = username
            If players.Exists(match) Then
                Dim p As Player = players.Find(match)
                Dim notes As String = ""
                For Each n As String In p.notes
                    notes = notes & n & vbCrLf
                Next
                MessageBox.Show(notes)
            End If
        End If
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click
        gametimer.Enabled = Not gametimer.Enabled
        Select Case sender.Text
            Case "Pause"
                sender.Text = "Resume"
            Case "Resume"
                sender.Text = "Pause"
        End Select
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        Dim newchamp As Boolean = False
        Dim champ As String = InputBox("Crown A New Champion", "", My.Settings.champion).ToLower()
        Dim match As Predicate(Of Player) = Function(pl) pl.Name = champ
        If players.Exists(match) Then
            Dim p As Player = players.Find(match)
            p.ischamp = True
            p.updategraphic()
            newchamp = True
        End If
        Dim match2 As Predicate(Of Player) = Function(pl) pl.Name = My.Settings.champion
        If newchamp AndAlso players.Exists(match2) Then
            Dim p As Player = players.Find(match2)
            p.ischamp = False
            p.updategraphic()
        End If
        If newchamp Then
            drawalluserresults()
            My.Settings.champion = champ
            My.Settings.Save()
        End If
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs) Handles TextBox1.TextChanged

    End Sub

    Private Sub Form1_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        dumpdata()
    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        If MsgBox("This mode cannot be disabled.  Are you sure?", MsgBoxStyle.YesNo, "Enable 2x Ball Mode") = 7 Then Exit Sub
        ballmultiplier = 2
        Button9.Enabled = False
        Me.Invoke(Sub() client.SendMessage(client.JoinedChannels(0), "Hold your hats everyone, now we're playing for DOUBLE BALLS!"))
    End Sub

    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.KeyCode = Keys.Return Then
            Label4.Text = TextBox1.Text.ToUpper()
        End If
    End Sub
End Class
