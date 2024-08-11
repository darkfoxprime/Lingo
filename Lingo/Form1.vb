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
    Public WithEvents ChatBot As IChatBot
    Public Channel As IChatBot.IChannel = Nothing
    Public wordlist1, wordlist2 As List(Of String)
    Public gamemode As GameModes = GameModes.notready
    Dim gametimer As Timer
    Dim gametime As Integer
    Dim scoringcompleted As Boolean
    Friend wordlist As New List(Of String)
    Public players As New List(Of Player)
    Public numballs As Integer = 0
    Public roundnum As Integer = 0
    Public ballmultiplier As Integer = 1



    ' Private classes and types



    Enum GameModes
        registration
        guessing
        waiting
        results
        notready = -1
    End Enum

    Enum MessageModes
        chat
        whisper
    End Enum




    ' ChatBot event handlers



    Private Sub ChatBot_Ready() Handles ChatBot.Ready
        ' Only do the initial steps if we haven't done anything yet
        If gamemode = GameModes.notready Then
            Channel.SendMessage("Lingo is LIVE!  To sign up: Using either the Twitch chat or a whisper to KourageTheCowardlyBot, type the word '!in'!")
            ChangeGameModeTo(GameModes.registration)
        End If
    End Sub

    Private Sub ChatBot_ConnectedToServer() Handles ChatBot.ConnectedToServer
        ConnectionStatusChanged("Connected")
    End Sub

    Private Sub ChatBot_ReconnectedToServer() Handles ChatBot.ReconnectedToServer
        ConnectionStatusChanged("Reconnected")
    End Sub

    Private Sub ChatBot_DisconnectedFromServer() Handles ChatBot.DisconnectedFromServer
        ConnectionStatusChanged("Disconnected")
    End Sub

    Private Sub ChatBot_JoinedChannel(Channel As IChatBot.IChannel) Handles ChatBot.JoinedChannel
        If Me.Channel Is Nothing Then
            Me.Channel = Channel
        End If
        ConnectionStatusChanged("Joined Channel")
    End Sub

    Private Sub ChatBot_MessageReceived(Message As String, Source As IChatBot.IUser, Channel As IChatBot.IChannel) Handles ChatBot.MessageReceived
        Handle_ChatBot_Message(Message, Source, MessageModes.chat)
    End Sub

    Private Sub ChatBot_PrivateMessageReceived(Message As String, Source As IChatBot.IUser) Handles ChatBot.PrivateMessageReceived
        Handle_ChatBot_Message(Message, Source, MessageModes.whisper)
    End Sub

    Private Sub ChatBot_LeftChannel(Channel As IChatBot.IChannel) Handles ChatBot.LeftChannel
        ConnectionStatusChanged("Left Channel")
        If Channel.ChannelId = Me.Channel.ChannelId Then
            Me.Channel = Nothing
        End If
    End Sub

    Private Sub ChatBot_ConnectionFailed(Reason As String) Handles ChatBot.ConnectionFailed
        Me.Invoke(Sub() MsgBox(Reason))
        Me.Invoke(Sub() dumpdata())
        Me.Invoke(Sub() Me.Close())
    End Sub



    ' Game logic



    Private Sub ConnectionStatusChanged(status As String)
        Debug.WriteLine(status)
        If status.ToLower.Contains("channel") Then
            Me.Invoke(Sub() TextBox3.Text = status)
        Else
            Me.Invoke(Sub() TextBox2.Text = status)
        End If
    End Sub

    Private Sub Handle_ChatBot_Message(message As String, fromUser As IChatBot.IUser, Optional howReceived As MessageModes = MessageModes.chat)
        Select Case True
            Case message.ToLower = "!in"
                Me.Invoke(Sub() registerplayer(fromUser.UserName))
                fromUser.SendMessage("Your entry is confirmed.")
            Case message.ToLower = "!out"
                Me.Invoke(Sub() unregisterplayer(fromUser.UserName))
            Case message.ToLower = "!feedback"
                Me.Invoke(Sub() sendfeedback(fromUser, howReceived))
            Case gamemode = GameModes.guessing And howReceived = MessageModes.whisper
                If System.Text.RegularExpressions.Regex.IsMatch(message, "^[A-Za-z]{5}$") Then
                    Me.Invoke(Sub() updateplayerguess(fromUser.UserName, message))
                    Me.Invoke(Sub() lockinplayerguess(fromUser.UserName))
                    Dim stillwaiting As Boolean = False
                    For Each p As Player In players
                        'need to limit this to players who have not already gotten the word right
                        If (p.guess = "" AndAlso p.roundresult(roundnum - 1) < 1) OrElse p.guess = "@@@@@" Then stillwaiting = True
                    Next
                    If Not stillwaiting Then Me.Invoke(Sub() Button1.PerformClick())
                Else
                    Me.Invoke(Sub() updateplayernotes(fromUser.UserName, message))
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
            Case howReceived = MessageModes.whisper
                Me.Invoke(Sub() updateplayernotes(fromUser.UserName, message))
        End Select
    End Sub

    ' Change the game mode to `new_gamemode` but only if the current
    ' game mode is in `only_from_gamemodes`
    Private Sub ChangeGameModeTo(new_gamemode As GameModes, only_from_gamemodes As GameModes())
        If only_from_gamemodes.Contains(gamemode) Then
            ChangeGameModeTo(new_gamemode)
        End If
    End Sub

    ' Change the game mode to `new_gamemode` (if it's not already that mode)
    Private Sub ChangeGameModeTo(new_gamemode As GameModes)
        If gamemode <> new_gamemode Then
            ' Debug.WriteLine($"Switching game mode from {gamemode} to {new_gamemode}")
            gamemode = new_gamemode
        End If
    End Sub

    Private Sub dumpdata()
        Dim formatter As IFormatter = New BinaryFormatter()
        Dim stream As Stream = New FileStream("lingosave.bin", FileMode.Create, FileAccess.Write, FileShare.None)
        formatter.Serialize(stream, players)
        stream.Close()
    End Sub

    Private Sub registerplayer(username As String)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = username)
        If p Is Nothing Then
            p = New Player(username)
            players.Add(p)
            players.Sort(Function(x, y) x.Name.CompareTo(y.Name))
            AddPlayerToUI(p)
        End If
    End Sub

    Private Sub unregisterplayer(username As String)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = username)
        If p IsNot Nothing Then
            RemovePlayerFromUI(p)
            players.Remove(p)
        End If
    End Sub

    Private Sub updatescore(username As String, score As Integer)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = username)
        If p IsNot Nothing Then
            p.Balls = score
            p.updategraphic()
            drawuserresult(p)
        End If
    End Sub

    Private Sub sendfeedback(user As IChatBot.IUser, mode As MessageModes)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = user.UserName)
        If p IsNot Nothing Then
            Select Case mode
                Case MessageModes.whisper
                    p.setfeedback(True)
                    user.SendMessage(p.feedback)
                Case MessageModes.chat
                    p.setfeedback(False)
                    Channel.SendMessage(p.feedback)
            End Select
        End If
    End Sub

    Private Sub lockinplayerguess(username As String)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = username)
        If p IsNot Nothing Then
            p.updategraphic("     ", True)
            drawuserresult(p)
        End If
    End Sub

    Private Sub updateplayerguess(username As String, message As String)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = username)
        If p IsNot Nothing Then
            p.guess = message
            UpdatePlayerGuessInUI(p)
        End If
    End Sub

    Private Sub updateplayernotes(username As String, message As String)
        Dim p As Player = players.Find(Function(pl As Player) pl.Name = username)
        If p IsNot Nothing Then
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




    ' UI Logic

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles Me.Load
        Dim bot_type = My.Settings.default_bot_type
        For Each arg In My.Application.CommandLineArgs
            Select Case arg
                Case "-discord"
                    bot_type = "discord"
                Case "-twitch"
                    bot_type = "twitch"
                Case Else
                    MessageBox.Show($"Ignoring unknown command line argument {arg} and all remaining arguments!")
                    Exit For
            End Select
        Next
        ' just in case the default `My.Settings.default_bot_type` is an unknown type
        If Not {"twitch", "discord"}.Contains(bot_type) Then
            MessageBox.Show($"Unknown bot type {bot_type} found, assuming `twitch`!")
            bot_type = "twitch"
        End If
        If bot_type = "twitch" Then
            If My.Settings.twitch_channel = "" Then
                My.Settings.twitch_channel = InputBox("What is the name of your Twitch channel?  Omit the 'twitch.tv/' part.")
            End If

            If My.Settings.twitch_username = "" Then
                My.Settings.twitch_username = InputBox("What Twitch username should I log in to?")
            End If

            If My.Settings.twitch_oauth_token = "" Then
                My.Settings.twitch_oauth_token = InputBox("What Twitch OAuth Token should I use to log in?")
            End If

            ' Twitch_Connect("kouragethecowardlybot", "mui2jnpzbi4ne7uohndwz5j0scbpym", channel)
            ChatBot = New TwitchBot(
                My.Settings.twitch_username,
                My.Settings.twitch_oauth_token,
                My.Settings.twitch_channel,
                My.Settings.twitch_bot_owner,
                Debug:=My.Settings.DEBUG
            )
        ElseIf bot_type = "discord" Then
            If My.Settings.discord_channel = "" Then
                My.Settings.discord_channel = InputBox("What Discord channel do you want the bot to talk on?")
            End If

            If My.Settings.discord_oauth_token = "" Then
                My.Settings.discord_oauth_token = InputBox("What Discord OAuth Token should I use to log in?")
            End If

            Dim log_level = If(My.Settings.DEBUG, DiscordBot.LogSeverity.Debug, DiscordBot.DefaultLogSeverity)
            ChatBot = New DiscordBot(
                My.Settings.discord_oauth_token,
                My.Settings.discord_channel,
                LogLevel:=log_level
            )
        Else
            ' Should be unreachable
            ChatBot = Nothing
        End If

        gametimer = New Timer(1000)
        AddHandler gametimer.Elapsed, New ElapsedEventHandler(AddressOf GameTimer_Tick)
        wordlist = My.Resources.LingoWords.Split(vbCrLf.ToCharArray, StringSplitOptions.RemoveEmptyEntries).ToList
        Dim r As New Random
        For i As Integer = 0 To 999
            ListBox1.Items.Add(wordlist(r.Next(wordlist.Count - 1)))
        Next

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

    Private Sub AddPlayerToUI(p As Player)
        drawalluserresults()
        ListBox2.Items.Add(p.Name + " - ")
        ListBox3.Items.Add(p.Name + " - ")
    End Sub

    Private Sub RemovePlayerFromUI(p As Player)
        drawalluserresults()
        ListBox2.Items.Remove(p.Name + " - ")
        ListBox3.Items.Remove(p.Name + " - ")
    End Sub

    Private Sub UpdatePlayerGuessInUI(p As Player)
        For i As Integer = 0 To ListBox2.Items.Count - 1
            If ListBox2.Items(i).StartsWith(p.Name + " - ") Then
                ListBox2.Items(i) = p.Name + " - " + p.guess.ToUpper
                Exit For
            End If
        Next
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        ChangeGameModeTo(GameModes.results)
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
            Channel.SendMessage("Time's up!  You now have 45 seconds to look at your feedback.  To see this at any time, type !feedback in chat.")
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
        ChangeGameModeTo(GameModes.guessing)
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
        While Me.Channel Is Nothing
            ChatBot.Reconnect()
            Threading.Thread.Sleep(5000)
        End While
        Channel.SendMessage($"Round {roundnum} has started!  The first letter is {Label4.Text.Chars(0)}.  You have 90 seconds to submit your guess via whisper!")
        gametime = 90
        gametimer.Start()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        ChangeGameModeTo(GameModes.guessing)
        For Each p As Player In players
            p.guess = ""
            p.updategraphic("     ", False)
        Next
        drawalluserresults()
        For i As Integer = 0 To ListBox2.Items.Count - 1
            ListBox2.Items(i) = ListBox2.Items(i).ToString.Split(" - ")(0) + " - "
        Next
        While Me.Channel Is Nothing
            ChatBot.Reconnect()
            Threading.Thread.Sleep(5000)
        End While
        Channel.SendMessage($"Round {roundnum} continues for {numballs} balls!  The first letter is `{Label4.Text.Chars(0)}`.  You have 90 seconds to submit your guess via whisper!")
        gametime = 90
        gametimer.Start()
    End Sub

    Private Sub GameTimer_Tick(sender As Object, e As ElapsedEventArgs)
        gametime -= 1
        Select Case True
            Case gametime = 30 AndAlso gamemode = GameModes.guessing
                If Channel IsNot Nothing Then
                    Channel.SendMessage("30 seconds to go...")
                End If
            Case gametime = 10 AndAlso gamemode = GameModes.guessing
                If Channel IsNot Nothing Then
                    Channel.SendMessage("LAST CHANCE, 10 seconds...")
                End If
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
        Channel.SendMessage("Hold your hats everyone, now we're playing for DOUBLE BALLS!")
    End Sub

    Private Sub TextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox1.KeyDown
        If e.KeyCode = Keys.Return Then
            Label4.Text = TextBox1.Text.ToUpper()
        End If
    End Sub
End Class
