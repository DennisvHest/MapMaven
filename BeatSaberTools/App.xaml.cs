﻿using Application = Microsoft.Maui.Controls.Application;

namespace BeatSaberTools
{
    public partial class App : Application
	{
		public App()
		{
			InitializeComponent();

			MainPage = new MainPage();
		}
	}
}
