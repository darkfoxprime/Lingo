''' <summary>
''' A generic "chat bot" interface.
''' </summary>
''' <remarks>
''' Written by Johnson Earls.
''' For support, email me at darkfoxprime@gmail.com.
''' </remarks>
Public Interface IChatBot

    ''' <summary>
    ''' A "generic" representation of a chat channel.  Every channel must have some sort of unique ID, a visible name, and a way to send a message to that channel.
    ''' </summary>
    Interface IChannel
        ''' <summary>
        ''' The unique ID for the channel.
        ''' </summary>
        ReadOnly Property ChannelId As Object
        ''' <summary>
        ''' The visible name for the channel.
        ''' </summary>
        ReadOnly Property ChannelName As String
        ''' <summary>
        ''' Send a message to this channel.
        ''' </summary>
        ''' <param name="Message">The text of the message to send</param>
        Sub SendMessage(Message As String)
    End Interface

    ''' <summary>
    ''' A "generic" representation of a chat user.  Every user must have some sort of unique ID, a visible name, and a way to send a message to that user.
    ''' </summary>
    Interface IUser
        ''' <summary>
        ''' The unique ID for the user.
        ''' </summary>
        ReadOnly Property UserId As Object
        ''' <summary>
        ''' The visible name for the user.
        ''' </summary>
        ReadOnly Property UserName As String
        ''' <summary>
        ''' Send a message to this user.
        ''' </summary>
        ''' <param name="Message">The text of the message to send</param>
        Sub SendMessage(Message As String)
    End Interface

    ''' <summary>
    ''' Raised when the connection attempt fails for whatever reason.
    ''' </summary>
    ''' <param name="Reason">The error message from the failed connection attempt.</param>
    Event ConnectionFailed(Reason As String)
    ''' <summary>
    ''' Raised when the bot connects to the server.
    ''' </summary>
    Event ConnectedToServer()
    ''' <summary>
    ''' Raised when the bot reconnects to the server.
    ''' </summary>
    Event ReconnectedToServer()
    ''' <summary>
    ''' Raised when the bot's connection is closed.
    ''' </summary>
    Event DisconnectedFromServer()
    ''' <summary>
    ''' Raised when the bot joins a channel.
    ''' </summary>
    ''' <param name="Channel">The <see cref="IChannel"/> representing the channel that was joined.</param>
    Event JoinedChannel(Channel As IChannel)
    ''' <summary>
    ''' Raised when the bot leaves a channel.
    ''' </summary>
    ''' <param name="Channel">The <see cref="IChannel"/> representing the channel that was left.</param>
    Event LeftChannel(Channel As IChannel)
    ''' <summary>
    ''' Raised when the bot is ready (meaning the client has connected and the bot has joined its channel).
    ''' </summary>
    Event Ready()
    ''' <summary>
    ''' Raised when a chat message is received in a channel.
    ''' </summary>
    ''' <param name="Message">The message text received.</param>
    ''' <param name="Source">The <see cref="IUser"/> representation of the user who sent the message.</param>
    ''' <param name="Channel">The <see cref="IChannel"/> representaiton of the channel in which the message was received.</param>
    Event MessageReceived(Message As String, Source As IUser, Channel As IChannel)
    ''' <summary>
    ''' Raised when a private message is received from a user.
    ''' </summary>
    ''' <param name="Message">The message text received.</param>
    ''' <param name="Source">The <see cref="IUser"/> representation of the user who sent the message.</param>
    Event PrivateMessageReceived(Message As String, Source As IUser)

    ''' <summary>
    ''' Find a chat channel and return its <see cref="IChannel"/> representation.
    ''' </summary>
    ''' <param name="ChannelName">The name of the channel to find.</param>
    ''' <returns>The <see cref="IChannel"/> representation of the channel.</returns>
    Function FindChannel(ChannelName As String) As IChannel

    ''' <summary>
    ''' Find a user and return their <see cref="IUser"/> representation.
    ''' </summary>
    ''' <param name="UserName">The name of the user to find.</param>
    ''' <returns>The <see cref="IUser"/> representation of the user.</returns>
    Function FindUser(UserName As String) As IUser

    ''' <summary>
    ''' Return the bot's owner.
    ''' </summary>
    ''' <returns>The <see cref="IUser"/> representation of the bot's owner.</returns>
    Function BotOwner() As IUser

    ''' <summary>
    ''' Close the bot's connection to the server.
    ''' </summary>
    Sub Close()

    ''' <summary>
    ''' Attempt to re-establish the bot's connection to the server,
    ''' whether it's currently open or not.
    ''' </summary>
    Sub Reconnect()

End Interface
