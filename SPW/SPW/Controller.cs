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
#endregion

public class Controller : GameComponent
{
  private KeyboardState kbPrevState, kbCurrState;
  private MouseState msPrevState, msCurrState;

  private Dictionary<PlayerIndex, GamePadState> gpPrevStates, gpCurrStates;
  private List<PlayerIndex> pis; // connected controllers.

  private SPW game ;

  private NetworkListener netConn ;

  public Controller( Game g )
    : base( g )  // call base class ctor
  {
    // save reference to the main game itself
    game = this.Game as SPW;



    netConn = new NetworkListener() ;
    


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
    //
    if( this.JustPressed( Keys.Escape ) )
      game.Exit();

    if( this.JustPressed( Keys.D5 ) )
      game.OpenContainingFolder();

    if( this.JustPressed( Keys.D9 ) )
      game.ScreenShot = true; // take a screenshot next frame.

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
    }
      
  }


  #region player control functions

  private void RunNetGameControls()
  {
    switch( SPW.netState )
    {
      case NetState.Waiting:
        SPW.sw[ "NetMessage" ] = new StringItem( "You are waiting for another player to connect at the server", 20, 80 );
        SPW.sw[ "NetMessage2" ] = new StringItem( "Press 'D' to cancel", 20, 100 );

        if( JustPressed( Keys.D ) )
        {
          //netConn.Shutdown();

          SPW.netState = NetState.Disconnected;
          SPW.gameState = GameState.TitleScreen ;
        }

        break;

      case NetState.Disconnected:
        SPW.sw[ "NetMessage" ] = new StringItem( "You were disconnected", 20, 80 );
        break;

      case NetState.Connected:
        {
          SPW.sw[ "NetMessage" ] = new StringItem( "You are playing with another player", 20, 80 );
          // poll remote host for input and modify game state this way
        }
        break;
    }
  }




  // Runs when we're at the title screen
  private void RunTitleScreenControls()
  {
    if( JustPressed( Keys.Space ) )
    {
      game.ResetLocalGame() ;
    }

    if( JustPressed( Keys.C ) )
    {
      // try and launch the connection
      if( netConn.Connect() )
      {
        // set game into netgame mode
        SPW.gameState = GameState.NetGame;
        SPW.netState = NetState.Waiting;
      }
      else
      {
        SPW.sw[ "error"  ] = new StringItem( "Couldn't connect to the server!", 40, 220, 4.0f, Color.Red ) ;
      }
    }
  }






  // Runs during normal game play
  private void RunGameControls()
  {
    Ship player1 = SPW.world.player1;
    Ship player2 = SPW.world.player2;

    // If the 1p controller is connected, then use it
    if( pis.Contains(PlayerIndex.One) )
    {
      RunPlayerFromController( PlayerIndex.One, player1 ) ;
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

        if( IsPressed( Keys.NumPad7 ) )
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

        if( IsPressed( Keys.Q ) )
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

      if( IsPressed( pNum, Buttons.X ) )
        playerShip.ShootPhasors();

      if( IsPressed( pNum, Buttons.RightTrigger ) )
        playerShip.Cloak();

      if( JustPressed( pNum, Buttons.A ) )
        playerShip.ShootTorpedos();
    }
  }
  #endregion





  public void Shutdown()
  {
    //netConn.Shutdown() ;
  }




  #region functions that check for key states
  public bool JustPressed( Keys key )
  {
    if( kbCurrState.IsKeyDown( key ) &&
        kbPrevState.IsKeyUp( key ) )
      return true;
    else
      return false;
  }

  public bool JustPressed( PlayerIndex which, Buttons button )
  {
    if( gpCurrStates[ which ].IsButtonDown( button ) &&
        gpPrevStates[ which ].IsButtonUp( button ) )
      return true;
    else
      return false;
  }

  public bool JustReleased( Keys key )
  {
    if( kbCurrState.IsKeyUp( key ) &&
        kbPrevState.IsKeyDown( key ) )
      return true;
    else
      return false;
  }

  public bool JustReleased( PlayerIndex which, Buttons button )
  {
    if( gpCurrStates[ which ].IsButtonUp( button ) &&
        gpPrevStates[ which ].IsButtonDown( button ) )
      return true;
    else
      return false;
  }

  public bool IsPressed( Keys key )
  {
    return kbCurrState.IsKeyDown( key );
  }

  public bool IsPressed( PlayerIndex which, Buttons button )
  {
    return gpCurrStates[ which ].IsButtonDown( button );
  }
  #endregion

}