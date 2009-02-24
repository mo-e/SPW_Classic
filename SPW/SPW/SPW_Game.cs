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



public class SPW : Microsoft.Xna.Framework.Game
{
  GraphicsDeviceManager graphics;
  SpriteBatch spriteBatch;
  public static Random rand = new Random() ;


  public static GameState gameState ;
  public static NetState netState ;


  /// <summary>
  /// Static member accessible from anywhere
  /// using SPW.world.
  /// </summary>
  public static World world ;

  /// <summary>
  /// The screenwriter - Also available everywhere
  /// </summary>
  public static ScreenWriter sw;

  // Game component - the controller
  Controller c ;


  public SPW()
  {
    graphics = new GraphicsDeviceManager( this );
    Content.RootDirectory = "Content";

    // instantiate the Controller
    c = new Controller( this );
    
    // add it as a component so ITS Update()
    // function will AUTOMATICALLY be called
    // everytime this class's does.
    this.Components.Add( c ) ;



    // Add a few more components.
    sw = new ScreenWriter( this ) ;
    this.Components.Add( sw ) ;


    


    gameState = GameState.TitleScreen ;
    netState = NetState.Disconnected ;

    world = new World();

    this.graphics.PreferredBackBufferHeight = world.ScreenHeight ;
    this.graphics.PreferredBackBufferWidth = world.ScreenWidth ;

  }

  protected override void Initialize()
  {
    base.Initialize();
  }

  protected override void LoadContent()
  {
    spriteBatch = new SpriteBatch( GraphicsDevice );

    world.gpu = GraphicsDevice ;

    world.CreateContent();

    // load the sounds
    world.sfx.Add( SFX.Alarm, Content.Load<SoundEffect>( "alarm" ) );
    world.sfx.Add( SFX.BlowUp, Content.Load<SoundEffect>( "blow_up" ) );
    world.sfx.Add( SFX.Hyperspace, Content.Load<SoundEffect>( "hyperspace" ) );
    world.sfx.Add( SFX.Phasor, Content.Load<SoundEffect>( "phasor" ) );
    world.sfx.Add( SFX.Torpedo, Content.Load<SoundEffect>( "torpedo" ) );
  }




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


    // check for collisions between projectiles and the players
    foreach( Projectile p in world.torpedos )
    {
      // Step the projectile
      p.Step( stepTime );

      // check if it hits any of the players

      // players cannot be hit while in hyperspace
      if( player1.state != ShipState.Hyperspace )
        if( p.Intersects( player1 ) )
          p.Strike( player1 );

      if( player2.state != ShipState.Hyperspace )
        if( p.Intersects( player2 ) )
          p.Strike( player2 );
    }


    // check for player-player collisions
    // No collisions can happen if either one is in hyperspace
    if( player1.state != ShipState.Hyperspace && player2.state != ShipState.Hyperspace )
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


    // remove all dead torpedos (either expired due to time
    // or hit something).
    for( int i = world.torpedos.Count - 1; i >= 0; i-- )
    {
      if( world.torpedos[ i ].dead )
        world.torpedos.RemoveAt( i );
    }


    // check if either player should blow up
    if( player1.shield < 0.0f )
      player1.BlowUp();

    if( player2.shield < 0.0f )
      player2.BlowUp();



    // now check if either has died.. which means
    // he finished blowing up and we should go
    // back to the title screen
    if( player1.dead || player2.dead )
    {
      gameState = GameState.TitleScreen;
    }
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

  protected override void Update( GameTime gameTime )
  {
    switch( gameState )
    {
      case GameState.TitleScreen:
        RunTitle( gameTime ) ;
        break;

      case GameState.LocalGame:
        RunGame( gameTime );
        break;

      case GameState.NetGame:
        if( netState == NetState.Connected )
          RunGame( gameTime ) ;

        break;
    }

    // check for screenshot
    checkScreenShot();

    base.Update( gameTime );
  }


  private void DrawTitle( GameTime gameTime )
  {
    if( world.player1.dead == true )
    {
      sw[ "player1lose" ] = new StringItem( "Player 1 has died", 40, 180 );
    }

    if( world.player2.dead == true )
    {
      sw[ "player2lose" ] = new StringItem( "Player 2 has died", 40, 200 ) ;
    }
  }

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


    player1.DrawHealth( spriteBatch );
    player2.DrawHealth( spriteBatch );
    spriteBatch.End();

    //debug();

  }


  // Draws the game, depending on whether
  // we're at menu, or in the game
  protected override void Draw( GameTime gameTime )
  {
    GraphicsDevice.Clear( Color.Black );

    switch( gameState )
    {
      case GameState.TitleScreen:
        DrawTitle( gameTime ) ;
        break;

      case GameState.LocalGame:
      case GameState.NetGame:
        DrawGame( gameTime );
        break;
    }


    base.Draw( gameTime );
  }


  private void debug()
  {
    sw[ "player-1" ] = new StringItem( world.player1.ToString(), 20, 20 ) ;
    sw[ "player-2" ] = new StringItem( world.player2.ToString(), 20, 40 );
  }


  // Resets the game
  public void ResetLocalGame()
  {
    world.CreateContent();
    gameState = GameState.LocalGame ;

    // show welcome message
    //sw[ "welcome" ] = new StringItem( "welcome to space wars!", 20, 20, 2.0f, Color.White, Color.Black );
  }


  protected override void OnExiting( object sender, EventArgs args )
  {
    // make sure to kill the listener thread it if is active
    c.Shutdown() ;

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

      sw["screenshot"] = new StringItem( "Took screenshot:  " + filename + ".  Press '5'", 20, graphics.PreferredBackBufferHeight - 40 );

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

  static void Main( string[] args )
  {
    SPW game = new SPW();
    game.Run();
  }
}
