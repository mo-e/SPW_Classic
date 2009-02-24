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

  public List<Sprite> stars ;
  public List<Projectile> torpedos ;

  public Texture2D whitePX;
  public Texture2D torpedoTexP1 ;
  public Texture2D torpedoTexP2;

  public Ship player1;
  public Ship player2;




  public Dictionary<SFX, SoundEffect> sfx ;

  /// <summary>
  /// Need a reference to the GraphicsDevice
  /// of the SPW class.
  /// </summary>
  public GraphicsDevice gpu ;

  /// <summary>
  /// Initialize the world with default state
  /// </summary>
  public World()
  {
    sfx = new Dictionary<SFX,SoundEffect>();
    torpedos = new List<Projectile>();
  }

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
    player1.playerNumber = 1 ;

    player1.position.X = 500;
    player1.position.Y = 200;

    player1.graphicalWidth = 20;
    player1.graphicalHeight = 20;
    player1.center = new Vector2( 10, 10 );
    player1.tex = new Texture2D( gpu, player1.graphicalWidth, player1.graphicalHeight );
    player1.tex.SetData<Color>( new Color[]{
      t, t, t, t, w, w, w, w, w, w, t, t, t, t, t, t, t, t, t, t,
      t, t, t, t, w, w, w, w, w, w, t, t, t, t, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, w, w, w, t, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, w, w, w, t, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, t, t, t, w, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, t, t, t, w, t, t, t, t, t, t,
      t, t, t, t, t, w, w, w, w, t, t, t, t, w, w, t, t, t, t, t,
      t, t, t, t, t, w, w, w, w, t, t, t, t, w, w, t, t, t, t, t,
      t, t, t, t, w, w, t, t, w, w, w, w, w, w, w, t, t, t, t, t,
      t, t, t, t, w, w, t, t, w, w, w, w, w, w, w, t, t, t, t, t,
      t, t, t, t, w, w, t, t, w, w, w, w, w, w, w, t, t, t, t, t,
      t, t, t, t, w, w, t, t, w, w, w, w, w, w, w, t, t, t, t, t,
      t, t, t, t, t, w, w, w, w, t, t, t, t, w, w, t, t, t, t, t,
      t, t, t, t, t, w, w, w, w, t, t, t, t, w, w, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, t, t, t, w, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, t, t, t, w, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, w, w, w, t, t, t, t, t, t, t,
      t, t, t, t, t, t, t, t, t, t, w, w, w, t, t, t, t, t, t, t,
      t, t, t, t, w, w, w, w, w, w, t, t, t, t, t, t, t, t, t, t,
      t, t, t, t, w, w, w, w, w, w, t, t, t, t, t, t, t, t, t, t
    } );



    // Init player 2
    player2 = new Ship();
    player2.playerNumber = 2;

    player2.position.X = 100;
    player2.position.Y = 200;

    player2.graphicalWidth = 20;
    player2.graphicalHeight = 20;
    player2.center = new Vector2( 10, 10 );

    player2.tex = new Texture2D( gpu, player2.graphicalWidth, player2.graphicalHeight );
    player2.tex.SetData<Color>( new Color[]{
      t, t, t, t, t, t, w, w, w, w, w, t, t, t, t, t, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, w, t, t, t, t, t, t, t, t, t,
      t, t, t, w, w, w, t, t, t, t, t, w, w, w, t, t, t, t, t, t,
      t, t, t, w, w, w, t, t, t, t, t, w, w, w, t, t, t, t, t, t,
      t, t, t, t, t, w, w, t, t, t, t, t, t, t, w, t, t, t, t, t,
      t, t, t, t, t, w, w, t, t, t, t, t, t, t, w, t, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, t, t, t, t, t, w, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, t, t, t, t, t, w, t, t, t, t,
      t, t, t, t, t, t, w, t, t, t, w, w, w, w, w, w, t, t, t, t,
      t, t, t, t, t, t, w, t, t, t, w, w, w, w, w, w, t, t, t, t,
      t, t, t, t, t, t, w, t, t, t, w, w, w, w, w, w, t, t, t, t,
      t, t, t, t, t, t, w, t, t, t, w, w, w, w, w, w, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, t, t, t, t, t, w, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, t, t, t, t, t, w, t, t, t, t,
      t, t, t, t, t, w, w, t, t, t, t, t, t, t, w, t, t, t, t, t,
      t, t, t, t, t, w, w, t, t, t, t, t, t, t, w, t, t, t, t, t,
      t, t, t, w, w, w, t, t, t, t, t, w, w, w, t, t, t, t, t, t,
      t, t, t, w, w, w, t, t, t, t, t, w, w, w, t, t, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, w, t, t, t, t, t, t, t, t, t,
      t, t, t, t, t, t, w, w, w, w, w, t, t, t, t, t, t, t, t, t
    } );


    torpedoTexP1 = new Texture2D( gpu, 7, 12 ) ;
    torpedoTexP1.SetData<Color>( new Color[] {
      t, w, w, t, t, t, t,
      t, w, w, t, t, t, t,
      t, t, w, w, w, t, t,
      t, t, w, w, w, t, t,
      t, t, t, w, w, w, w,
      t, t, t, w, w, w, w,
      t, t, t, w, w, w, w,
      t, t, t, w, w, w, w,
      t, t, w, w, w, t, t,
      t, t, w, w, w, t, t,
      t, w, w, t, t, t, t,
      t, w, w, t, t, t, t
    });


    torpedoTexP2 = new Texture2D( gpu, 7, 12 );
    torpedoTexP2.SetData<Color>( new Color[] {
      t, t, t, t, t, t, t,
      t, t, t, t, t, t, t,
      w, w, t, t, t, t, t,
      w, w, t, t, t, t, t,
      t, t, t, w, w, w, w,
      t, t, t, w, w, w, w,
      t, t, t, w, w, w, w,
      t, t, t, w, w, w, w,
      w, w, t, t, t, t, t,
      w, w, t, t, t, t, t,
      t, t, t, t, t, t, t,
      t, t, t, t, t, t, t
    } );


    stars = new List<Sprite>();
    for( int i = 0; i < 20; i++ )
    {
      stars.Add( new Sprite( whitePX, SPW.rand.Next( 0, ScreenWidth ), SPW.rand.Next( 0, ScreenHeight ), 1, 2 ) );
    }

    torpedos = new List<Projectile>();
  }
}



