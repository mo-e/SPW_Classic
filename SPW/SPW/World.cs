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




/// <summary>
/// A container for the world and all
/// the objects in it.
/// </summary>
public class World
{
  // This is a 2 player game.
  public int ScreenWidth = 640;
  public int ScreenHeight = 400;

  public List<Sprite> stars;
  public List<Projectile> torpedos;

  public Texture2D whitePX;
  public Texture2D torpedoTexP1;
  public Texture2D torpedoTexP2;

  public Ship player1;
  public Ship player2;

  // Contains all the sound effects.
  public Dictionary<SFX, SoundEffect> sfx;

  // Side walls of the world
  public Dictionary<Side, Plane> walls;

  // Need a reference to the GraphicsDevice
  // of the SPW class.
  public GraphicsDevice gpu;

  /// <summary>
  /// Initialize the world's object structures
  /// </summary>
  public World()
  {
    // just initialize this
    sfx = new Dictionary<SFX, SoundEffect>();

    // and initialize this
    torpedos = new List<Projectile>();

    #region create the side walls
    // and the walls which are the edges of the world
    walls = new Dictionary<Side, Plane>();

    // The wall planes are perpendicular to the
    // plane that the game takes place in.

    //            TOP plane (goes INTO the screen here, along the x-axis)
    // ------------------------------->
    // |                              |
    // |    C                         |
    // |                              |
    // |                              |< right plane, goes INTO the screen here, along y axis
    // |                       D      |
    // |                              |
    // V------------------------------|

    walls[ Side.Left ] = new Plane( -1, 0, 0, 0 );  // the left side plane has its
    // normal in the direction (-1, 0, 0) and you must travel
    // 0 units in the direction of the normal to GET TO the origin.

    walls[ Side.Top ] = new Plane( 0, -1, 0, 0 );

    walls[ Side.Right ] = new Plane( 1, 0, 0, -ScreenWidth );
    // travel -ScreenWidth units along the normal to GET TO the origin.

    walls[ Side.Bottom ] = new Plane( 0, 1, 0, -ScreenHeight );
    #endregion
  }


  /// <summary>
  /// Create textures, initialize players, etc.
  /// Also works to reset the game.
  /// </summary>
  public void CreateContent()
  {
    Color w = Color.White;
    Color t = Color.TransparentBlack;

    // Create a 1x1 white texture
    whitePX = new Texture2D( gpu, 1, 1 );
    whitePX.SetData<Color>( new Color[]{
      w
    } );


    // Init player 1
    player1 = new Ship();
    player1.playerNumber = 1;

    player1.position.X = 480;
    player1.position.Y = 276;

    // player 1 starts facing the left
    player1.rot = (float)Math.PI;

    player1.graphicalWidth = 20;
    player1.graphicalHeight = 20;
    player1.center = new Vector2( 10, 10 );
    player1.tex = new Texture2D( gpu, player1.graphicalWidth, player1.graphicalHeight );

    // this is the texture for player 1's ship.
    // i'm setting each pixel to a color manually -
    // "t" means "transparent", and "w" means white pixel.
    player1.tex.SetData<Color>( new Color[]{
      t,t,t,t,t,t,t,t,t,w,w,w,w,w,w,t,t,t,t,t,
      t,t,t,t,t,t,t,t,t,w,w,w,w,w,w,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,w,t,t,t,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,w,t,t,t,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,w,w,t,t,t,t,w,w,w,w,t,t,t,t,t,t,
      t,t,t,t,w,w,t,t,t,t,w,w,w,w,t,t,t,t,t,t,
      t,t,t,t,w,w,w,w,w,w,w,t,t,w,w,t,t,t,t,t,
      t,t,t,t,w,w,w,w,w,w,w,t,t,w,w,t,t,t,t,t,
      t,t,t,t,w,w,w,w,w,w,w,t,t,w,w,t,t,t,t,t,
      t,t,t,t,w,w,w,w,w,w,w,t,t,w,w,t,t,t,t,t,
      t,t,t,t,w,w,t,t,t,t,w,w,w,w,t,t,t,t,t,t,
      t,t,t,t,w,w,t,t,t,t,w,w,w,w,t,t,t,t,t,t,
      t,t,t,t,t,w,t,t,t,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,w,t,t,t,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,t,t,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,t,t,t,t,w,w,w,w,w,w,t,t,t,t,t,
      t,t,t,t,t,t,t,t,t,w,w,w,w,w,w,t,t,t,t,t
    } );


    // Init player 2
    player2 = new Ship();
    player2.playerNumber = 2;

    player2.position.X = 160;
    player2.position.Y = 90;

    player2.graphicalWidth = 20;
    player2.graphicalHeight = 20;
    player2.center = new Vector2( 10, 10 );

    player2.tex = new Texture2D( gpu, player2.graphicalWidth, player2.graphicalHeight );

    // Create the texture for player 2's ship
    player2.tex.SetData<Color>( new Color[]{
      t,t,t,t,t,t,w,w,w,w,w,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,w,t,t,t,t,t,t,t,t,t,
      t,t,t,w,w,w,t,t,t,t,t,w,w,w,t,t,t,t,t,t,
      t,t,t,w,w,w,t,t,t,t,t,w,w,w,t,t,t,t,t,t,
      t,t,t,t,t,w,w,t,t,t,t,t,t,t,w,t,t,t,t,t,
      t,t,t,t,t,w,w,t,t,t,t,t,t,t,w,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,t,t,t,t,t,w,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,t,t,t,t,t,w,t,t,t,t,
      t,t,t,t,t,t,w,t,t,t,w,w,w,w,w,w,t,t,t,t,
      t,t,t,t,t,t,w,t,t,t,w,w,w,w,w,w,t,t,t,t,
      t,t,t,t,t,t,w,t,t,t,w,w,w,w,w,w,t,t,t,t,
      t,t,t,t,t,t,w,t,t,t,w,w,w,w,w,w,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,t,t,t,t,t,w,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,t,t,t,t,t,w,t,t,t,t,
      t,t,t,t,t,w,w,t,t,t,t,t,t,t,w,t,t,t,t,t,
      t,t,t,t,t,w,w,t,t,t,t,t,t,t,w,t,t,t,t,t,
      t,t,t,w,w,w,t,t,t,t,t,w,w,w,t,t,t,t,t,t,
      t,t,t,w,w,w,t,t,t,t,t,w,w,w,t,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,w,t,t,t,t,t,t,t,t,t,
      t,t,t,t,t,t,w,w,w,w,w,t,t,t,t,t,t,t,t,t
    } );


    torpedoTexP1 = new Texture2D( gpu, 7, 12 );

    // the texture of player 1's torpedos
    torpedoTexP1.SetData<Color>( new Color[] {
      t,w,w,t,t,t,t,
      t,w,w,t,t,t,t,
      t,t,w,w,w,t,t,
      t,t,w,w,w,t,t,
      t,t,t,w,w,w,w,
      t,t,t,w,w,w,w,
      t,t,t,w,w,w,w,
      t,t,t,w,w,w,w,
      t,t,w,w,w,t,t,
      t,t,w,w,w,t,t,
      t,w,w,t,t,t,t,
      t,w,w,t,t,t,t
    } );


    torpedoTexP2 = new Texture2D( gpu, 7, 12 );

    // the texture of player 2's torpedos
    torpedoTexP2.SetData<Color>( new Color[] {
      t,t,t,t,t,t,t,
      t,t,t,t,t,t,t,
      w,w,t,t,t,t,t,
      w,w,t,t,t,t,t,
      t,t,t,w,w,w,w,
      t,t,t,w,w,w,w,
      t,t,t,w,w,w,w,
      t,t,t,w,w,w,w,
      w,w,t,t,t,t,t,
      w,w,t,t,t,t,t,
      t,t,t,t,t,t,t,
      t,t,t,t,t,t,t
    } );


    // create the stars in the sky
    stars = new List<Sprite>();
    for( int i = 0; i < 400; i++ )
    {
      stars.Add( new Sprite( whitePX, SPW.rand.Next( 0, ScreenWidth ), SPW.rand.Next( 0, ScreenHeight ), 1, 2 ) );
    }

    // create / clear out the list of 
    // torpedos
    torpedos = new List<Projectile>();
  }
}



