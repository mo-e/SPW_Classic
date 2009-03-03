using System;
using System.Runtime.InteropServices;

#region extras - don't really need this part

/// <summary>
/// Classful of static methods exposing functionality from Win32 API
/// You do NOT need to look at this, only if you are interested.
/// </summary>
public static class Win32
{
  /// <summary>
  /// Moves a window to where you want it.
  /// Note width and height are INCLUDING 
  /// the window's "trim".
  /// 
  /// There is a function AdjustWindowRect() function
  /// which you can use if interested in fixing.
  /// </summary>
  /// <param name="hwnd">Window.Handle</param>
  /// <param name="x">The x of where you want it.</param>
  /// <param name="y">The y of where you want it.</param>
  /// <param name="width">The width of a lunchbucket</param>
  /// <param name="height">The height of a skyscraper</param>
  /// <param name="doRepaint">Whether you want a repaint message to be sent to window after move.
  /// Your game draws 60 times sec, so don't worry about this one.</param>
  /// <returns>Success or fail</returns>
  [DllImport( "user32.dll" )]
  public static extern bool MoveWindow( IntPtr hwnd, int x, int y, int width, int height, bool doRepaint );


  /// <summary>
  /// Gets you information about system.
  /// </summary>
  /// <param name="nIndex">Integer index corresponding to value you want.
  /// Listing is bunch of constants starting with SM_ in winuser.h; e.g.
  /// SM_CXSCREENWIDTH is defined as equal to 0, and it gets you
  /// the x-resolution of the system's primary monitor.
  /// <see cref="http://msdn.microsoft.com/en-us/library/ms724385(VS.85).aspx"/></param>
  /// <returns>The value you are requesting, you ninny.</returns>
  [DllImport( "user32.dll" )]
  public static extern int GetSystemMetrics( int nIndex );

  /// <summary>
  /// Gives you the Handle of the Console Window.
  /// </summary>
  /// <returns>The Handle of the Console window, like
  /// the handle (anything),
  /// can be used to throw around/control
  /// the Console window.</returns>
  [DllImport( "Kernel32.dll" )]
  public static extern IntPtr GetConsoleWindow();
}


/// <summary>
/// Just a bunch of methods that get you
/// very specific things.
/// </summary>
public static class Win32Helper
{
  /// <summary>
  /// Get width (in pixels) of screen
  /// </summary>
  /// <returns>Width of primary monitor in px</returns>
  public static int GetScreenWidth()
  {
    return Win32.GetSystemMetrics( 0 ); // GetSystemMetrics( 0 ) gives screen width.
    // There are more.  defined @
    // http://msdn.microsoft.com/en-us/library/ms724385(VS.85).aspx
  }

  /// <summary>
  /// Get height (in pixels) of screen
  /// </summary>
  /// <returns>Duh, the height of the primary monitor in pix!</returns>
  public static int GetScreenHeight()
  {
    return Win32.GetSystemMetrics( 1 );  // GetSystemMetrics( 1 ) gives screen height.
    // really I could define a bunch of constants here,
    // such as ScreenHeightGettingConst = 1,
    // but this function only does one thing, so byah.
  }
}

#endregion
