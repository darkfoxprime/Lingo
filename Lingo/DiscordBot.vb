Imports System.Threading
Imports Discord
Imports Discord.WebSocket

''' <summary>
''' A Discord "chat bot" that implements the "IChatBot" interface.
''' </summary>
''' <remarks>
''' Written by Johnson Earls.
''' For support, email me at darkfoxprime@gmail.com.
''' </remarks>
Public Class DiscordBot
    Implements IChatBot

    Public Enum LogSeverity
        Debug = Discord.LogSeverity.Debug
        Verbose = Discord.LogSeverity.Verbose
        Info = Discord.LogSeverity.Info
        Warning = Discord.LogSeverity.Warning
        Err = Discord.LogSeverity.Error
        Critical = Discord.LogSeverity.Critical
    End Enum

    Public Const DefaultLogSeverity = LogSeverity.Warning

    Public Class Channel
        Implements IChatBot.IChannel
        Private ReadOnly _ChannelId As Object
        Private ReadOnly _ChannelName As String
        Private ReadOnly _Channel As ISocketMessageChannel
        Public ReadOnly Property ChannelId As Object Implements IChatBot.IChannel.ChannelId
            Get
                Return _ChannelId
            End Get
        End Property

        Public ReadOnly Property ChannelName As String Implements IChatBot.IChannel.ChannelName
            Get
                Return _ChannelName
            End Get
        End Property

        Public Sub SendMessage(Message As String) Implements IChatBot.IChannel.SendMessage
            _Channel.SendMessageAsync(Message).Wait()
        End Sub

        Public Sub New(Channel As ISocketMessageChannel)
            _ChannelId = Channel.Id
            _ChannelName = Channel.Name
            _Channel = Channel
        End Sub
    End Class

    Public Class User
        Implements IChatBot.IUser

        Private ReadOnly _UserId As Object
        Private ReadOnly _UserName As String
        Private ReadOnly _User As Discord.IUser

        Public ReadOnly Property UserId As Object Implements IChatBot.IUser.UserId
            Get
                Return _UserId
            End Get
        End Property

        Public ReadOnly Property UserName As String Implements IChatBot.IUser.UserName
            Get
                Return _UserName
            End Get
        End Property

        Public Sub SendMessage(Message As String) Implements IChatBot.IUser.SendMessage
            _User.SendMessageAsync(Message).Wait()
        End Sub

        Public Sub New(User As Discord.IUser)
            _UserId = User.Id
            If String.IsNullOrEmpty(User.GlobalName) Then
                _UserName = User.Username
            Else
                _UserName = User.GlobalName
            End If
            _User = User
        End Sub
    End Class



    ' Private instance members

    ''' <summary>
    ''' The Discord.Net client interface to Discord.
    ''' </summary>
    Private WithEvents Client As DiscordSocketClient

    ''' <summary>
    ''' The Discord application information.
    ''' </summary>
    Public ApplicationInfo As IApplication

    ''' <summary>
    ''' The channel name to report as "joined" per the IChatBot
    ''' interface.  When 'bot is "ready", it will look for this
    ''' channel name and raise the
    ''' <see cref="IChatBot.JoinedChannel"/> event.
    ''' </summary>
    Private ReadOnly ChannelNameToJoin As String

    ''' <summary>
    ''' The OAuth token used to establish (or re-establish) the connection to Discord.
    ''' </summary>
    Private ReadOnly BotToken As String

    ''' <summary>
    ''' The socket configuration used when connecting to Discord.
    ''' </summary>
    Private ReadOnly SocketConfig As DiscordSocketConfig

    ''' <summary>
    ''' Indicates if the connection is _intended_ to be closed.  Used to handle support reconnects.
    ''' </summary>
    Private ConnectionClosed As Boolean

    ' Properties

    ''' <summary>
    ''' The owner of the bot.
    ''' </summary>
    ''' <returns>The <see cref="IChatBot.IUser"/> representation of the bot's owner.</returns>
    Public ReadOnly Property BotOwner As IChatBot.IUser Implements IChatBot.BotOwner
        Get
            Return _BotOwner
        End Get
    End Property
    ''' <summary>
    ''' The backing store for the <see cref="BotOwner"/> property
    ''' </summary>
    Private _BotOwner As User

    ''' <summary>
    ''' The bot user itself.
    ''' </summary>
    ''' <returns>The <see cref="IChatBot.IUser"/> representation of the bot's user.</returns>
    Public ReadOnly Property BotUser As IChatBot.IUser Implements IChatBot.BotUser
        Get
            Return _BotUser
        End Get
    End Property
    ''' <summary>
    ''' The backing store for the <see cref="BotUser"/> property
    ''' </summary>
    Private _BotUser As User

    ' Events

    Public Event Ready() Implements IChatBot.Ready
    Public Event ConnectedToServer() Implements IChatBot.ConnectedToServer
    ' ReconnectedToServer is NOT USED in this chatbot
    Public Event ReconnectedToServer() Implements IChatBot.ReconnectedToServer
    Public Event DisconnectedFromServer() Implements IChatBot.DisconnectedFromServer
    Public Event ConnectionFailed(Reason As String) Implements IChatBot.ConnectionFailed
    Public Event JoinedChannel(Channel As IChatBot.IChannel) Implements IChatBot.JoinedChannel
    Public Event LeftChannel(Channel As IChatBot.IChannel) Implements IChatBot.LeftChannel
    Public Event MessageReceived(Message As String, Source As IChatBot.IUser, Channel As IChatBot.IChannel) Implements IChatBot.MessageReceived
    Public Event PrivateMessageReceived(Message As String, Source As IChatBot.IUser) Implements IChatBot.PrivateMessageReceived

    ' Code

    Public Sub New(DiscordBotToken As String, DiscordChannel As String, Optional LogLevel As Discord.LogSeverity = DefaultLogSeverity)
        SocketConfig = New DiscordSocketConfig() With {
            .GatewayIntents = GatewayIntents.Guilds Or GatewayIntents.GuildMessages Or GatewayIntents.DirectMessages Or GatewayIntents.MessageContent,
            .LogLevel = LogLevel
        }
        ChannelNameToJoin = DiscordChannel
        BotToken = DiscordBotToken
        Client = Nothing
        Reconnect()
    End Sub

    Public Sub Reconnect() Implements IChatBot.Reconnect
        If Client IsNot Nothing Then
            Client.StopAsync().Wait()
        End If
        ConnectionClosed = False
        Client = New DiscordSocketClient(SocketConfig)
        Client.LoginAsync(TokenType.Bot, BotToken).Wait()
        Dim AppInfoTask = Client.GetApplicationInfoAsync()
        AppInfoTask.Wait()
        ApplicationInfo = AppInfoTask.Result
        Client.StartAsync().Wait()
    End Sub

    Public Sub Close() Implements IChatBot.Close
        ConnectionClosed = True
        Client.StopAsync().Wait()
        Client = Nothing
    End Sub

    Public Function FindUser(Username As String) As IChatBot.IUser Implements IChatBot.FindUser
        If Not String.IsNullOrWhiteSpace(Username) Then
            Dim _id As ULong
            Dim User As IUser
            If ULong.TryParse(Username, _id) Then
                User = Client.GetUser(_id)
            Else
                User = Client.GetUser(Username)
            End If
            Return New User(User)
        End If
        Return Nothing
    End Function

    ' This is STUPIDLY expensive.  Don't call it more than once or twice.
    Public Function FindChannel(Channel As String) As IChatBot.IChannel Implements IChatBot.FindChannel
        Console.WriteLine($"Finding channel {Channel}...")
        If Not String.IsNullOrEmpty(Channel) Then
            For Each Guild In Me.Client.Guilds
                Console.WriteLine($"Checking Guild {Guild.Id} = {Guild.Name}...")
                For Each GuildChannel In Guild.Channels
                    Console.WriteLine($"Checking Channel {GuildChannel.Name}")
                    If Channel = $"{Guild.Name}.${GuildChannel.Name}" Or Channel = GuildChannel.Name Then
                        Return New Channel(GuildChannel)
                    End If
                Next
            Next
        End If
        Return Nothing
    End Function

    ' Event Handlers

    Private Function Client_Connected() As Task Handles Client.Connected
        RaiseEvent ConnectedToServer()
        Return Task.CompletedTask
    End Function

    Private Function Client_Ready() As Task Handles Client.Ready
        _BotOwner = New User(ApplicationInfo.Owner)
        _BotUser = New User(Client.CurrentUser)
        Dim channel = FindChannel(ChannelNameToJoin)
        If channel IsNot Nothing Then
            RaiseEvent JoinedChannel(channel)
        End If
        RaiseEvent Ready()
        Return Task.CompletedTask
    End Function

    ' This is actually only raised when the client connection fails?!?
    Private Function Client_Disconnected(Exc As Exception) As Task Handles Client.Disconnected
        RaiseEvent DisconnectedFromServer()
        If Not ConnectionClosed Then
            Reconnect()
        End If
        Return Task.CompletedTask
    End Function

    Private Function Client_MessageReceived(Message As SocketMessage) As Task Handles Client.MessageReceived
        If Message.Author.Id <> Me.Client.CurrentUser.Id Then
            If TypeOf (Message.Channel) Is IPrivateChannel Then
                RaiseEvent PrivateMessageReceived(Message.Content, New User(Message.Author))
            Else
                RaiseEvent MessageReceived(Message.Content, New User(Message.Author), New Channel(Message.Channel))
            End If
        End If
        Return Task.CompletedTask
    End Function

    Private Shared Function Log(Message As LogMessage) As Task Handles Client.Log
        Console.WriteLine(Message.ToString())
        Return Task.CompletedTask
    End Function

End Class
