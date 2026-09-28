namespace Scope.App;

internal sealed class InfoForm : Form

{

	private readonly Image logo;

	public InfoForm(string title, string content, bool large=false)

	{

		Text=title;
		Font=new("Consolas", 10);
		BackColor=Color.FromArgb(97, 97, 97);
		ForeColor=Color.WhiteSmoke;
		Icon=AppAssets.CreateIcon();
		ClientSize=large ? new(760, 560) : new(540, 250);
		MinimumSize=large ? new(620, 440) : Size;
		StartPosition=FormStartPosition.CenterParent;
		FormBorderStyle=FormBorderStyle.FixedDialog;
		MaximizeBox=false;
		MinimizeBox=false;
		ShowInTaskbar=false;
		logo=AppAssets.CreateLogo();

		TableLayoutPanel layout=new()

		{

			Dock=DockStyle.Fill,
			ColumnCount=2,
			RowCount=2,
			Padding=new(16),
			BackColor=BackColor

		};
		layout.ColumnStyles.Add(new(SizeType.Absolute, 124));
		layout.ColumnStyles.Add(new(SizeType.Percent, 100));
		layout.RowStyles.Add(new(SizeType.Percent, 100));
		layout.RowStyles.Add(new(SizeType.Absolute, 44));
		PictureBox picture=new()

		{

			Image=logo,
			SizeMode=PictureBoxSizeMode.Zoom,
			Dock=DockStyle.Top,
			Height=112,
			Margin=new(0, 0, 12, 0)

		};
		TextBox information=new()

		{

			Text=content,
			ReadOnly=true,
			Multiline=true,
			ScrollBars=large ? ScrollBars.Vertical : ScrollBars.None,
			WordWrap=true,
			Dock=DockStyle.Fill,
			BackColor=Color.FromArgb(82, 82, 82),
			ForeColor=Color.WhiteSmoke,
			BorderStyle=BorderStyle.FixedSingle,
			Font=Font,
			Margin=new(0)

		};
		Button close=new()

		{

			Text="Zamknij",
			Width=110,
			Height=32,
			Anchor=AnchorStyles.Right,
			BackColor=Color.FromArgb(192, 192, 192),
			ForeColor=Color.Black,
			UseVisualStyleBackColor=false

		};
		close.Click+=(_, _) => Close();
		layout.Controls.Add(picture, 0, 0);
		layout.SetRowSpan(picture, 2);
		layout.Controls.Add(information, 1, 0);
		layout.Controls.Add(close, 1, 1);
		Controls.Add(layout);
		Shown+=(_, _) => close.Select();

	}

	protected override void Dispose(bool disposing)

	{

		if (disposing)
			logo.Dispose();
		base.Dispose(disposing);

	}


}
