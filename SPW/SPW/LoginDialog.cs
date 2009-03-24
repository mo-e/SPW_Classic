#region Using Statements
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using WindowSystem;
#endregion

public class LoginDialog : Dialog
{
  private const int SPACING = 10;

  private TextBox textBoxName;
  private TextBox textBoxPW;

  private TextButton buttonOK;
  private TextButton buttonCancel;

  private Action OnOK;
  private Action OnCancel ;

  #region getters
  public string EnteredUsername
  {
    get { return textBoxName.Text; }
  }

  public string EnteredPassword
  {
    get { return textBoxPW.Text; }
  }
  #endregion

  public LoginDialog( Game game, GUIManager guiManager, Action toExecuteOnOK, Action toExecuteOnCancel ) : base( game, guiManager )
  {
    #region set up the name label and its text field
    Label labelName = new Label( game, guiManager );
    this.Add( labelName );
    labelName.Text = "Name:";
    labelName.X = SPACING;
    labelName.Y = SPACING;
    labelName.Width = 75;
    labelName.Height = labelName.TextHeight;
    labelName.Color = Color.White;
    
    // Name textbox
    textBoxName = new TextBox( game, guiManager );
    Add( this.textBoxName );
    textBoxName.Initialize();
    textBoxName.X = labelName.X;  // lined up with its label
    textBoxName.Y = labelName.Y + labelName.Height + SPACING / 2;
    textBoxName.Color = Color.White;
    #endregion

    #region set up the password field and its text label
    Label labelPW = new Label( game, guiManager );
    this.Add( labelPW );
    labelPW.Text = "Password:";
    labelPW.X = labelName.X ;
    labelPW.Y = textBoxName.Y + 3 * SPACING;  // textboxname was previous
    labelPW.Width = 200;
    labelPW.Height = labelName.TextHeight;
    labelPW.Color = Color.White ;

    textBoxPW = new TextBox( game, guiManager );
    textBoxPW.IsPassword = true;
    Add( this.textBoxPW );
    textBoxPW.Initialize();
    textBoxPW.X = labelPW.X;
    textBoxPW.Y = labelPW.Y + labelPW.Height + SPACING;
    textBoxPW.Color = Color.White;
    #endregion

    // Set the window width to the default textbox width
    this.ClientWidth = textBoxName.Width + ( 2 * SPACING );

    // Cancel button
    #region set up the cancel and OK buttons
    buttonCancel = new TextButton( game, guiManager );
    Add( this.buttonCancel );
    buttonCancel.Text = "Cancel";
    buttonCancel.X = this.ClientWidth - this.buttonCancel.Width - SPACING;
    buttonCancel.Y = this.textBoxPW.Y + this.textBoxName.Height + SPACING;
    buttonCancel.Color = Color.White;

    buttonCancel.Click += new ClickHandler( buttonCancel_Click );
    if( toExecuteOnCancel != null )
      this.OnCancel = toExecuteOnCancel;

    // OK button
    buttonOK = new TextButton( game, guiManager );
    Add( this.buttonOK );
    buttonOK.Text = "OK";
    buttonOK.X = this.buttonCancel.X - SPACING - this.buttonOK.Width;
    buttonOK.Y = this.buttonCancel.Y;
    buttonOK.Color = Color.White;

    buttonOK.Click += new ClickHandler( buttonOK_Click );
    if( toExecuteOnOK != null )
      this.OnOK = toExecuteOnOK ;
    #endregion

    // Set the window height to the amount needed to show all controls
    this.ClientHeight = this.buttonOK.Y + this.buttonOK.Height + SPACING;

    // Set the window title
    this.TitleText = "Login!";
    this.Color = Color.White ;

    // This dialog does not need to be resized by the user
    this.Resizable = false;

    this.CenterWindow();
  }

  void buttonOK_Click( UIComponent sender )
  {
    this.SetDialogResult( DialogResult.OK );

    this.CloseWindow();

    // Now execute callback for OK, if was set
    if( this.OnOK != null )
      this.OnOK();
  }

  void buttonCancel_Click( UIComponent sender )
  {
    this.SetDialogResult( DialogResult.Cancel );

    this.CloseWindow();

    if( this.OnCancel != null )
      this.OnCancel();
  }



}