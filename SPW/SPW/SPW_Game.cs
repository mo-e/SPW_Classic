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
#endregion



// MAIN GAME FILE
// This file contains the core of the game.


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
  public static GameState gameState;

  // if in a netgame, then this is the state of the networked game.
  public static NetState netState;


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



    // Initialize the game state
    gameState = GameState.TitleScreen;  // let game start @ title screen
    netState = NetState.Disconnected;   // 

    // create the world object
    world = new World();

    // set width and height of the backbuffer
    this.graphics.PreferredBackBufferHeight = world.ScreenHeight;
    this.graphics.PreferredBackBufferWidth = world.ScreenWidth;

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

        // THIS IS THE PART YOU MUST WORK ON.
        if( netState == NetState.Connected )
          RunGame( gameTime );

        break;
    }

    // check for screenshot.
    // take a screenshot by pressing '9'
    checkScreenShot();

    base.Update( gameTime );
  }

  // Code that runs when sitting at the title screen.
  private void RunTitle( GameTime gameTime )
  {
    // Just display messages telling the player what to do
    sw[ "Title" ] = new StringItem( "SPACE WARS", 40, 40, 1.0f );

    sw[ "waiting" ] = new StringItem( "Press 'C' to Connect ...", 40, 60, 1.0f );

    // if he pushes space, Controller will start the game.
    sw[ "PushKey" ] = new StringItem( "... or press spacebar to play locally", 40, 80, 1.0f );
  }

  // Method that steps the game forwards one frame
  private void RunGame( GameTime gameTime )
  {
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
    if( world.player1.dead == true )
    {
      sw[ "player1lose" ] = new StringItem( "Player 1 has died", 40, 180 );
    }

    if( world.player2.dead == true )
    {
      sw[ "player2lose" ] = new StringItem( "Player 2 has died", 40, 200 );
    }
  }

  // Draws the game
  private void DrawGame( GameTime gameTime )
  {
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


  // Resets the game
  public void ResetLocalGame()
  {
    world.CreateContent();

    gameState = GameState.LocalGame;
  }


  protected override void OnExiting( object sender, EventArgs args )
  {
    // make sure to kill the listener thread it if is active
    c.Shutdown();

    base.OnExiting( sender, args );
  }

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

  #region ScreenShot providing code
  public bool ScreenShot = false;
  private void checkScreenShot()
  {
    if( ScreenShot == true )
    {
      // stamp with time and dump to disk.
      string filename = DateTime.Now.ToString( "MMM_dd_yy__HH_mm_ss_ffffff" ) + ".png";

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
