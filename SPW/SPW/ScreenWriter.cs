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


public class StringItem
{
  private Color initColor;
  private Color finalColor;
  public float life;
  public string message;
  public Vector2 pos;
  public bool isActive;


  public StringItem()
  {
    isActive = false;
  }

  public StringItem( string msg ) : this( msg, 20, 20, 3.0f, Color.White, Color.White ) { }

  public StringItem( string msg, int x, int y ) : this( msg, x, y, 3.0f, Color.White, Color.White ) { }

  public StringItem( string msg, int x, int y, float i_life ) : this( msg, x, y, i_life, Color.White, Color.White ) { }

  public StringItem( string msg, int x, int y, float i_life, Color i_color ) : this( msg, x, y, i_life, i_color, Color.White ) { }

  public StringItem( string msg, int x, int y, float i_life, Color i_color, Color f_color )
  {
    message = msg;
    pos = new Vector2( x, y );
    life = i_life;
    initColor = i_color;
    finalColor = f_color;
    finalColor.A = 0;  // force fadeout
    isActive = true;
  }

  public Color Color
  {
    get
    {
      if( life < 1 )
        return new Color( Vector4.Lerp( finalColor.ToVector4(), initColor.ToVector4(), life ) );
      else
        return initColor;
    }
  }

  /// <summary>
  /// Provides debug spew of this StringItem
  /// </summary>
  /// <returns>String with data about this instance</returns>
  public override string ToString()
  {
    return base.ToString() +
      ": '" + this.message + "' " +
      ( this.isActive ? "is" : "is not" ) + " active " +
      ( this.life < 0 ? "is dead " : "has " + this.life + " left " ) +
      "positioned at " + this.pos.ToString();
  }
}



public class ScreenWriter : DrawableGameComponent
{
  private Dictionary<string, StringItem> history;
  private List<StringItem> err;  // the error list is sequential.

  private SpriteBatch sb;
  private ContentManager Content;

  /////
  // You can change these.
  public SpriteFont sf;
  public SpriteFont errFont;
  public Color errColorStart;
  public Color errColorEnd;



  public ScreenWriter( Game g )
    : base( g )
  {
    history = new Dictionary<string, StringItem>();
    err = new List<StringItem>();

    errColorStart = Color.White;
    errColorEnd = Color.TransparentWhite;

    Content = new ContentManager( g.Services );
  }

  protected override void LoadContent()
  {
    sb = new SpriteBatch( this.GraphicsDevice );
    try
    {
      errFont = sf = this.Game.Content.Load<SpriteFont>( "screenwriterFont" );
    }
    catch( Exception e )
    {
      throw new Exception( "You ninny!  You must provide a SpriteFont called \"screenwriterFont\" for the ScreenWriter engine to use!\n\n" + e.Message );
    }

    base.LoadContent();
  }

  public StringItem this[ string id ]
  {
    get
    {
      if( history.ContainsKey( id ) )
        return history[ id ];
      else
        return null;
    }
    set
    {
      if( history.ContainsKey( id ) )
        history[ id ] = value;
      else
        history.Add( id, value );
    }
  }

  public override void Update( GameTime gameTime )
  {
    foreach( StringItem si in history.Values )
    {
      if( si.isActive )
      {
        si.life -= (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;

        if( si.life < 0 )
        {
          si.isActive = false;
        }
      }
    }
  }

  public void ReactivateLastDeactivated()
  {
    float leastDead = 0.0f ;
    string leastDeadIndex = string.Empty ;
    foreach( KeyValuePair<string, StringItem> pair in history )
    {
      if( pair.Value.isActive == false &&  // looking for deactivated
          pair.Value.life < leastDead )    // AND most recently deactivated
      {
        // this one is the least dead so far
        leastDead = pair.Value.life ;

        // so remember it
        leastDeadIndex = pair.Key ;
      }
    }

    if( leastDeadIndex != string.Empty )
    {
      // reactivate least dead.
      history[ leastDeadIndex ].life = 5.0f ;
      history[ leastDeadIndex ].isActive = true;
    }
    else
    {
      Console.WriteLine(" I couldn't find any strings" ) ;
    }
  }

  public override void Draw( GameTime gameTime )
  {
    sb.Begin( SpriteBlendMode.AlphaBlend );


    foreach( StringItem si in history.Values )
    {
      if( si.isActive )
        sb.DrawString( sf, si.message, si.pos, si.Color );
    }

    sb.End();


    base.Draw( gameTime );
  }
}