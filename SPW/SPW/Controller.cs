#region using...
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;
using System.Text;
#endregion

public class Controller : GameComponent
{
  #region controller state variables
  // keep BOTH the previous keyboard state (last frame)
  // and PRESENT keyboard state (this frame).
  // Reason explained at end of this file,
  // near the JustPressed() function
  private KeyboardState kbPrevState, kbCurrState;
  private MouseState msPrevState, msCurrState;

  private Dictionary<PlayerIndex, GamePadState> gpPrevStates, gpCurrStates;
  private List<PlayerIndex> pis; // connected controllers.
  #endregion

  public static SPW game;

  public NetworkListener netConn;

  #region database stuff

  // ### You can add more variables to do with
  // the database connection here
  
  /// <summary>
  /// Tells if logged in or not.
  /// </summary>
  public bool loggedIn ;

  #region code that runs after the user clicks 'ok' or 'cancel' on the "connect" dialog box
  private void RunAfterClickOK()
  {
    // ### Your part.  Database query to permit login.
    bool loginSuccessful ;
    
    // if( user exists in database ) {
        loginSuccessful = true; //!! You actually have to
    // }
    // make a database query here, before setting
    // this loginSuccessful variable to TRUE.





    // Now, if the login WAS successful, then
    // we can let him connect
    // to the actual server to play.
    if( loginSuccessful == true )
    {
      SPW.logger.Log( "Logged in as user=" +
        SPW.windowing.loginDialog.EnteredUsername +
        " pw=" + SPW.windowing.loginDialog.EnteredPassword,
        LogMessageType.Info,
        OutputDevice.ScreenAndFile );

      loggedIn = true;
    }
    else
    {
      SPW.logger.Log( "Login failed for user='" +
        SPW.windowing.loginDialog.EnteredUsername +
        "' pw='" + SPW.windowing.loginDialog.EnteredPassword + "'.  Try again ninny.", LogMessageType.Error, OutputDevice.ScreenAndFile );
    }



    // The person is logged in!  Now let them
    // connect to one of our many game servers in order
    // to actually play as (whoever they successfully logged in as)
    if( loggedIn == true )
    {
      // Reset game internals
      game.ResetGameInternals();

      // try and launch the connection
      if( netConn.Connect() )
      {
        // set game into netgame mode
        SPW.gameState = GameState.NetGame;
        SPW.netState = NetState.Waiting;
      }
      else
      {
        SPW.logger.Log( "Couldn't connect to the server!", LogMessageType.Error, OutputDevice.ScreenAndFile );
        SPW.sw[ "connectFail" ] = new StringItem( "Connection failed.  Is the server up?", Color.Red );
      }
    }
  }

  /// <summary>
  /// Just here for demonstration purposes
  /// </summary>
  private void RunAfterClickCancel()
  {
    SPW.logger.Log( "You mean you don't wanna play online?", LogMessageType.Info, OutputDevice.Screen );
    SPW.logger.Log( "The user has clicked CANCEL.  You could do anything else here, but you don't really have to.", LogMessageType.Info, OutputDevice.File );
  }
  #endregion
  
  #endregion // database stuff

  #region networked game variables
  /// <summary>
  /// Each message we get from across the network will
  /// be frame stamped with the frame at which that ACTION
  /// (fire torpedo) occurred according to the player
  /// who committed the action.
  /// 
  /// To maintain SYNC between our machine and their machine
  /// we must process the inputs from the remote player
  /// AT THE EXACT SAME FRAME on both machines.
  /// 
  /// We can't expect that if someone shoots a torpedo
  /// in frame 50, for us to be able to process it
  /// in both machines by frame 51.  No.  Its more like
  /// on one machine, the message will get there at
  /// frame 57, and on the other machine, frame 54.
  /// 
  /// So, we need to establish the number of frames to
  /// WAIT before "working a message into" the game engine.
  /// 
  /// Here I'm just setting it at 10.  Really this number
  /// should be like determined by the quality of the network
  /// connection and should be adjusted throughout gameplay (set it
  /// lower when the connection is good, set it higher when the
  /// connection is getting laggy)
  /// </summary>
  public static int NETWORK_DELAY = 10 ; // use a higher number like 24 for more laggy server

  /// <summary>
  /// SHARED with listenerThread in NetworkListener.cs.
  /// The first-stage incoming message queue.  
  /// 
  /// The NetworkListener class will listens
  /// for incoming Message objects from the other player,
  /// then it adds them to this queue here.
  /// 
  /// The CHECK_MESSAGES() method of the Controller (in this file)
  /// will then PULL the messages from the 'incoming' queue
  /// and very quickly copy them to the 'processing' queue.
  /// 
  /// The reason we want to COPY the messages to the 'processing'
  /// queue is to minimize the amount of 'contact-time' we have with
  /// the 'incoming' queue.
  ///
  /// The Controller.cs file then takes its sweet time (several frames!)
  /// working through and processing each and every message
  /// that has come in.  We don't want to sit on the incoming
  /// queue for a large number of CPU cycles at all because ONLY ONE THREAD can use
  /// the 'incoming' queue at a time.
  /// </summary>
  public volatile static List<Message> incoming = new List<Message>();

  /// <summary>
  /// This is the 'processing' list.  When messages come in
  /// from the other player from across the network, they
  /// first go into the 'incoming' queue, which is a List object
  /// that gets shared with the listenerThread.
  /// 
  /// Messages might sit in the 'processing' list for several
  /// frames.  See discussions/comments in the functions that do
  /// the processing for an explanation.
  /// </summary>
  public static List<Message> processing = new List<Message>();

  /// <summary>
  /// In a netgame, the player number you are (1 or 2)
  /// </summary>
  public static int myNetgamePlayerNumber;

  /// <summary>
  /// This is used as a performance measurement.
  /// Measures stuff like average message transit time, etc.
  /// </summary>
  public static DelayMetrics delayMetrics = new DelayMetrics();

  /// <summary>
  /// Running indicator of how many FRAMES YOU ARE AHEAD
  /// the guy you're playing with.  If this value is
  /// negative, then you are BEHIND.  Updates every FRAME,
  /// based on last SYNC message received.  Displayed in debug output.
  /// </summary>
  public static int framesAhead ;

  /// <summary>
  /// The frame the other guy was on at
  /// his last SYNC message to us.
  /// 
  /// We can prevent ourselves from running too
  /// far ahead by making sure we DO NOT process
  /// more than NETWORK_DELAY frames ahead of this
  /// number.
  /// 
  /// Because we're using TCP, this is pretty much
  /// a guarantee that the game shouldn't fall out of
  /// sync at all, since neither simulation will
  /// be allowed to run ahead enough frames to
  /// allow an "expired message" to be passed __at all__.
  /// </summary>
  public static int frameOtherGuyWasOnAtLastSync ;
  #endregion

  #region Controller constructor and Update/Check method
  /// <summary>
  /// Constructor
  /// </summary>
  public Controller( Game g ) : base( g )  // call base class ctor
  {
    // save reference to the main game itself
    game = this.Game as SPW;



    netConn = new NetworkListener();



    //find number of controllers attached.
    int nConn = 0;

    Array players = Enum.GetValues( typeof( PlayerIndex ) );
    pis = new List<PlayerIndex>();

    gpPrevStates = new Dictionary<PlayerIndex, GamePadState>();
    gpCurrStates = new Dictionary<PlayerIndex, GamePadState>();

    // Prepare to track states
    // for player controllers
    // that are connected.
    foreach( PlayerIndex pi in players )
    {
      if( GamePad.GetState( pi ).IsConnected )
      {
        nConn++;
        pis.Add( pi );

        gpPrevStates.Add( pi, new GamePadState() );
        gpCurrStates.Add( pi, new GamePadState() );
      }
    }
  }

  // Since this is a GameComponent,
  // Update will AUTOMATICALLY be called
  // by the framework when we .Add this
  // to the Components collection of
  // our Game.
  public override void Update( GameTime gameTime )
  {
    // Update current states for mouse,
    // gamepads, keyboard.
    kbCurrState = Keyboard.GetState();
    foreach( PlayerIndex pi in pis )
      gpCurrStates[ pi ] = GamePad.GetState( pi );
    msCurrState = Mouse.GetState();

    this.Check();

    // Update prev states for mouse,
    // gamepads, keyboard.
    kbPrevState = kbCurrState;
    foreach( PlayerIndex pi in pis ) // each connected GamePad
      gpPrevStates[ pi ] = gpCurrStates[ pi ];
    msPrevState = msCurrState; //mouse

    base.Update( gameTime );
  }

  public void Check()
  {
    if( this.JustPressed( Keys.Escape ) )
      game.Exit();

    if( this.JustPressed( Keys.D5 ) )
      game.OpenContainingFolder();

    if( this.JustPressed( Keys.D6 ) )
      game.OpenLogFileFolder();

    if( this.JustPressed( Keys.D7 ) )
      game.OpenLogFile();

    if( this.JustPressed( Keys.D8 ) )
    {
      game.ToggleDebug();

      SPW.sw["debugmsg"] = new StringItem( "Debug messages " + (SPW.showDebug?"on":"off"), StringItem.Centering.Horizontal, 250, StringItem.DEFAULT_LIFETIME, Color.Gray ) ;
    }

    if( this.JustPressed( Keys.D9 ) )
      game.ScreenShot = true; // take a screenshot next frame.

    if( this.JustPressed( Keys.OemMinus ) )
      SPW.logger.DeleteOldestMessage();

    if( this.JustPressed( Keys.Enter ) )
      SPW.sw.ReactivateLastDeactivated();



    // Run Controller input stuff
    // We run a different function depending
    // on what STATE the game is in.

    // If we're at the title screen, then
    // pressing SPACE resets the game.

    // if we're in-game, then the input
    // keys respond to player inputs etc.
    switch( SPW.gameState )
    {
      case GameState.LocalGame:
        // we are in-game
        RunGameControls();
        break;

      case GameState.NetGame:
        // running a network game
        RunNetGameControls();
        break;

      case GameState.TitleScreen:
        // we are at the title screen
        RunTitleScreenControls();
        break;

      case GameState.Testing:
        if( JustPressed( Keys.Back ) )
        {
          netConn.StopTest();
          SPW.gameState = GameState.TitleScreen;
        }
        break;
    }
  }
  #endregion

  #region netgame methods
  private void RunNetGameControls()
  {
    // Start x and y positions for messages
    int msgX = 40 ;
    int msgY = 200 ;

    switch( SPW.netState )
    {
      case NetState.Waiting:

        #region code to execute when in NetState.Waiting network state
        // Here we've connected to the server successfully and
        // we're waiting for another player to connect to play with.

        SPW.sw[ "NetMessage" ] = new StringItem( "You are waiting for another player to connect at the server", msgX, msgY += 20, 0, Color.Wheat );
        SPW.sw[ "NetMessage2" ] = new StringItem( "Press 'Backspace' to cancel", msgX, msgY += 20, 0, Color.Wheat );

        // A message will come in when the game is to start,
        // so we need to be checking for messages here.
        
        // 1.  Check for new messages from server
        CHECK_MESSAGES();

        // We won't do anything else though.
        // Just check for messages, NOT send any,
        // 2.  Send messages indicating player actions at keyboard
        // SEND_MESSAGES();

        // And no need for heartbeat
        // 3.  Send heartbeat
        // HEARTBEAT();

        // because the game really hasn't started yet,
        // and you'd just be sending announcements that
        // you are on frame 0.

        if( JustPressed( Keys.Back ) )
        {
          // player wants to abandon this netgame.

          // Run disconnection clean up
          netConn.Shutdown();

          // Change state to disconnected state, actual
          // kicking of user to titlescreen occurs
          // NEXT iteration of loop,
          
          // (see the NetState.Disconnected case in this switch)
          SPW.netState = NetState.Disconnected ;
        }
        #endregion
        break;

      case NetState.Disconnected:

        #region NetState.Disconnected

        // Log that was disconnected
        SPW.logger.Log( "You were disconnected", LogMessageType.Warning, OutputDevice.ScreenAndFile );

        // Player lost his network connection.  So,
        // he's no longer part of a NetGame, so
        // CHANGE GAMESTATE from GameState.NetGame to
        // GameState.TitleScreen immediately.
        SPW.gameState = GameState.TitleScreen;
  
        // Once we change the gameState from
        // GameState.NetGame to GameState.TitleScreen,
        // this function ( RunNetGameControls() ) won't run anymore.

        // So the "You were disconnected" line of code
        // only happens once.
        
        // When there is an attempt to connect again
        // netState gets immediately set to NetState.Waiting,
        // not NetState.Disconnected
        #endregion
        break;

      case NetState.Connected:

        #region NetState.Connected
        // Run the net game.
        // Running the net game has 3 steps for the controller.
        
        // 1. check for new messages and process them
        CHECK_MESSAGES();

        // 2.  check my local keyboard for inputs, if
        // any, framestamp and send them
        SEND_MESSAGES();

        // 3. sends an additional SYNC message every (2) frames
        // so the other player can know if he is AHEAD
        // and if he needs to "wait up"
        HEARTBEAT();
        #endregion
        break;

      
      case NetState.TooFarAhead:

        #region NetState.TooFarAhead
        // In this case, we have detected that your computer is
        // running too fast and you're basically running through
        // frames at a higher rate than the other guy.

        // To maintain synchronization, we'll force you to slow down.
        // We also give you the option to quit using the 'backspace' key.

        SPW.sw[ "NetMessage" ] = new StringItem( "Waiting for the other player...", msgX, msgY += 20, 0, Color.Gray );
        SPW.sw[ "NetMessage2" ] = new StringItem( "Press 'backspace' to drop out of this game", msgX, msgY += 20, 0, Color.Gray );

        // Only check for messages.  We need to check
        // for a new SYNC message, which will basically
        // tell us the other guy is back online.
        CHECK_MESSAGES() ;

        // Give the player the option to quit.
        if( JustPressed( Keys.Back ) )
        {

          SPW.logger.Log( "You dropped the game!!", LogMessageType.Info, OutputDevice.ScreenAndFile );
          SPW.sw[ "NetMessage3" ] = new StringItem( "You dropped the game!!", msgX, msgY += 20, 5.0f, Color.Wheat );

          // set up for disconnection in the next frame
          SPW.netState = NetState.Disconnected;

        }
        #endregion
        break;

      case NetState.TooFarBehind:

        #region NetState.TooFarBehind
        // This player poses a threat to the game sync.
        // A lagging player, if he lags TOO FAR, can
        // really spoil game synchronization by sending
        // an [[ "expired message" ]] (a message that is frame stamped
        // with a frame that is too far in the past for the
        // other player to process.. e.g. I am on frame 60,
        // you are on frame 45, you send me a message that
        // is meant to be processed by frame 55, but I cannot
        // process it by the time I receive it, because I
        // am already on frame 60!)

        SPW.sw[ "NetMessage" ] = new StringItem( "You are lagging...", msgX, msgY += 20, 0, Color.Tomato );
        SPW.sw[ "NetMessage2" ] = new StringItem( "Press 'backspace' to drop out of this game", msgX, msgY += 20, 0, Color.Wheat );


        // So, we basically run a restricted version
        // of the code that runs under NetState.Connected:
        // 1.  Check for messages
        CHECK_MESSAGES();

        // 2.  DO NOT SEND MESSAGES, because you are lagging
        //     you threaten to desynch the game by sending
        //     an [[ "expired message" ]] as explained above
        //SEND_MESSAGES();  // Just a precaution.

        // 3.  Send the heartbeat, to tell the player once
        //     we've caught up (he should see TooFarAhead
        //     when we see TooFarBehind)
        HEARTBEAT();

        // Give the player the option to quit.
        if( JustPressed( Keys.Back ) )
        {

          SPW.logger.Log( "You dropped the game!!", LogMessageType.Info, OutputDevice.ScreenAndFile );
          SPW.sw[ "NetMessage3" ] = new StringItem( "You dropped the game!!", msgX, msgY += 20, 5.0f, Color.Wheat );

          // set up for disconnection in the next frame
          SPW.netState = NetState.Disconnected;

        }
        #endregion
        break;
    }
  }

  /// <summary>
  /// Check if there are any new messages from
  /// the server in the Controller.incoming queue.
  /// 
  /// If there are, each message is copied to
  /// the 'processing' queue, and then each
  /// message in the 'processing' queue is
  /// executed IF it is "time" for it to be executed.
  /// </summary>
  private void CHECK_MESSAGES()
  {
    // In a net game, the INPUTS are basically network packets.

    // Are there any ready for us to process?

    // LOCK the incoming queue, so the other thread can't
    // add to it while we're trying to process its messages (the
    // other thread will simply have to wait!)
    #region copy messages from Controller.incoming
    lock( Controller.incoming )
    {
      // now pull out ALL the messages into another queue,
      // waitingToProcess

      for( int i = 0; i < Controller.incoming.Count; i++ )
      {
        // Rapidly copy each message from Controller.incoming
        // to processing.  Notice how I am NOT trying to process/
        // interpret meaning of messages here:  that would lock
        // down the 'Controller.incoming' queue for far too long.
        processing.Add( Controller.incoming[ i ] );  //rapid copy

        // Now measure the delay metrics.  Done here
        // because each message is guaranteed to pass
        // through here ONLY ONCE.
        if( incoming[ i ].playerNumber == myNetgamePlayerNumber )
        {
          // only count my own messages, because the other player may be ahead in frames,
          // which screws up the metric with negative numbers
          delayMetrics.LastMessageTransportTime = SPW.currentFrame - incoming[ i ].frame ;
        }
      }

      // Now that we have captured all the messages from incoming,
      // CLEAR IT OUT, so more messages can be placed in it
      // by the listener thread in the netConn object
      Controller.incoming.Clear();
    } // unlock the incoming queue
    #endregion

    // Now that all the new messages have
    // been pulled in from the network onto
    // the 'processing' list, we must sort it.
    #region why you should sort the 'processing' list
    // The list must be sorted.  This is another source of desync
    // because, SAY we have hyperspace happening for both players
    // at the same frame

    // Frame 1211:  Player 1 HYPERSPACE
    // Frame 1211:  Player 2 HYPERSPACE

    // Because each player just adds his own actions directly
    // his own local Controller.incoming queue, on player 1's machine,
    // player 1's hyperspace will be processed first:

    // __Player 1's machine__:
    // Frame 1211:  Player 1 HYPERSPACE  [ done first ]
    // Frame 1211:  Player 2 HYPERSPACE  [ done second ]

    // But on player 2's machine, player 2's hyperspace gets processed first.

    // __Player 2's machine__:
    // Frame 1211:  Player 2 HYPERSPACE  [ done first ]
    // Frame 1211:  Player 1 HYPERSPACE  [ done second ]
    
    // SO, why is this a big problem that causes desync?
    
    // It goes back to the pseudorandom number generator.
    
    // Say we "seed" the random number generator with '5'.
    // Then, on BOTH machines, the first 10 random digits
    // pulled out will be:



    // 0.5, 0.71, -0.3, 0.11, 0.2, 0.67, -0.4, 0.099, 0, 0.1

    // Just remember the first 4 numbers there for a minute.
    
    // Now because Hyperspace pulls numbers from the random number
    // generator (which we took care to SEED with the same number, I
    // think I used 5 at the beginning of the netgame), we are
    // basically COUNTING ON the random number generator to give
    // the same sequence of random numbers on both machines.

    // So, it should be clear why not having the same order
    // of hyperspacing will desync the game:

    // __Player 1's machine__:
    // Frame 1211:  Player 1 HYPERSPACE  [ gets random velocity vector ( 0.5, 0.71 ) ]
    // Frame 1211:  Player 2 HYPERSPACE  [ gets random velocity vector ( -0.3, 0.11 ) ]

    // __Player 2's machine__:
    // Frame 1211:  Player **2** HYPERSPACE  [ gets random velocity vector ( 0.5, 0.71 ) ]
    // Frame 1211:  Player 1 HYPERSPACE  [ gets random velocity vector ( -0.3, 0.11 ) ]

    
    // The random numbers that come out of the random number generator
    // will be the SAME, yes, but because on player 1's machine, player 1
    // is getting the ( 0.5, 0.71 ) values for his hyperspace velocity vector
    // and on player 2's machine, player 2 is getting ( 0.5, 0.71 ),
    // so now the game has fallen out of sync.

    // To fix this, we must guarantee THE EXACT SAME ORDERING of message processing
    // That is, we're going to go ahead and use a sorting algorithm so that
    // it will be GUARANTEED that player 1's hyperspace ALWAYS gets processed FIRST,
    // THEN player 2's hyperspace.  in this way, the random numbers will be assigned
    // TO THE SAME PLAYER, and so we'll have:

    // __Player 1's machine__:
    // Frame 1211:  Player 1 HYPERSPACE  [ gets random velocity vector ( 0.5, 0.71 ) ]
    // Frame 1211:  Player 2 HYPERSPACE  [ gets random velocity vector ( -0.35, 0.11 ) ]

    // __Player 2's machine__:
    // Frame 1211:  Player 1 HYPERSPACE  [ gets random velocity vector ( 0.5, 0.71 ) ]
    // Frame 1211:  Player 2 HYPERSPACE  [ gets random velocity vector ( -0.35, 0.11 ) ]
    #endregion

    // Sorting the 'processing' list.
    processing.Sort();  // See the CompareTo method in Message class for
    // explanation of how this is made to work correctly.  'processing'
    // is a list full of Message structs, and instructions to the .NET
    // framework about HOW TO SORT an array of Message structs is in
    // the CompareTo method of the Message struct.


    spewList();  // see the list in debug output.  this makes the file huge, but
    // wanna see it anyway.  whenever a desync bug occurs, immediately
    // quit and you can view the "train" of messages to see what it might have been.
    

    // Now, we need to go through the processing queue and
    // execute ALL the message structs that are "ready" to
    // be executed
    for( int i = 0; i < processing.Count; i++ )
    {
      Message currentMessage = processing[i] ;

      // check if it is "TIME" to process this message or not.

      int targetFrameToProcess = currentMessage.frame + NETWORK_DELAY;

      #region crucial synching stuff - NETWORK_DELAY explained and the reason we don't process messages IMMEDIATELY
      // A message to fire a torpedo marked frame 50
      // (basically indicating that the player pressed the
      // key to fire a torpedo AT frame 50) should only
      // be PROCESSED when BOTH player's machines reach frame 60.
      // 50 + (NETWORK_DELAY=10) == 60.

      // But WHY?

      // Both (our machine) AND the other player's machine MUST process
      // the message at the EXACT same frame number
      // otherwise we will fall out of sync.

      // This is hard to explain, but basically if we're trying
      // to get the game simulation to run in 'lockstep', we
      // have to guarantee that all player 'actions' (torpedo firings,
      // engine thrust, rotation) occur in the EXACT SAME FRAME on
      // our local machine and on the remote machine.

      // Think about it.  What if on frame 10, the game looks like this:

      //    D       C
      // Where D is one ship and C is the other.
      // (Just pretend you can see that they are facing each other)

      // Two players are playing together on the same machine in this
      // example, so there's no delay to speak of.

      // So, say D fires a torpedo at frame 10.
      // C fires a torpedo at frame 16.

      // Frame 10:
      //    D>      C
      // Frame 11:
      //    D >     C
      // Frame 12:
      //    D  >    C
      // Frame 13:
      //    D   >   C
      // Frame 14:
      //    D    >  C
      // Frame 15:
      //    D     > C

      // Frame 16: (Player C fires torpedo just in time, torpedos neutralize each other, nobody gets hit.)
      //    D      xC

      // Frame 17:
      //    D       C

      // Now introduce a network connection.  Now compensate
      // to the fact that it will take at least 5 frames of
      // processing time for a message to __make its way__ across
      // the internet.. i.e. 5 frames of processing time before
      // player C HEARS ABOUT the fact that player D fired a missile...

      // If we simply executed messages immediately, here's what might happen...

      // PLAYER 1:                        PLAYER 2:
      // Frame 10: ( D fires torpedo )    Frame 10: ( hasn't 'heard' D fired yet )
      //    D>      C                        D       C
      // Frame 11:                        Frame 11:
      //    D >     C                        D       C
      // Frame 12:                        Frame 12:
      //    D  >    C                        D       C
      // Frame 13:                        Frame 13:
      //    D   >   C                        D       C
      // Frame 14:                        Frame 14:
      //    D    >  C                        D       C
      // Frame 15:                        Frame 15: ( C receives message that D fired torpedo, which really happened 5 frames ago, according to D )
      //    D     > C                        D>      C
      // Frame 16:                        Frame 16: ( C fires his own torpedo, sends message, which won't get to D until 5 frames from NOW )
      //    D      >C                        D >    <C
      // Frame 17: (player C dies)        Frame 17:
      //    D       *                        D  >  < C
      // Frame 18:                        Frame 18:
      //    D                                D   ><  C
      // Frame 19:                        Frame 19: (torpedos neutralize)
      //    D                                D       C
      // Frame 20:                        Frame 20:
      //    D                                D       C
      // Frame 21: ( C fires torpedo? )   Frame 21:
      //    D                                D       C
      // Frame 22:                        Frame 22:
      //    D                                D       C

      // Frame 21, player D gets the message that C tried
      // to fire a torpedo, but according to D's reckoning,
      // C is already dead.  Desync happens.



      // So to allow for the message to get across the 
      // network, we arbitrarily assigned a NETWORK_DELAY
      // of 10 frames.  If the connection was worse, we'd
      // need to increase the value of NETWORK_DELAY, because
      // messages would need more time to propagate across the internet.

      // It'd be IMPOSSIBLE for BOTH machines to execute a message
      // that was created frame 50 AT frame 50, so we have to
      // project a reasonable frame in the future to "work that message into"
      // the game engine at BOTH clients.
      
      // Try fiddling around with the value of the NETWORK_DELAY variable. 
      // Setting it very high ( 40 or 50 ) will make the game respond
      // VERY slowly, because you're basically giving messages an entire
      // 40 frames (0.666 seconds!) to get across the internet to the other machine.
      // That's too slow.  If you set it too low though (e.g. NETWORK_DELAY=5), then
      // the game will keep "waiting" for the other player and
      // the game will get VERY choppy and non-playable.
      #endregion

      
      // So here, we ARE walking thru the list
      // of all messages in the processing queue,
      // but we'll only pull out and process the
      // messages that are 'ripe' (ready to be
      // processed).
      
      // Let's check if it is TIME to process
      // the currentMessage:
      if( currentMessage.cmd == NetMessageCommand.GameStart || // for GameStart message, frame irrelevant
          currentMessage.cmd == NetMessageCommand.Sync || // we ALWAYS process sync messages immediately
          // because its how we COME OUT of the wait state (when the other player
          // finally sends us a SYNC message that says he has caught up in framage)

          targetFrameToProcess == SPW.currentFrame   // it is time!!
        )
      {
        // It is time to process this message.
        PROCESS_NET_MESSAGE_CMD( currentMessage ) ;

        // Now remove it from the queue
        processing.RemoveAt( i ) ;
        i-- ;

        // All messages that don't get processed remain in
        // the 'processing' queue, waiting for their turn
        // to be processed (which will be NETWORK_DELAY frames
        // after their framestamp member.)

        // To SEE the message queue, take a look
        // at the log file!!
      }
      else if( targetFrameToProcess < SPW.currentFrame ) // WE MISSED IT!! OH NOS!!! THE GAME STATE IS CORRUPTED!! GAME OVER MAN!!!!
      {
        #region explaining the oh no part
        // This shouldn't ever really happen,
        // but it CAN.  This is the situation
        // that totally can still spoil game sync,
        // even though we are frame stamping, if
        // we get to this point in the code though,
        // the synchronized game state has been compromised.

        // Here is what is happening, and how we
        // might get here.

        // Set NETWORK_DELAY at 10 frames.  Then,
        // say player 2 lags quite a bit, by 8 frames:
        
        // Player 1 is at frame 90
        // Player 2 is at frame 82

        // (I know that player 2 can't go back in framage,
        // but just work with this idea here.)
        
        // Now what if Player 2 lags a bit more

        // Player 1 is at frame 90
        // Player 2 is at frame 78

        // NOW, Player 2 sends a torpedo message,
        // while he is at frame 78.

        // Player 1, still at frame 90, gets it,
        // say at frame 92.  Since player 2 sent it
        // at frame 78, player 1 thinks he should
        // process it... at frame 88!  But frame 88
        // already passed for player 1!  So, player 1
        // cannot process this message.

        // We drop the game.
        #endregion

        SPW.logger.Log( "Something really bad happened!!!!  We MISSED processing  " + currentMessage.ToString() +
          " and it is " + SPW.currentFrame + ".  The game has fallen out of sync",
          LogMessageType.Error, OutputDevice.File );

        SPW.sw[ "lostsync" ] = new StringItem( "The game lost sync.", Color.Red ) ;
        SPW.sw[ "lostsync2" ] = new StringItem( "This game ends with no winner.", StringItem.Centering.Horizontal, SPW.world.ScreenHeight / 2 + 20 );

        SPW.netState = NetState.Disconnected ;
      }
    }
  }


  /// <summary>
  /// Listens at input devices (gamepad, keyboard) and
  /// sends messages across the network about how I would
  /// like to control my ship.
  /// </summary>
  private void SEND_MESSAGES()
  {
    Message outgoingMessage = new Message();

    outgoingMessage.frame = SPW.currentFrame;

    outgoingMessage.playerNumber = myNetgamePlayerNumber;

    Ship thisShip ;
    if( myNetgamePlayerNumber == 1 )
      thisShip = SPW.world.player1 ;
    else
      thisShip = SPW.world.player2 ;
    // poll keyboard for input
    // if there is some input, then send
    // off a message with command

    // person not allowed to do anything if
    // the ship he controls is in hyperspace
    #region controls
    if( thisShip.state != ShipState.Hyperspace )
    {
      if( pis.Contains( PlayerIndex.One ) )
      {
        // let 'em use either controller OR keyboard
        if( IsPressed( Keys.Z ) || IsPressed( Keys.NumPad1 ) || IsPressed( PlayerIndex.One, Buttons.LeftShoulder ) )
        {
          outgoingMessage.cmd = NetMessageCommand.TradeShieldForEnergy;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.X ) || IsPressed( Keys.NumPad2 ) || IsPressed( PlayerIndex.One, Buttons.Y ) )
        {
          outgoingMessage.cmd = NetMessageCommand.BeginHyperspace;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.C ) || IsPressed( Keys.NumPad3 ) || IsPressed( PlayerIndex.One, Buttons.RightShoulder ) )
        {
          outgoingMessage.cmd = NetMessageCommand.TradeEnergyForShield;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.A ) || IsPressed( Keys.NumPad4 ) || IsPressed( PlayerIndex.One, Buttons.DPadLeft ) )
        {
          outgoingMessage.cmd = NetMessageCommand.RotateLeft;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.S ) || IsPressed( Keys.NumPad5 ) || IsPressed( PlayerIndex.One, Buttons.B ) )
        {
          outgoingMessage.cmd = NetMessageCommand.IncreaseThrust;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.D ) || IsPressed( Keys.NumPad6 ) || IsPressed( PlayerIndex.One, Buttons.DPadRight ) )
        {
          outgoingMessage.cmd = NetMessageCommand.RotateRight;
          netConn.Send( outgoingMessage );
        }

        if( JustPressed( Keys.Q ) || IsPressed( Keys.NumPad7 ) || JustPressed( PlayerIndex.One, Buttons.X ) )
        {
          outgoingMessage.cmd = NetMessageCommand.ShootPhasors;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.W ) || IsPressed( Keys.NumPad8 ) || IsPressed( PlayerIndex.One, Buttons.RightTrigger ) )
        {
          outgoingMessage.cmd = NetMessageCommand.Cloak;
          netConn.Send( outgoingMessage );
        }

        if( JustPressed( Keys.E ) || IsPressed( Keys.NumPad9 ) || JustPressed( PlayerIndex.One, Buttons.A ) )
        {
          outgoingMessage.cmd = NetMessageCommand.ShootTorpedos;
          netConn.Send( outgoingMessage );
        }
      }
      else
      {
        // player controls strictly from keyboard
        
        // A player might hold down multiple keys simultaneously,
        // so a player might need to send MORE THAN one message
        // per frame.

        // So in this series of it statements, we see which keys
        // are down, and we fire off a seperate message for each
        // "action" the player wishes to perform.

        // The first 2 fields ( outgoingMessage.frame and
        // outgoingMessage.playerNumber ) remain the same
        if( IsPressed( Keys.Z ) || IsPressed( Keys.NumPad1 ) )
        {
          // Set the command of the outgoingMessage to
          // 
          outgoingMessage.cmd = NetMessageCommand.TradeShieldForEnergy;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.X ) || IsPressed( Keys.NumPad2 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.BeginHyperspace;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.C ) || IsPressed( Keys.NumPad3 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.TradeEnergyForShield;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.A ) || IsPressed( Keys.NumPad4 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.RotateLeft;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.S ) || IsPressed( Keys.NumPad5 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.IncreaseThrust;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.D ) || IsPressed( Keys.NumPad6 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.RotateRight;
          netConn.Send( outgoingMessage );
        }

        if( JustPressed( Keys.Q ) || IsPressed( Keys.NumPad7 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.ShootPhasors;
          netConn.Send( outgoingMessage );
        }

        if( IsPressed( Keys.W ) || IsPressed( Keys.NumPad8 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.Cloak;
          netConn.Send( outgoingMessage );
        }

        if( JustPressed( Keys.E ) || JustPressed( Keys.NumPad9 ) )
        {
          outgoingMessage.cmd = NetMessageCommand.ShootTorpedos;
          netConn.Send( outgoingMessage );
        }
      }
    }
    #endregion
  }

  /// <summary>
  /// Sends a message announcing what frame we're on
  /// to the other player.
  /// </summary>
  private void HEARTBEAT()
  {
    // every (?)th frame.  I seem to like 2.
    if( SPW.currentFrame % ( 2 ) == 0 )
    {
      // create and send the SYNC message
      // information says what frame I am AT 
      // right now.  If "you" are too far ahead
      // then you should wait for me to catch up

      Message syncMessage = new Message();

      // Mark the message with the current frame
      syncMessage.frame = SPW.currentFrame;

      // designate this message as the SYNC message
      syncMessage.cmd = NetMessageCommand.Sync;

      // and just include my player number
      syncMessage.playerNumber = myNetgamePlayerNumber;



      netConn.Send( syncMessage );
    }



    #region crucial synching stuff - making absolutely sure we don't run ahead
    //////////
    // DID I NOT GET A SYNC MESSAGE for a while?
    // WHEN was the last sync message?  What frame
    // was the other guy on?

    // We CANNOT BE ALLOWED TO process more than
    // (frameOtherGuyWasOnAtLastSync + 10)
    // (where NETWORK_DELAY is assumed to be 10)

    // BECAUSE, SAY frameOtherGuyWasOnAtLastSync is 80.
    // And SAY NETWORK_DELAY is 10.  And we're on frame 90.

    // Once we reach frame 91, then ANY MESSAGES
    // that the other guy sends will AUTOMATICALLY
    // be expired.  He sends them framestamped 80,
    // when get here, they will WANT to be processed
    // at frame 90, but we will already have passed that frame.
    // So it will be too late, and we won't be able to process it.

    // So THIS SECTION OF CODE IS THE SAFEGUARD that stops the game
    // from falling out of sync.  Also note that we
    // are dependent on the TCP subsystem to ensure
    // that every message that is sent is going to arrive
    // in order.  If the messages weren't guaranteed to
    // arrive in order, then a system like this would not
    // guarantee sync for you.

    // It is crucial that we ensure we DO NOT step ahead
    // more than NETWORK_DELAY frames of the other player.
    #endregion

    //                      80       +           9         ==     89        // THEN YOU ARE TOO CLOSE
    if( frameOtherGuyWasOnAtLastSync + ( NETWORK_DELAY - 1 ) == SPW.currentFrame )
    {
      // You are running TOO FAR AHEAD.

      // We know this because the other guy HAS NOT sent you his heartbeat (SYNC)
      // message in quite a few frames, so you should stop and wait for him.
      SPW.logger.Log( "You are ahead by heartbeat():  frameOtherGuyWasOnAtLastSync=" + frameOtherGuyWasOnAtLastSync, LogMessageType.Warning, OutputDevice.File );
      SPW.netState = NetState.TooFarAhead;

      // The only thing that will reset SPW.netState = Connected again,
      // is a new SYNC message that indicates the other guy has
      // caught up sufficiently in "framage"
    }
  }

  /// <summary>
  /// Processes all the Message objects that come in
  /// from the network.  Basically executes the
  /// appropriate bit of code based on what type
  /// of message came in from the network.
  /// 
  /// Pass only 'ripe' messages to this function
  /// because any Message you pass here __will be__
  /// executed immediately, regardless of frame stamp!
  /// </summary>
  /// <param name="message">The message object to execute</param>
  private void PROCESS_NET_MESSAGE_CMD( Message message )
  {
    if( message.cmd == NetMessageCommand.GameStart )
    {
      #region handling the GameStart message from the server
      // GameStart is a special message indicating the game should begin.
      SPW.logger.Log( "Game start", LogMessageType.Info, OutputDevice.Screen );

      // Assign the player his ID, which will have come
      // down from the server attached to this 'message' object
      myNetgamePlayerNumber = message.playerNumber;

      // SEED the random number generator with 5 (chosen
      // arbirarily), -- could use ANY other int, just as long as you know
      // its the same seed as the other player is using! )
      // ( Could also have come from server, but we didn't bother )
      SPW.rand = new Random( 5 ) ;
      
      // Start the game
      SPW.netState = NetState.Connected ;

      // Part of sync (hyperspace direction etc) relies on random
      // number generator generating the SAME NUMBERS, IN THE SAME ORDER
      // at client machines.  Because we just seeded the random number
      // generator at the line above, the "list of random numbers" that
      // rand.Next() pulls from should be exactly the same on both machines.
      SPW.logger.Log( "Testing random number generator:  these numbers should be the EXACT same on both clients " + SPW.rand.Next(), LogMessageType.Info, OutputDevice.File );

      SPW.logger.Log( "Started the local sim", LogMessageType.Info, OutputDevice.File );

      SPW.logger.Log( "You are player " + myNetgamePlayerNumber, LogMessageType.Info, OutputDevice.File );
      #endregion

      // that's it for this message.  that's all
      // the information it has.
      return ;
    }
    else if( message.cmd == NetMessageCommand.Sync )
    {
      #region handling the SYNC message
      // The Sync message.  This message is basically
      // one client telling the other client "Hey!  I'm
      // on frame 99.  Don't run too far ahead of me!"

      if( message.playerNumber == myNetgamePlayerNumber )
      {
        // This is my OWN sync message, bouncing off the server
        // So, I must ignore it by returning here, without
        // processing it further.

        return ;
      }

      ////////////////////////////////////////////////////////////////
      // got a sync from the other player, so we can set this now:  //
      frameOtherGuyWasOnAtLastSync = message.frame ;                //
      //                                                            //
      ////////////////////////////////////////////////////////////////
      // Other player has announced what frame he's on,
      // so I keep that number to make sure I DO NOT RUN
      // TOO FAR AHEAD OF HIM.

      #region crucial synching stuff

      // To really get the game to work properly,
      // we have to make sure that both HE (remote player)
      // and I are at ROUGHLY the same frame, for the
      // duration of the game.

      // Why?  If HE falls behind, say 15 frames,
      // e.g. he's on frame 60, and I'm on frame 75.
      // he's sending messages that are "supposed"
      // to be processed by frame 65.  Even if it
      // gets here lightening quick, (my frame 76),
      // I STILL can't process it in time because my
      // entire game was running too far ahead.

      // So, we have to put in place here a few
      // mechanisms to keep the games in relative step.

      // He shouldn't fall behind more than NETWORK_DELAY frames.
      // This is better explained in the "#region crucial synching stuff - NETWORK_DELAY explained"
      // section in the CHECK_MESSAGES() function
      // and also the "#region crucial synching stuff - making absolutely sure we don't run ahead"
      // section in HEARTBEAT() function.

      // Basically here I am trying to figure out how
      // far ahead of the other play in framage I am,
      // and based on that, I'll be able to determine
      // whether I'm too far ahead, too far behind, or
      // in (nearly) perfect sync.

      // The idea is we want to stay as close to (nearly)
      // perfect sync for as much time as is possible.

      // So we compute:

      // (what frame I'm on) - (what frame he's on)

      // e.g.  i'm on 87, and he's on 77
      // then the value is positive: ( 87 - 77 = 10 )
      // which I understand to mean I am 10 frames AHEAD.

      // e.g. 2. i'm on 95, and he's on 105, then
      // 95 - 105 = -10 frames
      // That negative value will be understood to mean
      // that I am 10 frames behind.
      framesAhead = SPW.currentFrame - message.frame ;

      // See if we are 'TooFarAhead':
      if( framesAhead > NETWORK_DELAY/2 )
      {
        // I am TOO FAR ahead, I must WAIT for the other player to
        // catch up in framage

        SPW.logger.Log( "SYNC message:  You are ahead by " + framesAhead + " frames:  You=" +
          SPW.currentFrame + " /Other=" + message.frame + ")",
          LogMessageType.Warning, OutputDevice.File );
        
        SPW.netState = NetState.TooFarAhead ;
      }
      else if( framesAhead < -NETWORK_DELAY/2 )
      {
        // I am TOO FAR BEHIND.  I should disconnect
        // input from myself (the behind player) because
        // I might corrupt the game by sending a packet
        // that has "expired"

        // (e.g. if I am on frame 72, and he's already
        // on frame 85, he CANNOT PROCESS anything I will
        // send (because he would have to have processed it
        // by frame 72+10=82, which has already passed for him))

        SPW.logger.Log( "SYNC message:  You are behind by " + framesAhead + " frames:  You=" + SPW.currentFrame + " /Other=" + message.frame + ")",
          LogMessageType.Warning, OutputDevice.File );

        SPW.netState = NetState.TooFarBehind ;
      }
      else
      {
        // small difference in frame counters
        // "perfect sync"
        SPW.logger.Log( "SYNC message:  Good sync, " + framesAhead + " frames:  You=" + SPW.currentFrame + " /Other=" + message.frame + ")",
          LogMessageType.Info, OutputDevice.File );
        
        SPW.netState = NetState.Connected;
      }

      #endregion
      #endregion

      // DO NOT let processing of the SYNC message go any
      // further.  It shouldn't reach the switch(), because
      // it doesn't need to - its already been handled here.
      // its not part of the switch because we left that part
      // to strictly deal with player ship manipulation messages.
      return ;
    }

    // Next, we process all the other types of
    // messages that indicate player control of the ships,
    // missile firing, etc.
    #region __Ship movement message handling and game state update code__
    // Which player does this network message
    // intend to manipulate?  Notice we don't
    // look at YOUR assigned player ID (SPW.netgamePlayerNumber)
    // that number is what you stamp your OUTGOING messages with.

    // BOTH messages you sent and messages the other player
    // sent will eventually come down through here.
    // Every message that manipulates game state in a netgame,
    // are ALL added to Controller.incoming, and from there,
    // move to the 'processing' queue, and from there,
    // get dispatched to this section of code, where
    // changes to the game state are made.

    // This section of code is THE ONLY section of code
    // that changes the player's positions, missile shootings,
    // etc. when in a netgame mode.
    Ship thePlayer ;
    if( message.playerNumber == 1 )
      thePlayer = SPW.world.player1 ;
    else
      thePlayer = SPW.world.player2 ;
   
    SPW.logger.Log( "PROCESSING " + message.ToString() + " currentFrame=" + SPW.currentFrame, LogMessageType.Info, OutputDevice.File ) ;
    
    switch( message.cmd )
    {
      // 9 actions the player can take
      case NetMessageCommand.TradeShieldForEnergy:
        thePlayer.TradeShieldForEnergy();
        break;

      case NetMessageCommand.BeginHyperspace:
        thePlayer.BeginHyperspace();
        break;

      case NetMessageCommand.TradeEnergyForShield:
        thePlayer.TradeEnergyForShield();
        break;

      case NetMessageCommand.RotateLeft:
        thePlayer.RotateLeft();
        break;

      case NetMessageCommand.IncreaseThrust:
        thePlayer.IncreaseThrust();
        break;
  
      case NetMessageCommand.RotateRight:
        thePlayer.RotateRight();
        break;

      case NetMessageCommand.ShootPhasors:
        thePlayer.ShootPhasors();
        break;

      case NetMessageCommand.Cloak:
        thePlayer.Cloak();
        break;
      
      case NetMessageCommand.ShootTorpedos:
        thePlayer.ShootTorpedos();
        break;

      default:
        SPW.logger.Log("Holy spumoni batman!  There was an invalid message passed.  Details: " + message.ToString(), LogMessageType.Error, OutputDevice.File | OutputDevice.Screen ) ;
        break;
    }
    #endregion
  }
  #endregion

  #region NON-networked/other player control functions
  /// <summary>
  /// Runs when we're at the title screen
  /// </summary>
  private void RunTitleScreenControls()
  {
    if( JustPressed( Keys.Space ) )
    {
      game.ResetGameInternals();
      SPW.gameState = GameState.LocalGame;
    }

    // Runs a test that basically fires off
    // messages on the network really fast.
    // Attempts to measure average round-trip time.
    if( JustPressed( Keys.T ) )
    {
      netConn.StartTest();
    }

    if( JustPressed( Keys.C ) )
    {
      // To connect, we now display the login dialog
      // and only if he authenticates can he actually
      // connect to the server.

      // Notice when we call to display the login dialog,
      // we are telling it WHAT FUNCTION we want executed WHEN
      // the person finishes typing in his name
      // and password, and then clicking OK.

      SPW.windowing.DisplayLoginDialog( RunAfterClickOK, RunAfterClickCancel );

      #region passing a function
      // Does it feel weird to pass a FUNCTION as a
      // parameter to a function?  Well, that is
      // exactly what we're doing here.

      // These functions we are passing
      // are going to be cast as Action objects.  

      // Say there's a function KillPlayer() that
      // kills a player.

      // An 'Action' object is just a reference to a function.
      // If you create an Action object like:

      //    Action a = KillPlayer ; // 1.

      // You already knew that you can execute
      // KillPlayer at any point in the code
      // by typing:
      //    KillPlayer() ;    // 2.

      // But did you know that
      // once you set up the Action a as a
      // REFERENCE to KillPlayer, you
      // could just type:
      //    a();              // 3.  executes KillPlayer

      // and that would execute KillPlayer, since
      // we already set up a = KillPlayer in an earlier line of code (1.)

      // So how is that useful?  Well you pass functions
      // to DisplayWindowDialog, and it uses the stuff
      // described above to save off those function references
      // as Action objects for __later__ execution.

      // note that an Action object only works to reference functions that
      // have BOTH return type VOID __AND__ no arguments.
      // When I say "no arguments", I mean, the function
      // gets called like this KillPlayer(), not
      // like KillPlayer( "arg 1", "arg 2" ) ;
      // The things you pass between the brackets are
      // the arguments.  If you wanted to be able to
      // pass a function with arguments and with
      // a different return type than VOID, you'd
      // need to explore __DELEGATES__, which we'll
      // not go into here.
      #endregion
    }
  }

  // Runs during normal game play
  private void RunGameControls()
  {
    Ship player1 = SPW.world.player1;
    Ship player2 = SPW.world.player2;

    // If the 1p controller is connected, then use it
    if( pis.Contains( PlayerIndex.One ) )
    {
      RunPlayerFromController( PlayerIndex.One, player1 );
    }
    else
    {
      // use the keyboard for player 1

      // player can't control when in hyperspace
      if( player1.state != ShipState.Hyperspace )
      {
        if( IsPressed( Keys.NumPad1 ) )
          player1.TradeShieldForEnergy();

        if( IsPressed( Keys.NumPad2 ) )
          player1.BeginHyperspace();

        if( IsPressed( Keys.NumPad3 ) )
          player1.TradeEnergyForShield();

        if( IsPressed( Keys.NumPad4 ) )
          player1.RotateLeft();

        if( IsPressed( Keys.NumPad5 ) )
          player1.IncreaseThrust();

        if( IsPressed( Keys.NumPad6 ) )
          player1.RotateRight();

        if( JustPressed( Keys.NumPad7 ) )
          player1.ShootPhasors();

        if( IsPressed( Keys.NumPad8 ) )
          player1.Cloak();

        if( JustPressed( Keys.NumPad9 ) )
          player1.ShootTorpedos();
      }
    }



    // Move player 2
    if( pis.Contains( PlayerIndex.Two ) )
    {
      // use control pad #2 for player2
      RunPlayerFromController( PlayerIndex.Two, player2 );
    }
    else
    {
      // use the keyboard for player 2

      // player can't control when in hyperspace
      if( player2.state != ShipState.Hyperspace )
      {
        if( IsPressed( Keys.Z ) )
          player2.TradeShieldForEnergy();

        if( IsPressed( Keys.X ) )
          player2.BeginHyperspace();

        if( IsPressed( Keys.C ) )
          player2.TradeEnergyForShield();

        if( IsPressed( Keys.A ) )
          player2.RotateLeft();

        if( IsPressed( Keys.S ) )
          player2.IncreaseThrust();

        if( IsPressed( Keys.D ) )
          player2.RotateRight();

        if( JustPressed( Keys.Q ) )
          player2.ShootPhasors();

        if( IsPressed( Keys.W ) )
          player2.Cloak();

        if( JustPressed( Keys.E ) )
          player2.ShootTorpedos();
      }
    }
  }



  // Runs EITHER player 1 or player 2 from whatever
  // index controller you want (provided its still connected!)
  private void RunPlayerFromController( PlayerIndex pNum, Ship playerShip )
  {
    // player can't control when in hyperspace
    if( playerShip.state != ShipState.Hyperspace )
    {
      if( IsPressed( pNum, Buttons.LeftShoulder ) )
        playerShip.TradeShieldForEnergy();

      if( IsPressed( pNum, Buttons.Y ) )
        playerShip.BeginHyperspace();

      if( IsPressed( pNum, Buttons.RightShoulder ) )
        playerShip.TradeEnergyForShield();

      if( IsPressed( pNum, Buttons.DPadLeft ) )
        playerShip.RotateLeft();

      if( IsPressed( pNum, Buttons.B ) )
        playerShip.IncreaseThrust();

      if( IsPressed( pNum, Buttons.DPadRight ) )
        playerShip.RotateRight();

      if( JustPressed( pNum, Buttons.X ) )
        playerShip.ShootPhasors();

      if( IsPressed( pNum, Buttons.RightTrigger ) )
        playerShip.Cloak();

      if( JustPressed( pNum, Buttons.A ) )
        playerShip.ShootTorpedos();
    }
  }
  #endregion

  #region other functions
  /// <summary>
  /// Debug spews the message queue to the log file
  /// </summary>
  private void spewList()
  {
    if( processing.Count == 0 )
    {
      ////SPW.logger.Log( "The 'processing' incoming message queue is empty", LogMessageType.Info, OutputDevice.File );
      return;
    }

    // debug spew the message queue
    StringBuilder line1 = new StringBuilder();
    StringBuilder line2 = new StringBuilder();
    StringBuilder line3 = new StringBuilder();

    for( int i = 0; i < processing.Count; i++ )
    {
      line1.Append( processing[ i ].DebugLine( 1 ) );
      line2.Append( processing[ i ].DebugLine( 2 ) );
      line3.Append( processing[ i ].DebugLine( 3 ) );
    }

    SPW.logger.Log( line1.ToString(), LogMessageType.Info, OutputDevice.File );
    SPW.logger.Log( line2.ToString(), LogMessageType.Info, OutputDevice.File );
    SPW.logger.Log( line3.ToString(), LogMessageType.Info, OutputDevice.File );
  }

  public void Shutdown()
  {
    netConn.Shutdown() ;
  }
  #endregion

  #region functions that check for key states

  /// <summary>
  /// Tells you if "key" was JUST PRESSED DOWN.
  /// 
  /// JUST PRESSED DOWN means the key was
  /// UP in previous frame, but is DOWN in
  /// THIS frame.
  /// 
  /// If you press and hold a key, for no matter
  /// how many seconds you hold it down for,
  /// you'll only have JustPressed return true ONCE.
  /// </summary>
  /// <param name="key">The key to check if it was JUST pressed down</param>
  public bool JustPressed( Keys key )
  {
    // See, to determine if a button was JUST
    // pressed, we have to know the key
    // state of all the keys during the PREVIOUS
    // frame of the game.

    // So that is why we keep TWO structs:
    //   kbCurrState is the CURRENT state of 
    //   the keyboard for THIS frame,
    //   kbPrevState REMEMBERS whta the state
    //   of the keyboard was in the LAST frame

    // A key was only JUST PRESSED if it was
    // UP last frame and is DOWN this frame.
    if( kbCurrState.IsKeyDown( key ) &&
        kbPrevState.IsKeyUp( key ) )
      return true;
    else
      return false;
  }


  /// <summary>
  /// Tells you if a button was JUST PRESSED DOWN on
  /// a game pad 
  /// </summary>
  /// <param name="which">The game pad index to check
  /// (PlayerIndex.One checks gamepad #1, etc)</param>
  /// <param name="button">The button to check if it was JUST pressed down</param>
  public bool JustPressed( PlayerIndex which, Buttons button )
  {
    if( gpCurrStates[ which ].IsButtonDown( button ) &&
        gpPrevStates[ which ].IsButtonUp( button ) )
      return true;
    else
      return false;
  }

  /// <summary>
  /// Returns true if a key was just let go of
  /// </summary>
  /// <param name="key">The key to check if it was just let go of</param>
  public bool JustReleased( Keys key )
  {
    if( kbCurrState.IsKeyUp( key ) &&
        kbPrevState.IsKeyDown( key ) )
      return true;
    else
      return false;
  }

  /// <summary>
  /// Returns true if a gamepad's button was just let go of
  /// </summary>
  /// <param name="which">GamePad # to check (PlayerIndex.One checks
  /// Player 1's gamepad)</param>
  /// <param name="button">Button to check if its just been let go of</param>
  public bool JustReleased( PlayerIndex which, Buttons button )
  {
    if( gpCurrStates[ which ].IsButtonUp( button ) &&
        gpPrevStates[ which ].IsButtonDown( button ) )
      return true;
    else
      return false;
  }

  /// <summary>
  /// Tells you if a key is BEING HELD DOWN
  /// </summary>
  /// <param name="key">Key to check if its being held down</param>
  public bool IsPressed( Keys key )
  {
    return kbCurrState.IsKeyDown( key );
  }

  /// <summary>
  /// Tells you if a button on a gamepad is being held down
  /// </summary>
  /// <param name="which">GamePad # to check (PlayerIndex.One checks
  /// Player 1's gamepad)</param>
  /// <param name="button">Button to check if its down</param>
  public bool IsPressed( PlayerIndex which, Buttons button )
  {
    return gpCurrStates[ which ].IsButtonDown( button );
  }
  #endregion

}


#region extra struct for tracking network perf
/// <summary>
/// A structure that's used to keep track of
/// network delay as the networked game progresses.
/// </summary>
public struct DelayMetrics
{
  // lastMessageTransportTime = SPW.currentFrame - Message.frame
  // A measurement (in number of frames) of
  // how long it took for a Message from the remote
  // player to get to THIS machine.
  private int lastMessageTransportTime;

  // A count of the total delay (running sum
  // of lastMessageTransportTime
  private int totalFrameDelay;
  public int TotalFrameDelay{ get { return totalFrameDelay ; } } // protection for totalFrameDelay
  // as it should NOT be set by the user (user should ONLY be setting
  // LastMessageTransportTime, using ITS setter.

  // A count of the total messages received so far.
  // Used to compute average delay
  private int totalMessagesReceived;
  public int TotalMessagesReceived { get { return totalMessagesReceived; } } // protection for totalMessagesReceived
  // as it should NOT be set by the user

  /// <summary>
  /// Update this value with the last message's transport time
  /// in frames.  Automatically keeps track of average of all
  /// message travel times that you have sent here.
  /// </summary>
  public int LastMessageTransportTime
  {
    get
    {
      return lastMessageTransportTime;
    }
    set
    {
      lastMessageTransportTime = value;
      totalMessagesReceived++;
      totalFrameDelay += value;
    }
  }


  public float averageMessageDelay
  {
    get
    {
      return (float)totalFrameDelay / totalMessagesReceived;
    }
  }


}

#endregion






