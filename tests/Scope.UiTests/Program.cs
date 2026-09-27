using Scope.App;
using Scope.Core;
using System.Drawing.Imaging;

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
					throw new Exception("Clipped button: "+button.Text);

			}

			Directory.CreateDirectory("artifacts/qa");
			using Bitmap bitmap=new(form.Width, form.Height);
			form.DrawToBitmap(bitmap, new(0, 0, form.Width, form.Height));
			bitmap.Save($"artifacts/qa/viewer-{j}.png", ImageFormat.Png);

		}

		form.Close();
		Application.DoEvents();
		Console.WriteLine("PASS: offline controls, two window sizes, render with full sample buffers");
		return 0;

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
