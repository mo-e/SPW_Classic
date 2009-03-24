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
using InputEventSystem ;
#endregion


/// <summary>
/// Uses Aaron MacDougall's Windowing project
/// Source code available @ http://www.codeplex.com/wsx/
/// </summary>
public class Windowing
{
  #region init and vars
  // Reference to owning game object
  SPW game;

  public InputEvents input;
  public GUIManager gui;
  public LoginDialog loginDialog;

  public Windowing( Game g )
  {
    game = g as SPW ;

    this.input = new InputEvents( game );
    game.Components.Add( this.input );

    this.gui = new GUIManager( game );
    game.Components.Add( this.gui );

    this.gui.Initialize();
  }

  public void LoadContent()
  {
    this.gui.SkinTextureFileName = "Content/Textures/Black";
  }
  #endregion

  /// <summary>
  /// Returns true if a modal dialog is already displaying.
  /// </summary>
  public bool IsModalDialogAlreadyDisplaying
  {
    get
    {
      if( this.gui.GetModal() == null )
        return false ; // no window is showing
      else
        return true ; // a window is already showing
    }
  }

  /// <summary>
  /// Displays login dialog.
  /// </summary>
  /// <param name="onOK">Function to execute when user clicks OK.</param>
  /// <param name="onCancel">Function to execute when user clicks cancel.
  /// If you pass null here, then nothing will happen when the user
  /// clicks cancel.</param>
  public void DisplayLoginDialog( Action onOK, Action onCancel )
  {
    if( IsModalDialogAlreadyDisplaying == false )
    {
      // Create the login dialog, passing references to
      // the functions that are to be executed when the
      // user clicks ok, and when the user clicks cancel
      loginDialog = new LoginDialog( game, gui, onOK, onCancel );

      // Now show it, as a MODAL dialog (meaning that
      // no other GUI elements will be displayed as long
      // as this one hasn't been dismissed yet)
      loginDialog.Show( true );
    }
  }

  public void ShowMessage( string message )
  {
    // Only allow ONE DIALOG TO SHOW at a time.
    // You can change this if you want, but usually
    // you only want one dialog at a time, eh?
    if( IsModalDialogAlreadyDisplaying == false )
    {
      MessageBox mb = new MessageBox( game, gui, message, "Info", MessageBoxButtons.OK, MessageBoxType.None );
      mb.Show( true ) ;
    }
  }
}