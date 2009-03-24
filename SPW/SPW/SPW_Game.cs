#region using...
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using WindowSystem;
#endregion



// MAIN GAME FILE
// This file contains the core of the game.

// Do a project-wide search for ###
// to find the database sections you should work on.



#region Note about multi-game_server.py
/*
 * When using the multi-game_server.py file, to host
 * several games in a row, this game sometimes hangs at
 * "You are waiting for another player to connect at the server..."
 *
 * To fix:  Just press backspace and have THE OTHER GUY
 * try connecting first.  Or, you can exit and restart the game.
 * 
 */
#endregion

#region game description
// Destroy the other ship with PHOTON TORPEDOS or
// PHASERS until all its SHEILD energy is gone.

// Weapons:  PHOTON TORPEDOS - Use = 1 unit, Damage = 4 units
// PHASERS: - Use = 1 unit, Damage = 2 units

// Defense:
// IMPULSE ENGINES - Use = 1 unit every 1/2 second
// CLOAK - Use = 1 unit every 1/2 second
// HYPER SPACE - Use = 8 units

// Fire phasers     cloak      fire photons
// rotate ccw       engine     rotate cw
// weapon energy    phase      shield energy

// The version of SPW that this game emulates is located here:
// http://www.dosgames.com/dl.php?filename=http://www.dosgames.com/files/spacewar.zip

#endregion

public class SPW : Microsoft.Xna.Framework.Game
{
  GraphicsDeviceManager graphics;
  SpriteBatch spriteBatch;

  #region public static variable declarations
  // Provides cool windows with text input, 
  // originally by Aaron MacDougall, slightly modded by me
  // original source is located at http://www.codeplex.com/wsx/
  public static Windowing windowing;

  // Disk path for file output dumps
  // (used for log files and screenshots)
  public static string path = ""; //!! change to wherever you want your log files dumped

  public static bool showDebug = true ;

  // Because these are STATIC variables, they
  // are accessible game-wide, by typing in
  // SPW.(static variable name).

  // This is a great help, because it means that
  // these static variables will be initialized ONCE
  // (usually in the game constructor) and they
  // act as superglobal variables that are accessible
  // absolutely anywhere in the game code, simply
  // by typing in SPW.(variable name).

  // I'm sure to assign "public static" to every
  // item in this class that I want to be
  // shared game-wide.


  // Random number generator used game-wide
  public static Random rand = new Random();

  // state of the game.  You're either at the TitleScreen,
  // playing a LocalGame (2 players at same machine)
  // or playing a NetGame (which is what _lab 2_ is about completing!)
  public volatile static GameState gameState;

  // if in a netgame, then this is the state of the networked game.
  public volatile static NetState netState;

  /// <summary>
  /// The global frame counter to keep both game engines synchronized.
  /// We basically want "currentFrame" to match up for both players
  /// as closely as possible as the game proceeds.
  /// </summary>
  public static int currentFrame = 0 ;

  /// <summary>
  /// Static member accessible from anywhere,
  /// using SPW.world.
  /// The World object is meant to contain all
  /// game data.
  /// </summary>
  public static World world;

  /// <summary>
  /// The screenwriter - Also available everywhere,
  /// using SPW.sw.  See examples in code already
  /// written for how to use.
  /// </summary>
  public static ScreenWriter sw;

  /// <summary>
  /// Logs to the screen, a file, or the console.
  /// </summary>
  public static Logger logger ;
  

  // Game component - the controller object.
  // The Controller object contains ALL the code
  // that deals with user input and causes
  // state changes to the game.
  Controller c;
  #endregion

  #region constructor - runs FIRST
  public SPW()
  {
    graphics = new GraphicsDeviceManager( this );
    Content.RootDirectory = "Content";

    this.IsMouseVisible = true;

    #region creating and adding the game component objects Controller, ScreenWriter, Logger, and Windowing
    // instantiate the Controller
    c = new Controller( this );

    // add it as a component so ITS Update()
    // function will AUTOMATICALLY be called
    // everytime this class's does.
    this.Components.Add( c );
    // OK Hoooooold on.  Did that "component"
    // stuff make any sense? 

    // Looking at the definition of
    // the Controller class, notice how it says:

    //   public class Controller : GameComponent

    // This reads "public class Controller is-a GameComponent"

    // So what does that mean?

    // Well, it just means that the Controller promises
    // to have an 'Update()' method within it.  

    // Remember how our XNA game rapidly does the following:

    /*
     
     while( true )
     {
       Update() ; 
       Draw() ; 
     }
     
    */

    // When you add a GameComponent object to the Components
    // collection however, this is kind of (very roughly) what happens:

    /*
     while( true )
     {
       Update() ; 
       foreach( GameComponent component in this.Components )
         component.Update() ;
       
       Draw() ; 
     }
     
    */

    // The Update() and Draw() functions that will get called
    // by the XNA framework, will normally only be the Update()
    // and Draw() functions of THIS class (the main game class).

    // Ok so what's this?

    // Add a few more components.
    sw = new ScreenWriter( this );
    this.Components.Add( sw );

    // Here's one that's a bit different!
    // Notice that the ScreenWriter IS-A "DRAWABLEGAMECOMPONENT"

    //    public class ScreenWriter : DrawableGameComponent

    // So this is KIND OF what happens then:
    /*
     while( true )
     {
       Update() ; 
       foreach( GameComponent component in this.Components )
         component.Update() ;
           
       Draw() ; 
       foreach( DrawableGameComponent drawable in this.Components )
       {
         drawable.Draw() ; 
       }
     }
         
    */

    // This obviously isn't exactly what happens but I think it
    // communicates the idea.

    // Using GameComponents and DrawableGameComponents
    // are great because they tie up ALL the code
    // to do with one "COMPONENT" of the game into one file.


    logger = new Logger( this, true );
    this.Components.Add( logger ) ;

    // Create the Windowing object.
    // The source code for this system
    // by 'Aaron MacDougall', is on codeplex
    // http://www.codeplex.com/wsx/
    windowing = new Windowing( this );
    #endregion

    // Initialize the game state
    gameState = GameState.TitleScreen;  // let game start @ title screen
    netState = NetState.Disconnected;   // 

    // create the world object
    world = new World();

    // set width and height of the backbuffer
    this.graphics.PreferredBackBufferHeight = world.ScreenHeight;
    this.graphics.PreferredBackBufferWidth = world.ScreenWidth;


    // Tell player about how to toggle debug
    Color tBlueViolet = Color.BlueViolet;
    tBlueViolet.A = 0;
    sw[ "more" ] = new StringItem( "Press '8' to toggle debug messages", 40, world.ScreenHeight - 40, 5.0f, Color.Red, tBlueViolet );

    // Start with debug messages not being displayed on screen
    // (press '8' to toggle)
    ToggleDebug();
    
  }
  #endregion

  #region LoadContent - runs AFTER constructor
  protected override void LoadContent()
  {
    spriteBatch = new SpriteBatch( GraphicsDevice );

    /////
    // Start up the world:
    // give the World object a reference to the GraphicsDevice
    // so that it can create the textures it needs to (textures for ships).
    world.gpu = GraphicsDevice;

    // this is the command that asks the World object to
    // go ahead and generate the textures for the ships etc.
    world.CreateContent();



    /////
    // Init the renderer object for the FlatShapes class
    // so the FlatShapes class will be able to draw stuff.
    FlatShapes.renderer = new BasicEffect( GraphicsDevice, null );




    // load the sounds
    world.sfx.Add( SFX.Alarm, Content.Load<SoundEffect>( "alarm" ) );
    world.sfx.Add( SFX.BlowUp, Content.Load<SoundEffect>( "blow_up" ) );
    world.sfx.Add( SFX.Hyperspace, Content.Load<SoundEffect>( "hyperspace" ) );
    world.sfx.Add( SFX.Phasor, Content.Load<SoundEffect>( "phasor" ) );
    world.sfx.Add( SFX.Torpedo, Content.Load<SoundEffect>( "torpedo" ) );
    world.sfx.Add( SFX.Death, Content.Load<SoundEffect>( "explode" ) );  // this is from warcraft 3, orc building


    // Initialize Aaron's windowing system
    windowing.LoadContent();
  }
  #endregion

  // The MAIN update routine.  Called once per game frame
  // by xna framework.
  protected override void Update( GameTime gameTime )
  {
    // OK THIS IS THE MAIN Update() routine that gets
    // called by the XNA framework.  REMEMBER, this is
    // what the XNA framework does FOREVER (until we quit the game)

    /*
     
     while( true )
     {
       Update() ; 
       Draw() ; 
     }
     
    */

    // GOT IT?? So this is the Update() function.  It will
    // be called REPEATEDLY, and VERY RAPIDLY (60 times a second!!!)
    // THIS IS WHAT makes the game progress, its what makes
    // the ships move forward and the torpedos move forward..
    // its what computes the next FRAME of the game.


    // SO, HOW should we update the game state?
    // It all depends on WHAT "SCREEN" we're at.

    switch( gameState )
    {
      // Are we at the title screen?  if so,
      // then RunTitle() is the "update" function
      // that should run while sitting at the title screen.
      case GameState.TitleScreen:
        // This function essentially does nothing but
        // display the command options for the player
        RunTitle( gameTime );
        break;

      // are we running a local game?
      case GameState.LocalGame:
        // This RunGame function is THE CORE
        // of the game.  It:
        //    1)  Moves players forward an incremental amount
        //        (by whatever their velocity is)
        //    2)  Collision detects and causes damage to
        //        the relevant sprite objects that got damaged.
        RunGame( gameTime );
        break;

      // are we running a netgame?
      case GameState.NetGame:

        //////////////////////////////////
        //////////////////////////////////
        // The UPDATE() region for a NetGame.
        // Here is the thing.  When in a NetGame,
        // there are TIMES WHEN we DO NOT want the
        // player's local simuation to advance. Basically
        // we need to "put the game engine on hold" WHEN
        // a player is running "too far ahead" of
        // his opponent (e.g. say I'm on frame 110, and
        // my opponent is only on frame 90.  Then I should
        // definitely slow down to let him catch up,
        // otherwise the game could fall out of sync).
        //
        // Advance game state if Connected
        // OR if TooFarBehind (in order to catch up)
        if( netState == NetState.Connected ||
            netState == NetState.TooFarBehind )
        {
          RunGame( gameTime );

          // Use the logger to dump game state info to the log file
          // This is a helpful debugging technique
          LogGameState();
        }
        //
        //////////////////////////////////
        //////////////////////////////////
        break;

      case GameState.Testing:
        // This is just a test function I threw in here
        // to test the response times to the server.
        // You can ignore this part.
        if( SPW.netState == NetState.Connected )
          c.netConn.TestNetwork();
        else
          SPW.sw[ "teststart" ] = new StringItem( "Please be sure to connect another player in test mode as well, now.", Color.White, 0 );
        break;

    }

    // check for screenshot.
    // take a screenshot by pressing '9'
    checkScreenShot();
    
    base.Update( gameTime );
  }

  #region rrrrrrrrrrrrrrrrrun
  // Code that runs when sitting at the title screen.
  private void RunTitle( GameTime gameTime )
  {
    int startX = 40 ;
    int startY = 60 ;

    // Just display messages telling the player what to do
    sw[ "Title" ] = new StringItem( "SPACE WARS", StringItem.Centering.Horizontal, startY += 20, 0 );

    sw[ "waiting" ] = new StringItem( "Press 'C' to Connect ...", startX, startY += 220, 0, Color.Gray );

    // if he pushes space, Controller will start the game.
    sw[ "PushKey" ] = new StringItem( "Spacebar to play locally", startX, startY += 20, 0, Color.Gray );

    //sw[ "test" ] = new StringItem( "'T' to test the quality of your network connection", startX, startY += 20, 1.0f, Color.Gray );
  }

  // Method that steps the game forwards one frame
  private void RunGame( GameTime gameTime )
  {
    // Advance frame step number.
    currentFrame++ ;
    

    float stepTime = (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;

    // create alias variables for easy typing
    Ship player1 = world.player1;
    Ship player2 = world.player2;

    // STEP player 1 and player 2 in time.
    // this will basically move them, if
    // they have a velocity.
    player1.Step( stepTime );
    player2.Step( stepTime );

    #region torpedo - player and torpedo-torpedo collisions
    // check for collisions between projectiles and the players
    foreach( Projectile torpedo in world.torpedos )
    {
      // Step the projectile
      torpedo.Step( stepTime );

      // check if this projectile hits any of the players

      // players cannot be hit while in hyperspace
      if( player1.state != ShipState.Hyperspace &&
          player1.state != ShipState.BlowingUp )
        if( torpedo.Intersects( player1 ) )
          torpedo.Strike( player1 );

      if( player2.state != ShipState.Hyperspace &&
          player2.state != ShipState.BlowingUp )
        if( torpedo.Intersects( player2 ) )
          torpedo.Strike( player2 );



      // we should also check for collisions against any other torpedos
      // because torpedos should destroy each other if they collide
      foreach( Projectile otherp in world.torpedos )
      {
        if( torpedo != otherp )    // don't try and collision detect a prjoectile against itself!
          if( torpedo.shooter != otherp.shooter )// and don't detect collisions against two missiles from the same ship
            if( !torpedo.dead )  // don't double check on a torpedo which already died
              if( torpedo.Intersects( otherp ) )
                torpedo.Strike( otherp );  // kill both torpedos by having one strike the other.
      }
    }
    #endregion

    #region phasor collisions
    // Phasor is a bit hard because we have to determine the
    // CLOSEST body to intersect with.  E.g.

    //
    //  E <  D
    //
    // If E is a ship, D is the other ship, and < is a missile,
    // then if E fires its phasor, it should strike the MISSILE,
    // and NOT the ship D.


    // check player1's phasor against player2 ship (includes
    // checks against all torpedos as well)
    RunPlayerPhasor( player1, player2 );

    // now check player2's phasor
    RunPlayerPhasor( player2, player1 );

    #endregion

    #region player-player collisions
    // check for player-player collisions
    // No collisions can happen if either one is in hyperspace
    if( player1.state != ShipState.Hyperspace && player2.state != ShipState.Hyperspace &&
        player1.state != ShipState.BlowingUp  && player2.state != ShipState.BlowingUp )
    {
      if( player1.Intersects( player2 ) )
      {
        // if they do hit each other,
        // just trade velocities.  this is what
        // the original game appears to do.
        Vector2 temp = player1.velocity;
        player1.velocity = player2.velocity;
        player2.velocity = temp;
      }
    }
    #endregion

    #region clean-up

    // remove all dead torpedos (either expired due to time
    // or hit something).
    for( int i = world.torpedos.Count - 1; i >= 0; i-- )
    {
      if( world.torpedos[ i ].dead )
        world.torpedos.RemoveAt( i );
    }

    #endregion

    // now check if either has died.. which means
    // he finished blowing up and we should go
    // back to the title screen
    if( player1.dead || player2.dead )
    {
      gameState = GameState.TitleScreen;
    }
  }

  /// <summary>
  /// Makes a player's phasor strike
  /// the closest object, whether its the other ship
  /// or one of the torpedos.  This function isn't pretty
  /// so you might want to avoid looking at it if possible.
  /// </summary>
  /// <param name="shooter">The ship whose phasor is firing</param>
  /// <param name="against">The other player's ship</param>
  private void RunPlayerPhasor( Ship shooter, Ship against )
  {
    if( shooter.phasor.isActive )
    {
      // We need to find the closest Sprite 
      // to this Phasor.
      Sprite closestSpriteSoFar = null;


      // remember distance of closest object so far
      float? closestStrikeDistanceSoFar = float.MaxValue;
      // float? is a normal float value except
      // it can actually be given the value "null".



      // Try and see if this phasor would hit
      // the other ship, "against".
      if( against.state != ShipState.Hyperspace && // ships cannot be hit while in hyperspace
          against.state != ShipState.BlowingUp )   // and obviously not hittable when blowing up
      {
        // see strike distance to hit other's ship.
        float? strikeDistancePlayer = shooter.phasor.GetStrikeDistanceTo( against );

        if( strikeDistancePlayer < closestStrikeDistanceSoFar )
        {
          closestStrikeDistanceSoFar = strikeDistancePlayer;
          closestSpriteSoFar = against;
        }
      }

      // now check the torpedos.
      // find the closest one that the phasor
      // hits, if any.
      foreach( Projectile torpedo in world.torpedos )
      {
        if( !torpedo.dead )
        {
          float? strikeDistanceTorpedo = shooter.phasor.GetStrikeDistanceTo( torpedo );

          // interestingly, if strikeDistanceTorpedo came back as null,
          // then the comparison below will ALWAYS be FALSE.

          // null <compared to in any way> some other value ===== FALSE
          // null < 0    is    FALSE,   also,    null > 0    is    FALSE
          if( strikeDistanceTorpedo < closestStrikeDistanceSoFar )
          {
            closestStrikeDistanceSoFar = strikeDistanceTorpedo;
            closestSpriteSoFar = torpedo;
          }
        }
      }

      // Strike the closest body, be it
      // the other player's Ship OR just
      // a simple torpedo.
      if( closestSpriteSoFar != null )
        shooter.phasor.Strike( closestSpriteSoFar );
    }
  }
  #endregion

  #region drrrrrrrrrrrrrrrraw
  // The MAIN draw routine.  Called once per game frame
  // by xna framework.
  // Draws the game, depending on whether
  // we're at menu, or in the game
  protected override void Draw( GameTime gameTime )
  {
    // First, clear off all stuff from previous frame.
    GraphicsDevice.Clear( Color.Black );

    // Now, take a look at game state.
    switch( gameState )
    {
      case GameState.TitleScreen:
        // We are at the title screen.  So,
        // just draw relevant stuff for the TitleScreen.
        DrawTitle( gameTime );
        break;

      case GameState.LocalGame:
      case GameState.NetGame:
        // Whether we are running a local game or a netgame
        // the game state should render out the same, so
        // no changes should be made here.
        DrawGame( gameTime );
        break;
    }


    base.Draw( gameTime );
  }

  private void DrawTitle( GameTime gameTime )
  {
    ////ShowDebug();

    if( world.player1.dead == true )
    {
      // I just don't like these to show anymore
      //sw[ "player1lose" ] = new StringItem( "Player 1 has died", 40, 180, 0 );
    }

    if( world.player2.dead == true )
    {
      //sw[ "player2lose" ] = new StringItem( "Player 2 has died", 40, 200, 0 );
    }

    // if it was a netgame, then also tell the player if he won or lost
    // ### This is again YOUR PART:  Add code to record win or loss in database here.
    #region tell if won or lost the netgame
    if( netState == NetState.Connected )
    {
      string winMsg = "YOU WON YEAHAHEAH";
      string loseMsg = "YOU LOST";
      float displayTime = 3.0f;
      if( Controller.myNetgamePlayerNumber == 1 )
      {
        // here, you are player 1
        // messages for player 1
        if( world.player1.dead )  // this person WAS player 1, and player 1 is dead, so he lost
        {
          sw[ "yourMessage" ] = new StringItem( loseMsg, Color.Red, displayTime );
          logger.Log( "You LOST, as player 1 at " + currentFrame + ".  The game has ended.", LogMessageType.Info, OutputDevice.File );
        }
        else  // player 2 died, so this guy won
        {
          sw[ "yourMessage" ] = new StringItem( winMsg, Color.Teal, displayTime );
          logger.Log( "You WON, as player 1 at " + currentFrame + ".  The game has ended.", LogMessageType.Info, OutputDevice.File );
        }
      }
      else
      {
        // here, you are player 2
        // messages for player 2
        if( world.player1.dead ) // player 2 has killed player 1
        {
          sw[ "yourMessage" ] = new StringItem( winMsg, Color.Teal, displayTime );
          logger.Log( "You WON, as player 2 at " + currentFrame + ".  The game has ended.", LogMessageType.Info, OutputDevice.File );
        }
        else  // player 2 has died
        {
          sw[ "yourMessage" ] = new StringItem( loseMsg, Color.Red, displayTime );
          logger.Log( "You LOST, as player 2 at " + currentFrame + ".  The game has ended.", LogMessageType.Info, OutputDevice.File );
        }
      }
    }
    #endregion
  }

  // Draws the game
  private void DrawGame( GameTime gameTime )
  {
    if( showDebug )
      ShowDebug();


    spriteBatch.Begin();

    foreach( Sprite star in world.stars )
    {
      spriteBatch.Draw( star.tex, star.position, Color.White );
    }

    foreach( Projectile p in world.torpedos )
    {
      spriteBatch.Draw( p.tex, p.position, null, Color.White, (float)p.rot, p.center, 1.0f, SpriteEffects.None, 0.0f );
    }



    Ship player1 = world.player1;
    Ship player2 = world.player2;



    // You can't see the cloaking player so just don't draw him in that state.

    if( player1.state == ShipState.Normal ) // normal
      spriteBatch.Draw( player1.tex, player1.position, null, Color.White, (float)player1.rot, player1.center, 1.0f, SpriteEffects.None, 0.0f );
    else if( player1.state == ShipState.Hyperspace )
      player1.DrawParticles( spriteBatch );
    else if( player1.state == ShipState.BlowingUp )
      player1.DrawDeath( spriteBatch );

    if( player2.state == ShipState.Normal )
      spriteBatch.Draw( player2.tex, player2.position, null, Color.White, (float)player2.rot, player2.center, 1.0f, SpriteEffects.None, 0.0f );
    else if( player2.state == ShipState.Hyperspace )
      player2.DrawParticles( spriteBatch );
    else if( player2.state == ShipState.BlowingUp )
      player2.DrawDeath( spriteBatch );


    //player1.DrawHealth( spriteBatch );
    //player2.DrawHealth( spriteBatch );
    spriteBatch.End();


    // draw the phasors of each player, if active
    FlatShapes.Begin();
    if( player1.phasor.isActive )
    {
      FlatShapes.Line( player1.position, player1.phasor.getEndOfReachPoint() );
    }

    if( player2.phasor.isActive )
    {
      FlatShapes.Line( player2.position, player2.phasor.getEndOfReachPoint() );
    }


    player1.DrawHealthAsBars();
    player2.DrawHealthAsBars();

    FlatShapes.End();
  }
  #endregion

  #region debug and log
  /// <summary>
  /// Prints strings like
  /// [20:44:40] [1] [Info] P1Ship Normal shld=40 enrg=120 rot=3.14 pos=( 480.00, 276.00 ) vel=( 0.00, 0.00 )
  /// to the game's log file.
  /// </summary>
  private void LogGameState()
  {
    logger.Log( "P1" + world.player1.ToString(), LogMessageType.Info, OutputDevice.File );
    logger.Log( "P2" + world.player2.ToString(), LogMessageType.Info, OutputDevice.File );
  }

  /// <summary>
  /// Shows a bunch of strings using the ScreenWriter that contains
  /// just lots of debug info
  /// </summary>
  private void ShowDebug()
  {
    // Always draw the frame counter
    int startRightX = SPW.world.ScreenWidth - 200;
    int y = 10;
    sw[ "frameNumber" ] = new StringItem( currentFrame.ToString(), startRightX, y += 20, 1.0f, Color.White );
    sw[ "gameState" ] = new StringItem( "gameState: " + gameState, startRightX, y += 20, 1.0f, Color.Gray );
    sw[ "netState" ] = new StringItem( "netState: " + netState, startRightX, y += 20, 1.0f, Color.Gray );
    sw[ "delaystring" ] = new StringItem( "avg delay: " + Controller.delayMetrics.averageMessageDelay, startRightX, y += 20, 1.0f, Color.Gray );

    sw[ "delaystring1" ] = new StringItem( "last transit: " + Controller.delayMetrics.LastMessageTransportTime, startRightX, y += 20, 1.0f, Color.Gray );
    // sw[ "delaystring2" ] = new StringItem( "sum delay: " + Controller.delayMetrics.TotalFrameDelay, startRightX, y += 20, 1.0f, Color.Gray );
    // sw[ "delaystring3" ] = new StringItem( "total msgs: " + Controller.delayMetrics.TotalMessagesReceived, startRightX, y += 20, 1.0f, Color.Gray );
    sw[ "delaystring4" ] = new StringItem( "frames diff: " + Controller.framesAhead, startRightX, y += 20, 1.0f, Color.Gray );
    sw[ "delaystring5" ] = new StringItem( "(+ means ahead)", startRightX, y += 20, 1.0f, Color.Gray );

    /*
    if( c.netConn.listenerThread != null )
    {
      sw[ "delaystring5.5" ] = new StringItem( "listen thread " + ( c.netConn.listenerThread.IsAlive ? "alive" : "dead" ), startRightX, y += 20, 1.0f, Color.Gray );
      sw[ "delaystring5.6" ] = new StringItem( "thread id " + c.netConn.listenerThread.ManagedThreadId, startRightX, y += 20, 1.0f, Color.Gray );
    }
    */

    sw[ "delaystring6" ] = new StringItem( "last sync: " + Controller.frameOtherGuyWasOnAtLastSync, startRightX, y += 40, 1.0f, Color.Gray );

  }

  /// <summary>
  /// Toggles debug output to be either on or off
  /// </summary>
  public void ToggleDebug()
  {
    if( showDebug )
    {
      // was on, so turn off
      logger.Disable();
      showDebug = false;
    }
    else
    {
      logger.Enable();
      showDebug = true;
    }
  }
  #endregion

  #region reset and exit routines
  /// <summary>
  /// Resets the game.
  /// </summary>
  public void ResetGameInternals()
  {
    // reset player health, positions etc to 
    // proper starting values
    world.CreateContent();

    // clear out incoming and processing queues
    Controller.incoming.Clear();
    Controller.processing.Clear();

    // reset delay metrics measurement object
    Controller.delayMetrics = new DelayMetrics();

    c.netConn.ResetNetworkConnection();

    // and we reset to frame 0
    currentFrame = 0;

    // and last frame you got as sync, also to 0
    Controller.frameOtherGuyWasOnAtLastSync = 0;
  }

  /// <summary>
  /// XNA automatically runs this when the application is exiting,
  /// so we take the opportunity to shut down everything
  /// </summary>
  protected override void OnExiting( object sender, EventArgs args )
  {
    // Shut down the controller.  This in turn will
    // shut down the other thread.
    c.Shutdown();

    // sleep a bit, to make sure that the network stuff
    // shuts down, before shutting down the logger (because
    // the network stuff logs its shut down info)
    System.Threading.Thread.Sleep( 50 ) ;
    logger.Shutdown();

    base.OnExiting( sender, args );
  }
  #endregion

  #region other functions
  public string GameExecDir
  {
    get
    {
      return System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );
    }
  }

  public void OpenContainingFolder()
  {
    System.Diagnostics.Process.Start( "explorer.exe", GameExecDir );
  }

  /// <summary>
  /// Opens the folder indicated by "SPW.path", if set.  If not
  /// set, then just opens game exec dir
  /// </summary>
  public void OpenLogFileFolder()
  {
    if( path == string.Empty )
      OpenContainingFolder();
    else
      System.Diagnostics.Process.Start( "explorer.exe", path );
  }

  public void OpenLogFile()
  {
    System.Diagnostics.Process.Start( "explorer.exe", SPW.logger.Filename );
  }

  #region ScreenShot providing code ** improved **
  public bool ScreenShot = false;
  /// <summary>
  /// This new way works very well unless you fullscreen.
  /// Then it doesn't work so well.
  /// </summary>
  private void checkScreenShot()
  {
    if( ScreenShot == true )
    {
      string filename = path + DateTime.Now.ToString( "MMM_dd_yy__HH_mm_ss_ffffff" ) + ".png";

      int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
      int h = GraphicsDevice.PresentationParameters.BackBufferHeight;

      // Need to ADD A REFERENCE (right click "References.. Add Reference"
      // to System.Drawing to use this screenshotting method.
      System.Drawing.Bitmap screenShot = new System.Drawing.Bitmap( w, h );
      System.Drawing.Graphics g = System.Drawing.Graphics.FromImage( screenShot );

      g.CopyFromScreen( this.Window.ClientBounds.X, this.Window.ClientBounds.Y, 0, 0, new System.Drawing.Size( w, h ) );

      screenShot.Save( filename, System.Drawing.Imaging.ImageFormat.Png );

      ScreenShot = false;
    }

  }

  /// <summary>
  /// This old way doesn't work that well.  Use the new way.
  /// </summary>
  private void checkScreenShotOld()
  {
    if( ScreenShot == true )
    {
      // stamp with time and dump to disk.
      string filename = SPW.path + DateTime.Now.ToString( "MMM_dd_yy__HH_mm_ss_ffffff" ) + ".png";

      sw[ "screenshot" ] = new StringItem( "Took screenshot:  " + filename + ".  Press '5'", 20, graphics.PreferredBackBufferHeight - 40 );

      ResolveTexture2D rt = new ResolveTexture2D( GraphicsDevice,
        graphics.PreferredBackBufferWidth,
        graphics.PreferredBackBufferHeight,
        1, SurfaceFormat.HalfVector4 );

      GraphicsDevice.ResolveBackBuffer( rt );

      // Put this on a seperate thread, maybe.
      rt.Save( filename, ImageFileFormat.Png );

      ScreenShot = false;
    }
  }
  #endregion

  #endregion

  // Application starting point.
  static void Main( string[] args )
  {
    // Create an instance of the SPW class
    SPW game = new SPW();

    // Launch the game.
    game.Run();
  }
}
