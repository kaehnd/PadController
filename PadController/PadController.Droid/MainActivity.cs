using Android.App;
using Android.Widget;
using Android.OS;
using Android.Content.PM;
using Android.Views;

namespace PadController.Droid
{
	[Activity(
			MainLauncher = true,
			ConfigurationChanges = global::Uno.UI.ActivityHelper.AllConfigChanges,
			WindowSoftInputMode = SoftInput.AdjustPan | SoftInput.StateHidden,
			ScreenOrientation = ScreenOrientation.Landscape
		)]
	public class MainActivity : Windows.UI.Xaml.ApplicationActivity
	{
	}
}

