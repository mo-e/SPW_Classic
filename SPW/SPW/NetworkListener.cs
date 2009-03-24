#region using...
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading ;
using System.Runtime.InteropServices;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
#endregion

public class NetworkListener
{
  public static Socket socket ;
  public Thread listenerThread ;
  
  public static int MAX_PACKET_SIZE = 1008; // = 12*84. Want multiple of 12, since sizeof(Message) struct = 12 bytes

  // IP Address and port where server script can be reached.
  public static string SERVER_IP = "127.0.0.1" ;
  public static int SERVER_PORT  = 7070 ;

  /// <summary>
  /// The first place where messages that come in through the network get saved to.
  /// After they are saved here, we lock-down and rapidly copy them to the Controller.incoming
  /// list.
  /// </summary>
  private List<Message> initialMessageContainer;

  /// <summary>
  /// The listener thread has its own log.  This just makes
  /// browsing the log files easier, instead of having
  /// all the output intermingled in one place.
  /// </summary>
  private FileLogger netLogger;

  public NetworkListener()
  {
    initialMessageContainer = new List<Message>();

    netLogger = new FileLogger( SPW.path + "netlog_" + FileLogger.CurrentTimestamp + ".txt", false );
  }

  /// <summary>
  /// Loops forever, listening at network.
  /// When new data comes in, it puts that new data
  /// into the Controller's processing queue.
  /// </summary>
  public void listen()
  {
    netLogger.Info( "Listener thread startup" ) ;

    byte[] buf = new byte[ MAX_PACKET_SIZE ];

    // TRAP the LISTENER THREAD in this while(true) 
    // loop FOREVER.  Because this is a SEPARATE THREAD,
    // this won't screw up the execution of our MAIN THREAD
    // (all the code that performs the drawing and stuff).
    // If you put the MAIN THREAD in this loop, your game
    // would appear to freeze because it would be trapped
    // in here forever!
    while( true )
    {
      int bytesRead = 0 ;
      try
      {
        bytesRead = socket.Receive( buf ) ;
      }
      catch( Exception exc ) 
      {
        // If the socket throws an exception, it could
        // be because we've lost our connection.  At any
        // rate, it means something has gone horrribly wrong,
        // so we need to drop the network connection
        netLogger.Error( "Exception thrown in receive: " + exc.Message );
        
        // also log to the screen so we can see it
        SPW.logger.Log( "Couldn't receive on socket", LogMessageType.Warning, OutputDevice.ScreenAndFile );
        SPW.logger.Log( "Exception text: " + exc.Message, LogMessageType.Warning, OutputDevice.ScreenAndFile );
        // The exception "Thread was being aborted" is going to occur when we
        // kill the listener thread while this thread is sitting on the socket
        // listening.

        SPW.logger.Log( "Aborting listener thread...", LogMessageType.Warning, OutputDevice.ScreenAndFile );

        // Set to disconnected
        SPW.netState = NetState.Disconnected ;

        // This listener thread must die
        Thread.CurrentThread.Abort();
      }

      if( bytesRead == 0 )
      {
        // the server cut us off!
        // Not using netlogger.Info because I want to see this on the screen when it happens.
        SPW.logger.Log( "The server has disconnected you!", LogMessageType.Warning, OutputDevice.ScreenAndFile );

        SPW.logger.Log( "Aborting listener thread...", LogMessageType.Warning, OutputDevice.ScreenAndFile );

        SPW.netState = NetState.Disconnected;

        // This listener thread must die, then
        Thread.CurrentThread.Abort();

        // We could also have 'return'ed from this function,
        // which would effectively abort this listener thread,
        // but .Abort() is more clear when you READ this code,
        // about what exactly we are doing here.
      }

      // Because we're using TCP on this socket,
      // its possible (in fact likely) that we will
      // receive several Message struct objects
      // smushed together.

      // bytesRead will tell us how many Message structs
      // we got because it will be a multiple of 12 in this
      // instance (it will be 12, 24, 36 or 48 or something).

      // So we construct and process as many Message structs
      // as we know got sent.

      int expectedSize = Message.Size ;
      // now loop through, every Message.Size bytes is
      // another Message struct
      
      if( bytesRead != expectedSize )
      {
        // Just log a note so we can see how often this happens
        netLogger.Info( "TCP:  mashed more than 1 message together.  I read " + bytesRead + " but expected " + expectedSize );
      }
      
      if( bytesRead % Message.Size != 0 )
      {
        // Now, I should mention that It IS __POSSIBLE__ for TCP
        // to cut the message at a really bad spot
        // ( like at 1000 bytes ).  That is bad because,
        // since each Message struct is 12 bytes in size,
        // this would mean we'd have 1000/12 = 83.3333 messages.. basically
        // a Message struct would have been cut in half almost.
        
        // So we'd basically need to piece together that last
        // message struct with the beginning part (in this case,
        // the first 8 bytes of the next packet we receive would
        // complete the previous transmissions last packet).

        // Practically, this doesn't happen very often,
        // and we've set the maximum packet size to 1008 bytes
        // for exactly this reason:  1008 bytes = 12*84, which
        // means that even if the TCP buffer fills up to its maximum
        // (1008 bytes) it should still be sending off packets
        // that are a multiple of 12.

        // Still, it is POSSIBLE for this error to happen and
        // it'd be best if we had code here to take care of this case.
        netLogger.Error( "TCP:  Severe.  I read " + bytesRead + " which isn't a multiple of Message.Size=" + Message.Size );

        // We don't have code in place to handle this, so we'll
        // just log an error as a reminder.
        SPW.logger.Error( "Read " + bytesRead + " bytes, which isn't a multiple of " + Message.Size +
                          ", so a Message was cut in two.  " + 
                          "When are you going to code this section?  Now is a good time :)." );
      }

      for( int i = 0; i < bytesRead; i += expectedSize )
      {
        // get a hunk of 'expectedSize' bytes from the
        // collection of received bytes
        byte[] messageBytes = new byte[ expectedSize ] ;
        Buffer.BlockCopy( buf, i, messageBytes, 0, expectedSize ) ;

        // Create a Message struct from those bytes
        Message receivedMessage = Message.FromBytes( messageBytes ) ;
        if( receivedMessage.playerNumber == Controller.myNetgamePlayerNumber )
        {
          //!! Using own messages for measuring network performance ONLY.

          #region why we use our own messages for network performance measurement
          // We added our own messages to the Controller.incoming queue
          // at the time of the keypresses.

          // We can't use the opponents messages to determine the amount
          // of network delay because the FRAME that opponent is on at
          // any point in time differs.

          // So if your opponent is actually 5 frames AHEAD (he is on 85), and you
          // are 5 frames BEHIND (you are on frame 80), then he'll send
          // messages stamped 85.  So assume the transit time across the
          // internet is actually (5 frames) of time, then you'll get
          // these messages that he stamped "85" when YOU'RE at frame
          // 85.  So, it will appear that those messages are transmitting
          // in 0 frames, which really isn't the case, its just that he was ahead.
          // So to know the network delay time, we're using our own messages
          // that we send out and measuring how long it takes for them to 
          // get back to us.
          #endregion

          int messageTravelTimeInFRAMES = SPW.currentFrame - receivedMessage.frame;
          Controller.delayMetrics.LastMessageTransportTime = messageTravelTimeInFRAMES ;

          // Now we don't have to do anything else with this message,
          // because its really useless.  We already have the information
          // about our own keypresses in Controller.incoming, as we
          // said earlier, they were "short circuit added" to the
          // Controller.incoming queue when the keystrokes happened.
        }
        else
        {
          netLogger.Info( "Got a message " + receivedMessage.ToString() );
          initialMessageContainer.Add( receivedMessage ) ;
        }
      }


      #region copy messages from initialMessageContainer queue to Controller.incoming
      int messagesAdded = 0 ;

      // Because Controller.incoming is _shared_, we must acquire a
      // LOCK on it to prevent the other thread (the MAIN THREAD)
      // from using it while we are adding values to it.

      // We're doing a very rapid copy to the shared resource.
      // Notice how I'm trying to avoid locking the Controller.incoming
      // queue as much as possible, and I'm only locking it for the
      // shortest amount of time possible (just enough time to copy
      // all messages very rapidly from initialMessageContainer to
      // Controller.incoming).
      if( initialMessageContainer.Count > 0 )  // will be 0 when all messages came from 'yourself'
      {
        // lock-down the Controller.incoming queue for this
        // thread's (listenerThread's) exclusive use.
        lock( Controller.incoming )
        {
          // Now add to the collection
          // of Message structs that the MAIN THREAD
          // will eventually process
          for( int i = 0; i < initialMessageContainer.Count ; i++ )
          {
            Controller.incoming.Add( initialMessageContainer[i] );
            messagesAdded++ ;
          }
        }

        netLogger.Info( "Pushed " + messagesAdded + " to Controller.incoming queue" );
      }

      // after adding copies, clear it
      initialMessageContainer.Clear();

      #endregion
    }
  }

  /// <summary>
  /// Attempts to connect to the network game server.
  /// </summary>
  /// <returns>True if connection succeeded, false if failed.</returns>
  public bool Connect()
  {
    SPW.logger.Log( "Beginning connection attempt..", LogMessageType.Info, OutputDevice.File );

    // First, disconnect the old connection, if its still connected.
    ResetNetworkConnection();

    // Create the socket.
    try
    {
      socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP ) ;

      socket.Connect( SERVER_IP, SERVER_PORT );

      SPW.logger.Log( "Connected? " + socket.Connected, LogMessageType.Info, OutputDevice.File );

      // create the listener thread
      listenerThread = new Thread(listen);

      // start it!
      listenerThread.Start();
    }
    catch( Exception exc )
    {
      SPW.logger.Log( "Problem connecting to server.  Exception text: " + exc.Message, LogMessageType.Error, OutputDevice.ScreenAndFile );

      // Connect failed, then.
      // set socket to null, which can be considered
      // as another indicator that the socket is not active
      socket = null;

      // return false, indicating failure to connect to server
      return false ;
    }

    // If get down here, then it means the try block executed
    // "without a hitch", and so the catch() block was skipped
    // entirely, so we can return true here.
    return true ;
  }
  
  
  
  /// <summary>
  /// THE MESSAGE SEND METHOD.  Sends a message across the network to the server
  /// which will eventually be received by the other connected player.
  /// </summary>
  /// <param name="message">The message struct you want to send</param>
  public void Send( Message message )
  {
    if( message.playerNumber == Controller.myNetgamePlayerNumber )
    {
      // Short cct. push directly to incoming queue
      // before sending out across network.
      // (which is obviously necessary for other player to see it!!)

      lock( Controller.incoming )
      {
        Controller.incoming.Add( message ) ;
      }

      // Because we're doing this here, in the Receive
      // (the 'listen()' function), we only
      // copy the messages that originated from THE OTHER PLAYER
      // to the Controller.incoming queue, as our messages
      // are already in our own queue (though the server
      // will still send our own messages back down to us, due to
      // the way the server is programmed).
    }

    // Proceed to send out across network so the other player
    // will get it.
    // Break the Message struct down into its byte-representation
    // and then just send it.
    byte[] messageBytes = message.GetBytes();

    // send that array of bytes across the network, now
    try
    {
      // We could just straight
      // socket.Send it:
      /////////socket.Send( messageBytes ) ;  //!! Using socket.SendAsync() method instead, below
      // socket.Send() works ok, but can make the game
      // feel a bit "chunky" when tcp misbehaves a bit.
      
      ////
      // Here's another way that performs better
      // when the network is a bit laggy:  we'll
      // use the socket.SendAsync method.
      SocketAsyncEventArgs saea = new SocketAsyncEventArgs();
      saea.SetBuffer( message.GetBytes(), 0, Message.Size );
      socket.SendAsync( saea );  // send off "asynchronously" -
      // i.e. basically DO NOT BLOCK THIS THREAD is sending
      // takes more than a couple of microseconds.  Remember
      // this is the MAIN GAME THREAD we're on here, so if this
      // thread blocks, then THE WHOLE GAME STOPS (stops drawing,
      // stops advancing game engine, stops EVERYTHING).
    }
    catch( Exception exc )
    {
      SPW.logger.Log( "Error sending the message " + message.ToString(), LogMessageType.Error, OutputDevice.ScreenAndFile ) ;
      SPW.logger.Log( "Exception text: " + exc.Message, LogMessageType.Error, OutputDevice.File ) ;
    }
  }

  /// <summary>
  /// Resets the network connection by killing the listener thread
  /// and also shutting down the socket.
  /// </summary>
  public void ResetNetworkConnection()
  {
    SPW.logger.Log( "Resetting network connection..", LogMessageType.Info, OutputDevice.File );

    // If the listener thread is alive, abort it
    if( listenerThread != null )
    {
      if( listenerThread.IsAlive )
      {
        listenerThread.Abort();
        SPW.logger.Log( "Listener thread aborted", LogMessageType.Info, OutputDevice.File );
      }

      listenerThread = null;
    }

    // If the socket is not null, then destroy it
    if( socket != null )
    {
      try
      {
        socket.Shutdown( SocketShutdown.Both );
        socket.Close();

        SPW.logger.Log( "Socket closed", LogMessageType.Info, OutputDevice.File );
      }
      catch( Exception exc )
      {
        SPW.logger.Log( "Exception when trying to shut down the socket", LogMessageType.Warning, OutputDevice.File );
        SPW.logger.Log( "Exception text: " + exc.Message, LogMessageType.Warning, OutputDevice.File );
      }

      // Set socket to NULL, because we don't want it anymore.
      socket = null;
    }
  }

  /// <summary>
  /// Kills the listenerThread and also
  /// closes the socket.
  /// </summary>
  public void Shutdown()
  {
    SPW.logger.Log( "Shutting down the socket...", LogMessageType.Info, OutputDevice.File ) ;

    ResetNetworkConnection();

    SPW.logger.Log( "Network system shutdown completed successfully.", LogMessageType.Info, OutputDevice.File );
  }






















  #region NETWORK PERFORMANCE TESTING CODE - don't look at this part, because you don't really need to

  /// <summary>
  /// Frame counter for network test
  /// </summary>
  private static volatile int networkTest_FrameCount;

  /// <summary>
  /// Number of frames to run the network test for.
  /// </summary>
  private static volatile int networkTest_TestLength = 2000;

  /// <summary>
  /// Measures average network delay for the network test
  /// </summary>
  private static DelayMetrics networkTest_DelayMetrics ;

  /// <summary>
  /// The largest size message in bytes sent so far.
  /// If messages are being sent by TCP in big huge
  /// chunks (like 120 bytes) regularly, then
  /// the connection will FEEL very laggy, even if
  /// the connection is quite good (since TCP may be
  /// performing A LOT of buffering).  If this is
  /// one of the biggest problems you have, it
  /// may be time to switch to UDP.
  /// </summary>
  private static int networkTest_largestMessage ;

  /// <summary>
  /// Starts up the network test by firing up
  /// the listenerThread and tying it to
  /// the TestNetworkListen() function.
  /// </summary>
  public void StartTest()
  {
    #region start test
    // test the network
    networkTest_FrameCount = 0;
    networkTest_DelayMetrics = new DelayMetrics();
    networkTest_largestMessage = 0;

    ResetNetworkConnection();

    try
    {
      socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP );
      socket.Connect( SERVER_IP, 7070 );
    }
    catch( Exception exc )
    {
      SPW.logger.Log( "Couldn't connect to server!", LogMessageType.Error, OutputDevice.ScreenAndFile );
      SPW.logger.Log( "Exception text: " + exc.Message, LogMessageType.Error, OutputDevice.ScreenAndFile );
      SPW.sw[ "connectFail" ] = new StringItem( "Connection failed.  Is the server up?", Color.Red );
      SPW.gameState = GameState.TitleScreen ;
      return;
    }
    listenerThread = new Thread( TestNetworkListen );
    listenerThread.Start();

    SPW.logger.Log( "-- BEGIN TEST --", LogMessageType.Info, OutputDevice.File );
    
    SPW.gameState = GameState.Testing;
    SPW.netState = NetState.Waiting;

    // clear the old result text, if there
    SPW.sw[ "testResult1" ] = new StringItem();
    SPW.sw[ "testResult2" ] = new StringItem();


    // fire off empty starter message
    Send( new Message() );
    #endregion
  }


  /// <summary>
  /// Receives messages from network and simply
  /// tallies up average delay (round trip time)
  /// for each message as it comes in.
  /// </summary>
  private void TestNetworkListen()
  {
    #region test network listener function
    byte[] buf = new byte[ MAX_PACKET_SIZE ];
    float avgSize = 0;
    int totalChunks = 0;
    int totalSize = 0;
    while( true )
    {
      int bytesRead = 0;
      try
      {
        bytesRead = socket.Receive( buf );

        // the test has started.
        SPW.netState = NetState.Connected ;
        SPW.sw[ "teststart" ] = new StringItem();
      }
      catch( Exception exc )
      {
        SPW.logger.Log( "Couldn't receive on socket", LogMessageType.Warning, OutputDevice.ScreenAndFile );
        SPW.logger.Log( "Exception text: " + exc.Message, LogMessageType.Warning, OutputDevice.ScreenAndFile );

        // abort the test
        StopTest();

        // This listener thread must die
        Thread.CurrentThread.Abort();
      }

      if( bytesRead == 0 )
      {
        // the server cut us off!
        Console.WriteLine( "The server has disconnected you!" );
        SPW.sw[ "listenerError" ] = new StringItem( "The server has disconnected you!", 40, 400, 4.0f, Color.Red );

        // abort the test
        StopTest();

        // This listener thread must die
        Thread.CurrentThread.Abort();
      }
      if( bytesRead > networkTest_largestMessage )
      {
        networkTest_largestMessage = bytesRead;
        SPW.logger.Log( "new record: " + networkTest_largestMessage, LogMessageType.Info, OutputDevice.File );
      }
      totalChunks++;
      totalSize += bytesRead;
      avgSize = (float)totalSize / totalChunks;
      int expectedSize = Message.Size;

      if( bytesRead != expectedSize )
      {
        SPW.logger.Log( "Test:  TCP:  mashed more than 1 message together.  I read " + bytesRead + " but expected " + expectedSize, LogMessageType.Info, OutputDevice.File );
      }
      for( int i = 0; i < bytesRead; i += expectedSize )
      {
        // get a hunk of 'expectedSize' bytes from the
        // collection of received bytes
        byte[] messageBytes = new byte[ expectedSize ];
        Buffer.BlockCopy( buf, i, messageBytes, 0, expectedSize );

        // Create a Message struct from those bytes
        Message receivedMessage = Message.FromBytes( messageBytes );

        netLogger.Info( "Got a message " + receivedMessage );
        if( receivedMessage.cmd == NetMessageCommand.GameStart )
          Controller.myNetgamePlayerNumber = receivedMessage.playerNumber;

        if( receivedMessage.playerNumber == Controller.myNetgamePlayerNumber )
        {
          // Only count your own messages, because you don't know when the other guy's were sent.
          networkTest_DelayMetrics.LastMessageTransportTime = networkTest_FrameCount - receivedMessage.frame;
        }


      }


      SPW.sw[ "frameCountTest_msg" ] = new StringItem( "Testing network, please wait...", StringItem.Centering.Horizontal, 150, 1.0f );
      SPW.sw[ "frameCountTest_FC" ] = new StringItem( "FC: " + networkTest_FrameCount + "/" + networkTest_TestLength, Color.Red, 1.0f );


      SPW.logger.Log( "current delay: " + networkTest_DelayMetrics.averageMessageDelay + " / avgSize" + avgSize, LogMessageType.Info, OutputDevice.File );
    }
    #endregion
  }


  /// <summary>
  /// Sends out network test messages
  /// </summary>
  public void TestNetwork()
  {
    #region send out network test messages
    Message TestMsg = new Message();

    TestMsg.cmd = NetMessageCommand.IncreaseThrust;
    TestMsg.frame = networkTest_FrameCount;
    TestMsg.playerNumber = Controller.myNetgamePlayerNumber;

    // method #1:  just send on this thread
    // works well as long as socket "always seems ready",
    // which is the case when on localhost.
    // The simple SendAsync below however actually 
    // performs __much__ better in my tests.
    //////socket.Send( TestMsg.GetBytes() );

    // method #2:  works well
    // This way works very well because its _like_ sending
    // with UDP, in that the main thread (this thread) won't
    // block up in the event that the server is not "ready
    // to receive".  The block can be very short (~10ms) but
    // its enough to give the game a "chunking" feel when the
    // network connection isn't ideal.
    

    // 60% chance to actually send, to kind of
    // make the test more realistic (sending 100% of
    // messages (1 message / frame ) really isn't how
    // the game operates)
    if( SPW.rand.NextDouble() < 0.6 )
    {
      SocketAsyncEventArgs saea =  new SocketAsyncEventArgs( );
      saea.SetBuffer( TestMsg.GetBytes(), 0, Message.Size );
      socket.SendAsync( saea );
    }

    #region debug output
    // My verbose debug output
    int startRightX = SPW.world.ScreenWidth - 200;
    int y = 20;

    SPW.sw[ "gameState" ] = new StringItem( "gameState: " + SPW.gameState, startRightX, y += 20, 1.0f, Color.Gray );
    SPW.sw[ "netState" ] = new StringItem( "netState: " + SPW.netState, startRightX, y += 20, 1.0f, Color.Gray );

    SPW.sw[ "teststring" ] = new StringItem( "avg delay: " + networkTest_DelayMetrics.averageMessageDelay, startRightX, y += 20, 1.0f, Color.Gray );
    SPW.sw[ "teststring1" ] = new StringItem( "last transit: " + networkTest_DelayMetrics.LastMessageTransportTime, startRightX, y += 20, 1.0f, Color.Gray );
    SPW.sw[ "teststring2" ] = new StringItem( "sum delay: " + networkTest_DelayMetrics.TotalFrameDelay, startRightX, y += 20, 1.0f, Color.Gray );
    SPW.sw[ "teststring3" ] = new StringItem( "total msgs: " + networkTest_DelayMetrics.TotalMessagesReceived, startRightX, y += 20, 1.0f, Color.Gray );
    #endregion

    networkTest_FrameCount++;
    if( networkTest_FrameCount > networkTest_TestLength )
    {
      StopTest();
    }
    #endregion
  }


  /// <summary>
  /// Stops the network test.
  /// </summary>
  public void StopTest()
  {
    // Compute the average roundtrip delay for a message
    // to get from my computer, to the server, and back again.
    float timeBetweenFrames = (float)Controller.game.TargetElapsedTime.Ticks / TimeSpan.TicksPerSecond;
    string avgDelay = "avg roundtrip: " + networkTest_DelayMetrics.averageMessageDelay + " frames==" + ( networkTest_DelayMetrics.averageMessageDelay * timeBetweenFrames ).ToString() + " seconds";

    SPW.logger.Log( "*** TEST ENDED ***", LogMessageType.Info, OutputDevice.File );
    SPW.logger.Log( avgDelay, LogMessageType.Info, OutputDevice.ScreenAndFile );
    SPW.logger.Log( "largest message: " + networkTest_largestMessage + " bytes", LogMessageType.Info, OutputDevice.ScreenAndFile );

    SPW.sw[ "testResult1" ] = new StringItem( avgDelay, StringItem.Centering.Horizontal, 240, 20.0f, Color.Red );
    SPW.sw[ "testResult2" ] = new StringItem( "largest message: " + networkTest_largestMessage + " bytes", StringItem.Centering.Horizontal, 260, 20.0f, Color.Red );

    SPW.gameState = GameState.TitleScreen;
    SPW.netState = NetState.Disconnected;
    this.ResetNetworkConnection();
  }
  #endregion





}