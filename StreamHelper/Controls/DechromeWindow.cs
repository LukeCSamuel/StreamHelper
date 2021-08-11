using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace StreamHelper.Controls
{
	public class DechromeWindow : Window
	{
		public static readonly DependencyProperty CanCloseProperty =
			DependencyProperty.Register(nameof(CanClose), typeof(bool), typeof(DechromeWindow));
		public bool CanClose
		{
			get => (bool)GetValue(CanCloseProperty);
			set => SetValue(CanCloseProperty, value);
		}

		public DechromeWindow () : base ()
		{
			CanClose = true;
		}

		public override void OnApplyTemplate ()
		{
			(Template.FindName("PART_CloseButton", this) as Button).Click += CloseWindow;
			(Template.FindName("PART_MinimizeButton", this) as Button).Click += MinimizeWindow;
			(Template.FindName("PART_MaximizeButton", this) as Button).Click += MaximizeWindow;

			base.OnApplyTemplate();
		}

		void CloseWindow (object sender, RoutedEventArgs e)
		{
			if (CanClose)
			{
				Close();
			}
		}

		void MaximizeWindow (object sender, RoutedEventArgs e)
		{
			WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
		}

		void MinimizeWindow (object sender, RoutedEventArgs e)
		{
			WindowState = WindowState.Minimized;
		}
	}
}
