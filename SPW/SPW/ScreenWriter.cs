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



/// <summary>
/// A StringItem represents some text on the screen
/// with its own position and lifetime to display for
/// </summary>
public class StringItem
{
  /// <summary>
  /// The color the String should start at
  /// </summary>
  private Color initColor;

  /// <summary>
  /// The color the String should fade out to
  /// as it comes to the end of its life
  /// </summary>
  private Color finalColor;

  /// <summary>
  /// The number of seconds to display the String item for
  /// </summary>
  public float life;

  /// <summary>
  /// The actual text of the string to display
  /// </summary>
  public string message;

  /// <summary>
  /// WHERE on the screen to display it
  /// </summary>
  public Vector2 pos;


  /// <summary>
  /// Whether or not to actually DRAW IT.  Once
  /// a StringItem's life is up, it gets deactivated
  /// (but not deleted, so it can be revived in case
  /// user missed seeing the message)
  /// </summary>
  public bool isActive;


  public StringItem()
  {
    isActive = false;
  }

  // In the next few overloaded constructors, they ALL actually pass down
  // into what I'm calling the "MASTER CONSTRUCTOR"
  public StringItem( string msg ) : this( msg, 20, 20, 3.0f, Color.White, Color.White ) { }

  public StringItem( string msg, int x, int y ) : this( msg, x, y, 3.0f, Color.White, Color.White ) { }

  public StringItem( string msg, int x, int y, float lifeTime ) : this( msg, x, y, lifeTime, Color.White, Color.White ) { }

  public StringItem( string msg, int x, int y, float lifeTime, Color startColor ) : this( msg, x, y, lifeTime, startColor, Color.White ) { }

  /// <summary>
  /// I'm calling this the "MASTER CONSTRUCTOR", its the
  /// constructor that ALL overload calls eventually end up calling.
  /// </summary>
  /// <param name="msg">The message to display</param>
  /// <param name="x">Where to display it in x</param>
  /// <param name="y">Where to display it in y</param>
  /// <param name="i_life">Number of seconds to display for</param>
  /// <param name="i_color">Starting color</param>
  /// <param name="f_color">End color when fade out</param>
  public StringItem( string msg, int x, int y, float lifeTime, Color startColor, Color fadeToColor )
  {
    message = msg;
    pos = new Vector2( x, y );
    life = lifeTime;
    initColor = startColor;
    finalColor = fadeToColor;
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

  private SpriteBatch sb;

  /////
  // You can change these.
  public SpriteFont sf;
  public SpriteFont errFont;


  public ScreenWriter( Game g )
    : base( g )
  {
    // initialize the "history" object (which is just
    // a collection of all the strings being displayed
    // on the screen at the present time)
    history = new Dictionary<string, StringItem>();
  }

  protected override void LoadContent()
  {
    sb = new SpriteBatch( this.GraphicsDevice );
    try
    {
      // Use the Content object of the Game class (the SPW class)
      // that this GameComponent belongs to try and load a font
      errFont = sf = this.Game.Content.Load<SpriteFont>( "screenwriterFont" );
    }
    catch( Exception e )
    {
      // You have to supply a font called screenWriterFont in your project
      // for the ScreenWriter object to draw its text with.

      // (like, you must right click the "Content" folder, Add New Item..
      // SpriteFont item and CALL IT screenwriterFont)
      throw new Exception( "You ninny!  You must provide a SpriteFont called \"screenwriterFont\" for the ScreenWriter engine to use!\n\n" + e.Message );
    }

    base.LoadContent();
  }

  /// <summary>
  /// Adds a new StringItem to the collection
  /// of StringItems to display on the screen.
  /// </summary>
  /// <param name="id">The ID of the string to display
  /// (this is NOT what gets shown.. this is just how
  /// you can overwrite /erase a string that's 
  /// already on the screen)</param>
  /// <returns>The string item object referenced
  /// by the ID you are passing</returns>
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
      // if a StringItem by this id is already there...
      if( history.ContainsKey( id ) )
        history[ id ] = value; //...then overwrite it
      else
        history.Add( id, value ); //...add for the first time
    }
  }


  // "Steps" each StringItem in the history
  // collection forward in time.
  // "Stepping forward" for a StringItem just
  // decreases its lifetime by some incremental amount
  public override void Update( GameTime gameTime )
  {
    foreach( StringItem si in history.Values )
    {
      if( si.isActive )
      {
        // reduce life left
        si.life -= (float)gameTime.ElapsedGameTime.Ticks / TimeSpan.TicksPerSecond;

        if( si.life < 0 )
        {
          //deactivate it so it stops displaying
          si.isActive = false;

          // The reason the StringItems aren't removed is
          // so that they can be re-activated in case the
          // user missed the message and wants to see it again
        }
      }
    }
  }


  // Revives the last message that was last deactivated
  public void ReactivateLastDeactivated()
  {
    float leastDead = 0.0f;
    string leastDeadIndex = string.Empty;
    foreach( KeyValuePair<string, StringItem> pair in history )
    {
      if( pair.Value.isActive == false &&  // looking for deactivated
          pair.Value.life < leastDead )    // AND most recently deactivated
      {
        // this one is the least dead so far
        leastDead = pair.Value.life;

        // so remember it
        leastDeadIndex = pair.Key;
      }
    }

    if( leastDeadIndex != string.Empty )
    {
      // reactivate least dead.
      history[ leastDeadIndex ].life = 5.0f;
      history[ leastDeadIndex ].isActive = true;
    }
    else
    {
      Console.WriteLine( " I couldn't find any strings" );
    }
  }


  // Draws all the ACTIVE StringItems in the history
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