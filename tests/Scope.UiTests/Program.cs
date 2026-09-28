using Scope.App;
using Scope.Core;
using System.Drawing.Imaging;
using System.Security.Cryptography;

internal static class Program

{

	[STAThread]
	private static int Main(string[] args)

	{

		ApplicationConfiguration.Initialize();
		Application.SetDefaultFont(new Font("Consolas", 10));
		using MainForm form=new();
		form.ShowInTaskbar=false;
		form.Opacity=0;
		form.Show();
		Application.DoEvents();
		Control[] controls=Walk(form).ToArray();
		AssertApplicationIdentity(form);
		AssertConnectionControls(controls);
		AssertAboutMenu(form);
		if (controls.OfType<Button>().First(b => b.Text == "Zapisz CSV").Enabled)
			throw new Exception("CSV enabled before capture");
		if (controls.OfType<Button>().First(b => b.Text == "Start").Enabled)
			throw new Exception("Start enabled offline");
		WavePlot plot=controls.OfType<WavePlot>().Single();
		for (int j=0; j < 2; j++)

		{

			form.ClientSize=j == 0 ? new(1000, 660) : new(860, 510);
			Application.DoEvents();
			int n=20000;
			double[] a=Enumerable.Range(0, n).Select(i => 2*Math.Sin(i*2*Math.PI/5000)).ToArray();
			double[] b=Enumerable.Range(0, n).Select(i => Math.Sin(i*2*Math.PI/5000+0.6) > 0 ? 0.7 : -0.7).ToArray();
			plot.SetWaveforms([new(1, a, 1e-6, -0.01, DateTimeOffset.UnixEpoch), new(2, b, 1e-6, -0.01, DateTimeOffset.UnixEpoch)]);
			foreach (Label label in controls.OfType<Label>())
				if (label.Text.StartsWith("CH1 +"))
					label.Text="PRZYKŁAD WYGLĄDU - dane syntetyczne, bez połączenia z oscyloskopem";
			Application.DoEvents();
			foreach (Button button in controls.OfType<Button>().Where(c => c.Visible))

			{

				if (button.Right > button.Parent!.ClientSize.Width || button.Bottom > button.Parent.ClientSize.Height)
					throw new Exception($"Clipped button: {button.Text}; bounds={button.Bounds}; parent={button.Parent!.ClientRectangle}");

			}

			Directory.CreateDirectory("artifacts/qa");
			using Bitmap bitmap=new(form.Width, form.Height);
			form.DrawToBitmap(bitmap, new(0, 0, form.Width, form.Height));
			bitmap.Save($"artifacts/qa/viewer-{j}.png", ImageFormat.Png);

		}

		form.Close();
		Application.DoEvents();
		Console.WriteLine("PASS: application identity, offline controls, about windows, two window sizes, render with full sample buffers");
		return 0;

	}

	private static void AssertApplicationIdentity(MainForm form)
	{
		using Icon expected=new("siglent_sds1102cml+.ico");
		using Bitmap expectedBitmap=expected.ToBitmap();
		using Bitmap actualBitmap=form.Icon!.ToBitmap();
		if (!SHA256.HashData(BitmapBytes(expectedBitmap)).SequenceEqual(SHA256.HashData(BitmapBytes(actualBitmap))))
			throw new Exception("Main window does not use the supplied ICO icon");
	}

	private static void AssertConnectionControls(Control[] controls)
	{
		Button connection=controls.OfType<Button>().SingleOrDefault(button => button.Text == "Offline")
			?? throw new Exception("The connection button does not show only Offline");
		if (controls.OfType<Label>().Any(label => label.Text == "Offline"))
			throw new Exception("Offline is still displayed as a separate label");
		if (controls.OfType<Label>().Any(label => label.Text == "Podgląd nie zmienia ustawień oscyloskopu"))
			throw new Exception("The removed preview note is still visible");
		if (controls.OfType<Label>().Any(label => label.Text.Contains("USB", StringComparison.OrdinalIgnoreCase)))
			throw new Exception("USB implementation status is still visible in the main window");
		ComboBox mode=controls.OfType<ComboBox>().Single(combo => combo.Items.Cast<object>().Any(item => item?.ToString() == "LAN"));
		if (mode.Items.Count != 1 || mode.Items[0]?.ToString() != "LAN")
			throw new Exception("USB can still be selected as a connection method");
	}

	private static void AssertAboutMenu(MainForm form)
	{
		MenuStrip menu=form.MainMenuStrip ?? throw new Exception("Missing application toolbar");
		if (menu.RenderMode != ToolStripRenderMode.System)
			throw new Exception("The application toolbar does not use the standard Windows renderer");
		ToolStripMenuItem about=menu.Items.OfType<ToolStripMenuItem>().SingleOrDefault(item => item.Text == "O Aplikacji")
			?? throw new Exception("Missing O Aplikacji menu");
		ToolStripMenuItem author=about.DropDownItems.OfType<ToolStripMenuItem>().SingleOrDefault(item => item.Text == "Autor")
			?? throw new Exception("Missing Autor menu item");
		ToolStripMenuItem license=about.DropDownItems.OfType<ToolStripMenuItem>().SingleOrDefault(item => item.Text == "Licencja")
			?? throw new Exception("Missing Licencja menu item");

		author.PerformClick();
		Application.DoEvents();
		Form authorWindow=Application.OpenForms.Cast<Form>().Single(window => window.Text == "Autor");
		AssertInformationWindow(authorWindow, form, "Mateusz Skipor", "Inżynier Technik Elektroniki", "mskiporsklep@op.pl");
		CaptureWindow(authorWindow, "artifacts/qa/author.png");
		authorWindow.Close();

		license.PerformClick();
		Application.DoEvents();
		Form licenseWindow=Application.OpenForms.Cast<Form>().Single(window => window.Text == "Licencja");
		AssertInformationWindow(licenseWindow, form, "PolyForm Noncommercial License 1.0.0");
		CaptureWindow(licenseWindow, "artifacts/qa/license.png");
		licenseWindow.Close();
	}

	private static void AssertInformationWindow(Form window, MainForm main, params string[] expectedText)
	{
		Control[] controls=Walk(window).ToArray();
		string text=string.Join("\n", controls.Select(control => control.Text));
		if (window.BackColor != main.BackColor || window.Font.Name != "Consolas")
			throw new Exception(window.Text+" window does not match the application style");
		if (window.ShowIcon)
			throw new Exception(window.Text+" window still shows a title-bar graphic");
		if (controls.OfType<PictureBox>().Any())
			throw new Exception(window.Text+" window still contains artwork");
		if (controls.OfType<Button>().Any())
			throw new Exception(window.Text+" window still contains a close button");
		foreach (string expected in expectedText)
			if (!text.Contains(expected, StringComparison.Ordinal))
				throw new Exception(window.Text+" window is missing: "+expected);
	}

	private static void CaptureWindow(Form window, string path)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		using Bitmap bitmap=new(window.Width, window.Height);
		window.DrawToBitmap(bitmap, new(0, 0, window.Width, window.Height));
		bitmap.Save(path, ImageFormat.Png);
	}

	private static byte[] BitmapBytes(Bitmap bitmap)
	{
		using MemoryStream stream=new();
		bitmap.Save(stream, ImageFormat.Png);
		return stream.ToArray();
	}

	private static IEnumerable<Control> Walk(Control root)

	{

		foreach (Control control in root.Controls)

		{

			yield return control;
			foreach (Control child in Walk(control))
				yield return child;

		}


	}


}
