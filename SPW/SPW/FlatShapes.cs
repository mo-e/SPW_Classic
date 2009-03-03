#region using...
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

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


// Class full of functions for drawing 2d shapes to the screen.
public static class FlatShapes
{
  // the view params.  these are set to some default values
  // that make sense for drawing 2d flat shapes.
  public static Vector3 eye = new Vector3( 0, 0, 2 );  // put the camera @ ( 0, 0, 2 )
  public static Vector3 look = new Vector3( 0, 0, 0 ); // look directly at the origin
  public static Vector3 up = new Vector3( 0, 1, 0 );   // up is +y

  //the drawing tool
  public static BasicEffect renderer;

  // default color to draw prims in
  public static Color color = Color.White;


  private static List<VertexPositionColor> pointList = new List<VertexPositionColor>();
  private static List<VertexPositionColor> lineList = new List<VertexPositionColor>();
  private static List<VertexPositionColor> triList = new List<VertexPositionColor>();



  // This is a function that's meant to be like
  // the spritebatch's spriteBatch.Begin() method.
  // You just call it before drawing your 2d primitives.
  // then you DRAW your 2d primitives, then you call
  // End() when you're done.
  public static void Begin()
  {
    renderer.GraphicsDevice.RenderState.CullMode = CullMode.None; // don't discard "backwards" wound
    // triangles. ( in d3d here, it would like to discard any CCW wound triangles by default )

    // so vertex coloring will have an effect
    renderer.VertexColorEnabled = true;


    // create the view matrix from the eye, look and up vectors.
    // basically sets the camera position, its orientation, and where its pointing.
    renderer.View = Matrix.CreateLookAt( eye, look, up );



    // create the projection matrix
    // link to the screen width and height variables
    // that are part of SPW.world


    renderer.Projection = Matrix.CreateOrthographicOffCenter(

      0,  // left-edge of screen is 0 px

      renderer.GraphicsDevice.Viewport.Width,   // right-edge of screen is (whatever screen width is set to)


      renderer.GraphicsDevice.Viewport.Height,  // counter-intuitive:  set the BOTTOM of the screen
      // to being the height ...

      0,  // ... and the TOP of the screen is 0.. so 
      // what happens is the origin is placed at the 
      // top left corner of the window.  we want that
      // so this FlatShapes thing will draw in the same
      // way that the SpriteBatch tends to draw (using
      // the top left corner of the screen as the origin,
      // and 1 unit of world space per pixel)

      1,  // near plane.. we are sitting on the z-axis @ ( 0, 0, 2 ).
      // looking towards ( 0, 0, 0 ).  So, we'll set to "start" seeing things
      // that are 1 units away, i.e. we'll see things starting from
      // ( 0, 0, 1 ), but we WOULD NOT be able to see anything "closer" than
      // that ( i.e. an object at ( 0, 0, 1.1 ) would not be visible to us, 
      // since we are sitting @ ( 0, 0, 2 ) and the near plane is 1 unit away).

      3 ); // set the far plane to 3 units.
    // this way we can see anything from ( 0, 0, 1 ),
    // which was set by the near plane,
    // to ( 0, 0, -1 ).  ( 0, 0, -1 ) is 3 units away
    // from ( 0, 0, 2 ), so anything beyond (0, 0, -1) (e.g.
    // an object at ( 0, 0, -1.2 ) would be 
    // invisible to us.

    // We intend to draw EVERYTHING with z=0, so setting the near
    // and far planes this way (1 full unit of buffer in front and
    // in back) will make certain that stuff with z=0 will be visible.



    // Start the renderer
    renderer.Begin();

    // start the rendering pass
    renderer.CurrentTechnique.Passes[ 0 ].Begin();

  }

  //public static void DrawPrimitives( List<Prim>
  public static void End()
  {
    // embed call to actually flush out drawing here, @ end

    renderer.GraphicsDevice.VertexDeclaration = new VertexDeclaration( renderer.GraphicsDevice, VertexPositionColor.VertexElements );

    if( pointList.Count > 0 )
    {
      renderer.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(

        PrimitiveType.PointList, pointList.ToArray(), 0, pointList.Count

      );
    }

    // draw the linelist, if not empty
    if( lineList.Count > 0 )
    {
      renderer.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(

        PrimitiveType.LineList, // drawing lines, one after another

        lineList.ToArray(),     // this is the array of vertices of those lines

        0,                      // start at position 0 of that array

        ( lineList.Count / 2 )    // and there are going to be HALF as many
        // lines to draw as there are points.. (because it takes 2 points
        // to specify each line)

      );
    }

    // draw the tri list, if not empty
    if( triList.Count > 0 )
    {
      renderer.GraphicsDevice.DrawUserPrimitives<VertexPositionColor>(

        PrimitiveType.TriangleList, triList.ToArray(), 0, ( triList.Count / 3 )

      );
    }



    renderer.CurrentTechnique.Passes[ 0 ].End();
    renderer.End();

    // clear the lists.
    pointList.Clear();
    lineList.Clear();
    triList.Clear();
  }


  #region point
  public static void Point( Vector2 pos )
  {
    Point( pos.X, pos.Y, color );
  }

  public static void Point( Vector2 pos, Color theColor )
  {
    Point( pos.X, pos.Y, theColor );
  }

  public static void Point( float x, float y )
  {
    Point( x, y, color );
  }

  public static void Point( float x, float y, Color theColor )
  {
    pointList.Add(

      new VertexPositionColor( new Vector3( x, y, 0 ), theColor )

    );
  }
  #endregion


  #region line

  // Specify several different ways to call the Line() function.
  // All-in-all they boil down to calling the last one in this
  // listing, but it might be more convenient to be able to
  // use any of the versions provided here.

  /// <summary>
  /// Draws a line for you from start to end using
  /// currently set color.
  /// </summary>
  /// <param name="start">The starting position of the line, in screen coordinates</param>
  /// <param name="end">The end position of the line, in screen coordinates</param>
  public static void Line( Vector2 start, Vector2 end )
  {
    Line( start.X, start.Y, color, end.X, end.Y, color );
  }

  /// <summary>
  /// Draws a line for you from start to end using
  /// colors you like.
  /// </summary>
  /// <param name="start">The starting position of the line, in screen coordinates</param>
  /// <param name="startColor">Color to use at starting point</param>
  /// <param name="end">The end position of the line, in screen coordinates</param>
  /// <param name="endColor">Color to use at end point</param>
  public static void Line( Vector2 start, Color startColor, Vector2 end, Color endColor )
  {
    Line( start.X, start.Y, startColor, end.X, end.Y, endColor );
  }

  public static void Line( float start_x, float start_y, float end_x, float end_y )
  {
    Line( start_x, start_y, color, end_x, end_y, color );
  }

  public static void Line( float start_x, float start_y, Color start_color, float end_x, float end_y, Color end_color )
  {
    lineList.Add( new VertexPositionColor( new Vector3( start_x, start_y, 0 ), start_color ) );
    lineList.Add( new VertexPositionColor( new Vector3( end_x, end_y, 0 ), end_color ) );
  }
  #endregion


  #region triangle
  public static void Tri( Vector2 v1, Vector2 v2, Vector2 v3 )
  {
    triList.Add( new VertexPositionColor( new Vector3( v1.X, v1.Y, 0 ), color ) );
    triList.Add( new VertexPositionColor( new Vector3( v2.X, v2.Y, 0 ), color ) );
    triList.Add( new VertexPositionColor( new Vector3( v3.X, v3.Y, 0 ), color ) );
  }

  public static void Tri( Vector2 v1, Color c1, Vector2 v2, Color c2, Vector2 v3, Color c3 )
  {
    triList.Add( new VertexPositionColor( new Vector3( v1.X, v1.Y, 0 ), c1 ) );
    triList.Add( new VertexPositionColor( new Vector3( v2.X, v2.Y, 0 ), c2 ) );
    triList.Add( new VertexPositionColor( new Vector3( v3.X, v3.Y, 0 ), c3 ) );
  }
  #endregion
}