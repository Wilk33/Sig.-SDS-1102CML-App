using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Scope.App;

internal static class SystemTheme

{

	private const int DarkModeBefore20H1=19;
	private const int DarkMode=20;
	public static bool IsDark

	{

		get

		{

			if (SystemInformation.HighContrast)
				return false;
			if (Application.SystemColorMode == SystemColorMode.Dark)
				return true;
			try

			{

				using RegistryKey? key=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
				return key?.GetValue("AppsUseLightTheme") is int value && value == 0;

			}
			catch
			{
				return false;
			}

		}

	}

	public static void ApplyTitleBar(Form form)

	{

		if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17763))
			return;
		int enabled=IsDark ? 1 : 0;
		int result=DwmSetWindowAttribute(form.Handle, DarkMode, ref enabled, sizeof(int));
		if (result != 0)
			DwmSetWindowAttribute(form.Handle, DarkModeBefore20H1, ref enabled, sizeof(int));

	}

	[DllImport("dwmapi.dll")]
	private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);

}

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer

{

	public DarkMenuRenderer() : base(new DarkMenuColors())

	{

	}

	protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)

	{

		e.TextColor=e.Item.Enabled ? Color.WhiteSmoke : Color.Gray;
		base.OnRenderItemText(e);

	}

}

internal sealed class DarkMenuColors : ProfessionalColorTable

{

	private static readonly Color Background=Color.FromArgb(35, 35, 35);
	private static readonly Color Selection=Color.FromArgb(70, 70, 70);
	private static readonly Color Border=Color.FromArgb(95, 95, 95);
	public override Color ToolStripDropDownBackground => Background;
	public override Color ImageMarginGradientBegin => Background;
	public override Color ImageMarginGradientMiddle => Background;
	public override Color ImageMarginGradientEnd => Background;
	public override Color MenuBorder => Border;
	public override Color MenuItemBorder => Border;
	public override Color MenuItemSelected => Selection;
	public override Color MenuItemSelectedGradientBegin => Selection;
	public override Color MenuItemSelectedGradientEnd => Selection;
	public override Color MenuItemPressedGradientBegin => Background;
	public override Color MenuItemPressedGradientMiddle => Background;
	public override Color MenuItemPressedGradientEnd => Background;
	public override Color SeparatorDark => Color.FromArgb(80, 80, 80);
	public override Color SeparatorLight => Color.FromArgb(55, 55, 55);

}
