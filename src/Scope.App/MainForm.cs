using System.Text;
using System.Text.Json;
using Scope.Core;

namespace Scope.App;

public sealed class MainForm : Form

{

	private readonly ComboBox mode=new()
	{
		DropDownStyle=ComboBoxStyle.DropDownList,
		Width=90
	};
	private readonly TextBox address=new()
	{
		Width=175,
		PlaceholderText="Adres IP oscyloskopu"
	};
	private readonly Label usbNotice=new()
	{
		Text="USB - nietestowana, niewdrożona",
		AutoSize=true,
		Enabled=false,
		Padding=new(8, 8, 0, 0)
	};
	private readonly Button connect=Button("Połącz\r\nOffline", 124);
	private readonly CheckBox ch1=new()
	{
		Text="CH1",
		Checked=true,
		AutoSize=true,
		ForeColor=Color.FromArgb(255, 220, 50),
		Padding=new(0, 6, 0, 0)
	};
	private readonly CheckBox ch2=new()
	{
		Text="CH2",
		Checked=true,
		AutoSize=true,
		ForeColor=Color.FromArgb(80, 225, 230),
		Padding=new(0, 6, 0, 0)
	};
	private readonly CheckBox live=new()
	{
		Text="Podgląd",
		Checked=true,
		AutoSize=true,
		Padding=new(8, 6, 0, 0)
	};
	private readonly Button start=Button("Start", 90), stop=Button("Stop", 90), auto=Button("Auto", 90), capture=Button("Pobierz przebieg", 190), save=Button("Zapisz CSV", 150);
	private readonly Label status=new()
	{
		Text="Gotowy. Wybierz połączenie.",
		Dock=DockStyle.Fill,
		AutoEllipsis=true,
		Padding=new(10, 7, 10, 0)
	};
	private readonly Label detail=new()
	{
		Text="CH1 + CH2 | CSV: czas [s], napięcie [V]",
		Dock=DockStyle.Fill,
		AutoEllipsis=true,
		Padding=new(10, 7, 10, 0)
	};
	private readonly WavePlot plot=new()
	{
		Dock=DockStyle.Fill,
		Margin=new(10)
	};
	private readonly System.Windows.Forms.Timer timer=new()
	{
		Interval=750
	};
	private ScopeClient? scope;
	private Waveform[]? captured;
	private bool busy, closing, allowClose;
	private int[] Channels => new[]
{
ch1.Checked ? 1 : 0, ch2.Checked ? 2 : 0
}
.Where(x => x != 0).ToArray();
	private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SDS1102CML.Viewer", "settings.json");
	public MainForm()

	{

		Text="SIGLENT SDS1102CML+ - Viewer 0.2.0";
		Font=new("Consolas", 10);
		BackColor=Color.FromArgb(97, 97, 97);
		ForeColor=Color.WhiteSmoke;
		ClientSize=new(1000, 660);
		MinimumSize=new(870, 540);
		StartPosition=FormStartPosition.CenterScreen;
		AutoScaleMode=AutoScaleMode.Dpi;
		Icon=AppAssets.CreateIcon();
		mode.Items.Add("LAN");
		mode.SelectedIndex=0;
		connect.Height=44;
		MenuStrip menu=CreateMenu();
		TableLayoutPanel layout=new()

		{

			Dock=DockStyle.Fill,
			ColumnCount=1,
			RowCount=6,
			BackColor=BackColor

		};
		layout.RowStyles.Add(new(SizeType.Absolute, 60));
		layout.RowStyles.Add(new(SizeType.Absolute, 36));
		layout.RowStyles.Add(new(SizeType.Percent, 100));
		layout.RowStyles.Add(new(SizeType.Absolute, 49));
		layout.RowStyles.Add(new(SizeType.Absolute, 32));
		layout.RowStyles.Add(new(SizeType.Absolute, 32));
		FlowLayoutPanel top=new()

		{

			Dock=DockStyle.Fill,
			Padding=new(10, 4, 8, 4),
			WrapContents=false

		};
		top.Controls.AddRange([mode, address, connect, usbNotice]);
		FlowLayoutPanel selection=new()

		{

			Dock=DockStyle.Fill,
			Padding=new(14, 0, 8, 0),
			WrapContents=false

		};
		selection.Controls.AddRange([ch1, ch2, live]);
		FlowLayoutPanel actions=new()

		{

			Dock=DockStyle.Fill,
			Padding=new(10, 8, 8, 4),
			WrapContents=false

		};
		actions.Controls.AddRange([start, stop, auto, capture, save]);
		layout.Controls.Add(top, 0, 0);
		layout.Controls.Add(selection, 0, 1);
		layout.Controls.Add(plot, 0, 2);
		layout.Controls.Add(actions, 0, 3);
		layout.Controls.Add(detail, 0, 4);
		layout.Controls.Add(status, 0, 5);
		Controls.Add(layout);
		Controls.Add(menu);
		MainMenuStrip=menu;
		foreach (Control c in new Control[]
{
mode, address
})

		{

			c.BackColor=Color.FromArgb(145, 145, 145);
			c.ForeColor=Color.White;
			c.Font=Font;

		}

		ToolTip tips=new();
		tips.SetToolTip(start, "Wznawia poprzedni tryb wyzwalania. Gdy nieznany, uruchamia TRMD AUTO.");
		tips.SetToolTip(auto, "Auto Setup oscyloskopu. Zmienia ustawienia pomiaru.");
		tips.SetToolTip(live, "Cykliczne odczyty. Odznaczenie zatrzymuje tylko odświeżanie aplikacji.");
		tips.SetToolTip(capture, "Pobiera pełny dostępny blok próbek. Dla wspólnej akwizycji CH1/CH2 najpierw wybierz Stop.");
		tips.SetToolTip(save, "Zapisuje ostatni ręcznie pobrany przebieg, niezależnie od kolejnych odświeżeń podglądu.");
		connect.Click+=async (_, _) => await ConnectOrDisconnect();
		start.Click+=async (_, _) => await Command(s => s.Start(), "Start wysłany.");
		stop.Click+=async (_, _) => await Command(s => s.Stop(), "Stop wysłany.");
		auto.Click+=async (_, _) => await Command(s => s.Auto(), "Auto Setup wysłane.");
		capture.Click+=async (_, _) => await CaptureWaveform(true);
		save.Click+=async (_, _) => await SaveCsv();
		timer.Tick+=async (_, _) =>

{

	if (!busy && scope != null && live.Checked && Channels.Length > 0)
		await CaptureWaveform(false);

};
		ch1.CheckedChanged+=(_, _) => UpdateEnabled();
		ch2.CheckedChanged+=(_, _) => UpdateEnabled();
		LoadSettings();
		UpdateEnabled();
		timer.Start();

	}

	private static Button Button(string text, int width) => new()

	{

		Text=text,
		Width=width,
		Height=32,
		FlatStyle=FlatStyle.Standard,
		BackColor=Color.FromArgb(192, 192, 192),
		ForeColor=Color.Black,
		UseVisualStyleBackColor=false

	};
	private void UpdateEnabled()

	{

		bool connected=scope != null;
		connect.Enabled=!busy && !closing;
		connect.Text=connected ? "Rozłącz\r\nOnline" : "Połącz\r\nOffline";
		mode.Enabled=address.Enabled=!busy && !connected;
		start.Enabled=stop.Enabled=auto.Enabled=connected && !busy;
		capture.Enabled=connected && !busy && Channels.Length > 0;
		save.Enabled=captured != null && !busy;

	}

	private MenuStrip CreateMenu()

	{

		MenuStrip menu=new()

		{

			Dock=DockStyle.Top,
			BackColor=Color.FromArgb(82, 82, 82),
			ForeColor=Color.WhiteSmoke,
			Font=Font

		};
		ToolStripMenuItem about=new("O Aplikacji");
		ToolStripMenuItem author=new("Autor");
		ToolStripMenuItem license=new("Licencja");
		foreach (ToolStripMenuItem item in new[]

		{

			about, author, license

		})
		{

			item.BackColor=Color.FromArgb(82, 82, 82);
			item.ForeColor=Color.WhiteSmoke;
			item.Font=Font;

		}

		author.Click+=(_, _) => new InfoForm("Autor", "Mateusz Skipor\r\nInżynier Technik Elektroniki\r\nmskiporsklep@op.pl").Show(this);
		license.Click+=(_, _) => new InfoForm("Licencja", AppAssets.LicenseText, true).Show(this);
		about.DropDownItems.AddRange([author, license]);
		menu.Items.Add(about);
		return menu;


	}

	private async Task ConnectOrDisconnect()

	{

		if (busy)
			return;
		busy=true;
		UpdateEnabled();
		try

		{

			if (scope != null)

			{

				await Disconnect();
				return;

			}

			string host=address.Text.Trim();
			if (host.Length == 0)
				throw new InvalidOperationException("Wpisz adres IP oscyloskopu.");
			status.Text="Łączenie...";
			scope=await Task.Run(() =>

{

	IInstrumentTransport transport=new Vxi11Transport(host);
	ScopeClient client=new(transport);
	try

	{

		client.Initialize();
		return client;

	}

	catch
	{
		client.Dispose();
		throw;
	}


});
			status.Text=scope.Identity;
			SaveSettings();

		}

		catch (Exception ex)
		{
			ShowError(ex, true);
		}

		finally
		{
			busy=false;
			UpdateEnabled();
		}


	}

	private async Task Disconnect()

	{

		ScopeClient? old=scope;
		scope=null;
		plot.Stale=true;
		plot.Invalidate();
		if (old != null)
			await Task.Run(old.Dispose);
		status.Text="Rozłączono. Ostatnio pobrany przebieg można zapisać.";

	}

	private async Task Command(Action<ScopeClient> action, string message)

	{

		if (busy || scope == null)
			return;
		busy=true;
		UpdateEnabled();
		try

		{

			ScopeClient client=scope;
			await Task.Run(() => action(client));
			status.Text=message;

		}

		catch (Exception ex)
		{
			await Disconnect();
			ShowError(ex, true);
		}

		finally
		{
			busy=false;
			UpdateEnabled();
		}


	}

	private async Task CaptureWaveform(bool manual)

	{

		if (busy || scope == null)
			return;
		int[] channels=Channels;
		if (channels.Length == 0)
			return;
		busy=true;
		UpdateEnabled();
		try

		{

			if (manual)
				status.Text="Pobieranie przebiegu...";
			ScopeClient client=scope;
			Waveform[] result=await Task.Run(() => client.Capture(channels));
			plot.SetWaveforms(result);
			if (manual)
				captured=result;
			detail.Text=string.Join(" | ", result.Select(w => $"CH{w.Channel}: {w.Volts.Length:N0} pkt"))+" | "+DateTime.Now.ToString("HH:mm:ss");
			status.Text=manual ? "Pobrano. Zapisz CSV zapisze ten przebieg. Odczyty CH1/CH2 są sekwencyjne." : "Podgląd aktywny. Pobierz przebieg, aby zachować dane do CSV.";

		}

		catch (InvalidOperationException ex)
		{
			live.Checked=false;
			ShowError(ex, manual);
		}

		catch (Exception ex)
		{
			await Disconnect();
			ShowError(ex, manual);
		}

		finally
		{
			busy=false;
			UpdateEnabled();
		}


	}

	private async Task SaveCsv()

	{

		if (busy || captured == null)
			return;
		using SaveFileDialog dialog=new()

		{

			Filter="Przebieg CSV (*.csv)|*.csv",
			FileName="SDS1102CML_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".csv"

		};
		busy=true;
		UpdateEnabled();
		Waveform[] data=captured;
		try

		{

			if (dialog.ShowDialog(this) != DialogResult.OK)
				return;
			await Task.Run(() =>

{

	string temp=dialog.FileName+"."+Guid.NewGuid().ToString("N")+".tmp";
	try

	{

		using (StreamWriter writer=new(temp, false, new UTF8Encoding(true)))
			Csv.Write(writer, data);
		File.Move(temp, dialog.FileName, true);

	}

	finally
	{
		if (File.Exists(temp))
			File.Delete(temp);
	}


});
			status.Text="Zapisano: "+dialog.FileName;

		}

		catch (Exception ex)
		{
			ShowError(ex, true);
		}

		finally
		{
			busy=false;
			UpdateEnabled();
		}


	}

	private void ShowError(Exception ex, bool dialog)

	{

		status.Text=ex.Message;
		if (dialog)
			MessageBox.Show(this, ex.Message, "SDS1102CML+ Viewer", MessageBoxButtons.OK, MessageBoxIcon.Warning);

	}

	private void LoadSettings()

	{

		try

		{

			if (File.Exists(SettingsPath))

			{

				var settings=JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(SettingsPath));
				address.Text=settings?.GetValueOrDefault("address") ?? "";

			}


		}

		catch (Exception)
		{

		}


	}

	private void SaveSettings()

	{

		try

		{

			Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
			File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new Dictionary<string, string>
{

{
"address", address.Text.Trim()
}

}));

		}

		catch (Exception)
		{

		}


	}

	protected override async void OnFormClosing(FormClosingEventArgs e)

	{

		if (!allowClose)

		{

			e.Cancel=true;
			if (closing)
				return;
			closing=true;
			timer.Stop();
			UpdateEnabled();
			while (busy)
				await Task.Delay(100);
			await Disconnect();
			allowClose=true;
			Close();
			return;

		}

		timer.Dispose();
		base.OnFormClosing(e);

	}


}
