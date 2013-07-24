#region MIT.
// 
// Examples.
// Copyright (C) 2008 Michael Winsor
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// Created: Thursday, October 02, 2008 10:46:02 PM
// 
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Security.AccessControl;
using System.Text;
using System.Windows.Forms;
using Dialogs;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using Font = GorgonLibrary.Graphics.Font;

namespace GorgonLibrary.Example
{
	/// <summary>
	/// Main application form.
	/// </summary>
	public partial class MainForm 
		: Form
	{
	    private TextSprite txtspr;
	    private Font font;

		#region Methods.
		/// <summary>
		/// Handles the KeyDown event of the MainForm control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.KeyEventArgs"/> instance containing the event data.</param>
		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
				Close();
			if (e.KeyCode == Keys.S)
				Gorgon.FrameStatsVisible = !Gorgon.FrameStatsVisible;
            if (e.KeyCode == Keys.Z)
            {
                txtspr.Text = "Foobar!................asdffdeas";
                TextStatus();
            }
            if (e.KeyCode == Keys.X)
            {
                txtspr.Text = "Test";
                TextStatus();
            }
		}

		/// <summary>
		/// Handles the OnFrameBegin event of the Screen control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="FrameEventArgs"/> instance containing the event data.</param>
		private void Screen_OnFrameBegin(object sender, FrameEventArgs e)
		{
			// Clear the screen.
			Gorgon.Screen.Clear();
		    txtspr.Draw();
            Gorgon.Screen.Rectangle(0,0, txtspr.Size.X, txtspr.Size.Y, Color.Brown);
		}

		/// <summary>
		/// Handles the FormClosing event of the MainForm control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			// Perform clean up.
			Gorgon.Terminate();
		}

		/// <summary>
		/// Handles the Load event of the MainForm control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void MainForm_Load(object sender, EventArgs e)
		{
			try
			{
				// Initialize the library.
				Gorgon.Initialize();

				// Display the logo and frame stats.
				Gorgon.LogoVisible = false;
				Gorgon.FrameStatsVisible = false;

				// Set the video mode to match the form client area.
				Gorgon.SetMode(this);

				// Assign rendering event handler.
				Gorgon.Idle += new FrameEventHandler(Screen_OnFrameBegin);

				// Set the clear color to something ugly.
				Gorgon.Screen.BackgroundColor = Color.FromArgb(250, 245, 220);

			    LoadFont();

                txtspr = new TextSprite("txtspr", "Test", font, Color.Black);

                txtspr.SetPosition(1.0f,1.0f);
			    TextStatus();

			    RunMeasureLineTests();
				// Begin execution.
				Gorgon.Go();
			}
			catch (Exception ex)
			{
				UI.ErrorBox(this, "An unhandled error occured during execution, the program will now close.", ex.Message + "\n\n" + ex.StackTrace);
				Application.Exit();
			}
		}
		#endregion

        private void TextStatus()
        {
            var FontSize = txtspr.Size;

            Console.WriteLine("Size Method: Width: " + FontSize.X + "px Height: " + FontSize.Y + "px");
            var s = txtspr.MeasureLine(txtspr.Text);
            Console.WriteLine("MeasureLine Method: Width: " + s + "px");
            s = txtspr.MeasureLine("Foo Bar is a Bar Foo... ! I hate Foo Bars!");
            Console.WriteLine("MeasureLine Method: Width: " + s + "px");
        }

        private void RunMeasureLineTests()
        {
            Console.WriteLine("Running Tests...");
            TestMeasureLine("");
            TestMeasureLine(".");
            TestMeasureLine("..");
            TestMeasureLine("...");
            TestMeasureLine("....");
            TestMeasureLine(".....");
            TestMeasureLine("......");
            TestMeasureLine(".......");
            TestMeasureLine("........");
            TestMeasureLine(".........");
            TestMeasureLine("..........");
            TestMeasureLine("...........");
            TestMeasureLine("!");
            TestMeasureLine("!!");
            TestMeasureLine("!!!");
            TestMeasureLine("!!!!");
            TestMeasureLine("a.");
            TestMeasureLine("a!");
            TestMeasureLine("a");

            Console.WriteLine("Tests Complete.");
        }

        private void TestMeasureLine(string line)
        {
            var str = txtspr.Text;
            var w = txtspr.MeasureLine(line);
            txtspr.Text = line;
            var s = txtspr.Size;
            Console.WriteLine("SWidth: " + s.X + "px MWidth: " + w + "px Line: '" + line + "'");
            txtspr.Text = str;

        }

        private void LoadFont()
        {
            FileStream fs = new FileStream("CALIBRI.TTF", FileMode.Open);

            Font loadedFont = Graphics.Font.FromStream("calibri", fs, (int)fs.Length, 10, false);

            fs.Close();

            font = loadedFont;
        }

		#region Constructor/Destructor.
		/// <summary>
		/// Constructor.
		/// </summary>
		public MainForm()
		{
			InitializeComponent();
		}
		#endregion
	}
}