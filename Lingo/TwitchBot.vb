Imports TwitchLib.Client
Imports TwitchLib.Client.Enums
Imports TwitchLib.Client.Events
Imports TwitchLib.Client.Extensions
Imports TwitchLib.Client.Models
Imports TwitchLib.Communication.Events
Imports TwitchLib.Api


Public Class TwitchBot
    Implements IChatBot

    ' Class-specific types

    ''' <summary>
    ''' A simple enum used to choose between a private whisper or channel chat message
    ''' </summary>
    Public Enum MessageModes
        chat
        whisper
    End Enum

    ''' <summary>
    ''' A "generic" representation of a chat user.  Every user must have some sort of unique ID, a visible name, and a way to send a message to that user.
    ''' </summary>
    ''' <seealso cref="IChatBot.IUser"/>
    Public Class User
        Implements IChatBot.IUser

        ''' <summary>
        ''' The unique ID (in this case, the user name itself) for the user.
        ''' </summary>
        ''' <seealso cref="IChatBot.IUser.UserId"/>
        Public ReadOnly Property UserId As Object Implements IChatBot.IUser.UserId
            Get
                Return _UserName
            End Get
        End Property

        ''' <summary>
        ''' Backing store for <see cref="UserName"/>
        ''' </summary>
        Private _UserName As String

        ''' <summary>
        ''' The unique ID (in this case, the "room ID") for the user.
        ''' </summary>
        ''' <seealso cref="IChatBot.IUser.UserName"/>
        Public ReadOnly Property UserName As String Implements IChatBot.IUser.UserName
            Get
                Return _UserName
            End Get
        End Property

        ''' <summary>
        ''' The TwitchBot which holds the client used to talk to the user.
        ''' </summary>
        Private _TwitchBot As TwitchBot

        ''' <summary>
        ''' Send a message to this user.
        ''' </summary>
        ''' <param name="Message">The text of the message to send</param>
        ''' <seealso cref="IChatBot.IUser.SendMessage"/>
        Public Sub SendMessage(Message As String) Implements IChatBot.IUser.SendMessage
            _TwitchBot.Client.SendWhisper(_UserName, Message)
        End Sub

        ''' <summary>
        ''' Create the user representation.
        ''' </summary>
        ''' <param name="Name">The name of the user.</param>
        ''' <param name="Bot">The TwitchBot which holds the client used to talk to the user.</param>
        Public Sub New(Name As String, Bot As TwitchBot)
            _UserName = Name
            _TwitchBot = Bot
        End Sub
    End Class

    ''' <summary>
    ''' A "generic" representation of a chat channel.  Every channel must have some sort of unique ID, a visible name, and a way to send a message to that channel.
    ''' </summary>
    ''' <seealso cref="IChatBot.IChannel"/>
    Public Class Channel
        Implements IChatBot.IChannel

        ''' <summary>
        ''' The unique ID (in this case, the channel name itself) for the channel.
        ''' </summary>
        ''' <seealso cref="IChatBot.IChannel.ChannelId"/>
        Public ReadOnly Property ChannelId As Object Implements IChatBot.IChannel.ChannelId
            Get
                Return _ChannelName
            End Get
        End Property

        ''' <summary>
        ''' Backing store for <see cref="ChannelName"/>
        ''' </summary>
        Private _ChannelName As String

        ''' <summary>
        ''' The unique ID (in this case, the "room ID") for the channel.
        ''' </summary>
        ''' <seealso cref="IChatBot.IChannel.ChannelName"/>
        Public ReadOnly Property ChannelName As String Implements IChatBot.IChannel.ChannelName
            Get
                Return _ChannelName
            End Get
        End Property

        ''' <summary>
        ''' The TwitchBot which holds the client used to talk to the channel.
        ''' </summary>
        Private _TwitchBot As TwitchBot

        ''' <summary>
        ''' Send a message to this channel.
        ''' </summary>
        ''' <param name="Message">The text of the message to send</param>
        ''' <seealso cref="IChatBot.IChannel.SendMessage"/>
        Public Sub SendMessage(Message As String) Implements IChatBot.IChannel.SendMessage
            _TwitchBot.Client.SendMessage(_ChannelName, Message)
        End Sub

        ''' <summary>
        ''' Create the channel representation.
        ''' </summary>
        ''' <param name="Name">The name of the channel.</param>
        ''' <param name="Bot">The TwitchBot which holds the client used to talk to the channel.</param>
        Public Sub New(Name As String, Bot As TwitchBot)
            _ChannelName = Name
            _TwitchBot = Bot
        End Sub
    End Class



    ' Events raised by this class

    ''' <seealso cref="IChatBot.ConnectionFailed"/>
    Public Event ConnectionFailed(Reason As String) Implements IChatBot.ConnectionFailed
    ''' <seealso cref="IChatBot.ConnectedToServer"/>
    Public Event ConnectedToServer() Implements IChatBot.ConnectedToServer
    ''' <seealso cref="IChatBot.ReconnectedToServer"/>
    Public Event ReconnectedToServer() Implements IChatBot.ReconnectedToServer
    ''' <seealso cref="IChatBot.DisconnectedFromServer"/>
    Public Event DisconnectedFromServer() Implements IChatBot.DisconnectedFromServer
    ''' <seealso cref="IChatBot.JoinedChannel"/>
    Public Event JoinedChannel(Channel As IChatBot.IChannel) Implements IChatBot.JoinedChannel
    ''' <seealso cref="IChatBot.LeftChannel"/>
    Public Event LeftChannel(Channel As IChatBot.IChannel) Implements IChatBot.LeftChannel
    ''' <seealso cref="IChatBot.Ready"/>
    Public Event Ready() Implements IChatBot.Ready
    ''' <seealso cref="IChatBot.MessageReceived"/>
    Public Event MessageReceived(Message As String, Source As IChatBot.IUser, Channel As IChatBot.IChannel) Implements IChatBot.MessageReceived
    ''' <seealso cref="IChatBot.PrivateMessageReceived"/>
    Public Event PrivateMessageReceived(Message As String, Source As IChatBot.IUser) Implements IChatBot.PrivateMessageReceived



    ' Properties and fields

    ''' <summary>
    ''' The bot's username on Twitch.
    ''' </summary>
    Public ReadOnly Username As String

    ''' <summary>
    ''' The OAuth token used to authenticate the bot.
    ''' </summary>
    Private ReadOnly OAuthToken As String

    ''' <summary>
    ''' The Twitch channel that the 'bot listens and posts to.
    ''' </summary>
    Public ReadOnly BotChannel As Channel

    ''' <summary>
    ''' The Twitch channel that the 'bot listens and posts to.
    ''' </summary>
    Public ReadOnly Owner As User

    ''' <summary>
    ''' The Twitch API connection
    ''' </summary>
    Private WithEvents Client As TwitchClient

    ''' <summary>
    ''' True if the connection is currently closed.
    ''' If this is True _when_ the OnDisconnect event fires,
    ''' then do not try to reconnect.
    ''' </summary>
    Private ConnectionClosed As Boolean = True



    ' Methods

    ''' <summary>
    ''' This creates a new TwitchBot instance and connects to the chat server.
    ''' </summary>
    ''' <param name="botUsername">The username to connect as.</param>
    ''' <param name="botOAuth">The OAuth Token which will authenticate the bot.</param>
    ''' <param name="channelName">The Twitch channel to connect to.</param>
    ''' <param name="botOwner">The name of the user who owns the bot.</param>
    Public Sub New(botUsername As String, botOAuth As String, channelName As String, botOwner As String)
        ' Populate instance fields
        Username = botUsername
        OAuthToken = botOAuth
        BotChannel = New Channel(channelName, Me)
        Owner = New User(botOwner, Me)
        Client = New TwitchClient()
        ' Initialize the client
        Dim credentials As New ConnectionCredentials(Username, OAuthToken)
        Client.Initialize(credentials, BotChannel.ChannelName)
        ' Connect to the API
        Connect()
    End Sub

    ''' <summary>
    ''' Connect to the chat server.  Raises a <see cref="IChatBot.ConnectionFailed"/> event with an error message if the connection fails.
    ''' </summary>
    Private Sub Connect()
        Try
            Client.Connect()
        Catch ex As Exception
            RaiseEvent ConnectionFailed(ex.Message)
        End Try
    End Sub

    ''' <summary>
    ''' Catches Twitch errors and reports them to the Debug stream.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <code></code> which contains the exception that was caught.</param>
    Private Sub Twitch_onError(sender As Object, e As OnErrorEventArgs) Handles Client.OnError
        Debug.WriteLine(e.Exception.Message)
    End Sub

    ''' <summary>
    ''' Handle the bot's initial connection to Twitch: Raise a <see cref="IChatBot.ConnectedToServer"/> event.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <see cref="TwitchLib.Client.Events.OnConnectedArgs"/> which contains the <code>.BotUserName</code> that connected and the <code>.AutoJoinChannel</code> channel name that the bot automatically joined.</param>
    Private Sub Twitch_OnConnected(ByVal sender As Object, ByVal e As OnConnectedArgs) Handles Client.OnConnected
        Debug.WriteLine($"Connected to {e.AutoJoinChannel}")
        ConnectionClosed = False
        RaiseEvent ConnectedToServer()
    End Sub

    ''' <summary>
    ''' Handle the bot reconnecting to Twitch: Raise a <see cref="IChatBot.ReconnectedToServer"/> event.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <see cref="TwitchLib.Communication.Events.OnReconnectedEventArgs"/> which is empty.</param>
    Private Sub Twitch_OnReconnected(ByVal sender As Object, ByVal e As OnReconnectedEventArgs) Handles Client.OnReconnected
        RaiseEvent ReconnectedToServer()
    End Sub

    ''' <summary>
    ''' Handle the bot disconnecting from Twitch: Raise a <see cref="IChatBot.DisconnectedFromServer"/> event and immediately try to connect again.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <see cref="TwitchLib.Communication.Events.OnDisconnectedEventArgs"/> which is empty.</param>
    Private Sub Twitch_OnDisconnected(ByVal sender As Object, ByVal e As OnDisconnectedEventArgs) Handles Client.OnDisconnected
        Dim wasAlreadyClosed = ConnectionClosed
        ConnectionClosed = True
        RaiseEvent DisconnectedFromServer()
        If Not wasAlreadyClosed Then
            Connect()
        End If
    End Sub

    ''' <summary>
    ''' Handle the bot joining its channel: Raise a <see cref="IChatBot.JoinedChannel"/> event followed, if the channel was the bot's channel, by the <see cref="IChatBot.Ready"/> event.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <see cref="TwitchLib.Client.Events.OnJoinedChannelArgs"/> which contains the <code>.BotUserName</code> that connected and the <code>.Channel</code> channel name that the bot joined.</param>
    Private Sub Twitch_OnJoinedChannel(ByVal sender As Object, ByVal e As OnJoinedChannelArgs) Handles Client.OnJoinedChannel
        RaiseEvent JoinedChannel(New Channel(e.Channel, Me))
        If e.Channel = BotChannel.ChannelName Then
            RaiseEvent Ready()
        End If
    End Sub

    ''' <summary>
    ''' Handle the bot leaving its channel: Raise a <see cref="IChatBot.LeftChannel"/> event.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <code>OnLeftChannelArgs</code> which contains the <code>.BotUserName</code> that left the channel and the <code>.Channel</code> name that the bot left.</param>
    Private Sub Twitch_onLeftChannel(sender As Object, e As OnLeftChannelArgs) Handles Client.OnLeftChannel
        RaiseEvent LeftChannel(New Channel(e.Channel, Me))
    End Sub

    ''' <summary>
    ''' Handle the bot receiving a message from the chat channel: Raise a corresponding <see cref="IChatBot.MessageReceived"/> event.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <code>OnMessageReceivedArgs</code> which contains the <code>.ChatMessage</code> that was received.</param>
    Private Sub Twitch_OnMessageReceived(ByVal sender As Object, ByVal e As OnMessageReceivedArgs) Handles Client.OnMessageReceived
        RaiseEvent MessageReceived(e.ChatMessage.Message, New User(e.ChatMessage.Username, Me), New Channel(e.ChatMessage.Channel, Me))
    End Sub

    ''' <summary>
    ''' Handle the bot receiving a message from the chat channel: Raise a corresponding <see cref="IChatBot.MessageReceived"/> event.
    ''' </summary>
    ''' <param name="sender">The API object which raised the event.</param>
    ''' <param name="e">The <code>OnWhisperReceivedArgs</code> which contains the <code>.WhisperMessage</code> that was received.</param>
    Private Sub Twitch_OnWhisperReceived(ByVal sender As Object, ByVal e As OnWhisperReceivedArgs) Handles Client.OnWhisperReceived
        RaiseEvent PrivateMessageReceived(e.WhisperMessage.Message, New User(e.WhisperMessage.Username, Me))
    End Sub

    ''' <summary>
    ''' Find a channel and return the <see cref="IChatBot.IChannel"/>
    ''' representation of it.
    ''' 
    ''' THIS IS BROKEN and just creates a <code>ILingoChatBot.IChannel</code>
    ''' instance with the channel name.  I have no idea if it would
    ''' actually function.  (I don't even know if Twitch has the
    ''' concept of multiple channels per room.)
    ''' </summary>
    ''' <param name="ChannelName">The name of the channel to find.</param>
    ''' <returns>The <see cref="IChatBot.IChannel"/> corresponding to the named channel.</returns>
    Public Function FindChannel(ChannelName As String) As IChatBot.IChannel Implements IChatBot.FindChannel
        Return New Channel(ChannelName, Me)
    End Function

    ''' <summary>
    ''' Find a user and return the <see cref="IChatBot.IUser"/>
    ''' representation of it.
    ''' 
    ''' THIS IS BROKEN and just creates a <code>ILingoChatBot.IUser</code>
    ''' instance with the user name.  I have no idea if it would
    ''' actually function.
    ''' </summary>
    ''' <param name="UserName">The name of the user to find.</param>
    ''' <returns>The <see cref="IChatBot.IUser"/> corresponding to the named user.</returns>
    Public Function FindUser(UserName As String) As IChatBot.IUser Implements IChatBot.FindUser
        Return New User(UserName, Me)
    End Function

    ''' <summary>
    ''' Return the bot's owner.
    ''' 
    ''' NOTE - twitch does not seem to have registered bots, and so has no way of determining the bot's owner.
    ''' This just returns the <code>Owner</code> user object that was created as part of this instance.
    ''' </summary>
    ''' <returns>The <code>Owner</code> of the bot.</returns>
    Public Function BotOwner() As IChatBot.IUser Implements IChatBot.BotOwner
        Return Owner
    End Function

    ''' <summary>
    ''' Close the bot connection.
    ''' </summary>
    Public Sub Close() Implements IChatBot.Close
        ' Signal we want the connection to stay closed
        ConnectionClosed = True
        Client.Disconnect()
    End Sub

    ''' <summary>
    ''' Close and re-open the bot's connection.
    ''' </summary>
    Public Sub Reconnect() Implements IChatBot.Reconnect
        If ConnectionClosed Then
            Client.Connect()
        Else
            ' Disconnect and let the OnDisconnect handler re-connect
            Client.Disconnect()
        End If
    End Sub
End Class
