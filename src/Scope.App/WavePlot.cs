using System.Drawing.Drawing2D;
using Scope.Core;

namespace Scope.App;

public sealed class WavePlot : Control

{

	private Waveform[] waves=[];
	[System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
	public bool Stale

	{

		get; set;

	}

	private Point? cursor;
	public WavePlot()

	{

		DoubleBuffered=true;
		BackColor=Color.FromArgb(43, 43, 43);
		ForeColor=Color.WhiteSmoke;
		SetStyle(ControlStyles.ResizeRedraw, true);
		MouseMove+=(_, e) =>
{
	cursor=e.Location;
	Invalidate();
};
		MouseLeave+=(_, _) =>
{
	cursor=null;
	Invalidate();
};

	}

	public void SetWaveforms(Waveform[] value)

	{

		waves=value;
		Stale=false;
		Invalidate();

	}

	protected override void OnPaint(PaintEventArgs e)

	{

		base.OnPaint(e);
		Graphics g=e.Graphics;
		Rectangle r=new(78, 28, Math.Max(1, Width-102), Math.Max(1, Height-86));
		using Pen grid=new(Color.FromArgb(75, 75, 75));
		using Brush text=new SolidBrush(ForeColor);
		if (r.Width < 20 || r.Height < 20)
			return;
		for (int i=0; i <= 10; i++)

		{

			float x=r.Left+r.Width*i/10f;
			g.DrawLine(grid, x, r.Top, x, r.Bottom);

		}

		for (int i=0; i <= 8; i++)

		{

			float y=r.Top+r.Height*i/8f;
			g.DrawLine(grid, r.Left, y, r.Right, y);

		}

		if (waves.Length == 0)

		{

			string label="Połącz oscyloskop, aby wyświetlić przebieg";
			SizeF size=g.MeasureString(label, Font);
			g.DrawString(label, Font, text, r.Left+(r.Width-size.Width)/2, r.Top+r.Height/2);
			return;

		}

		double t0=waves.Min(w => w.Start), t1=waves.Max(w => w.Start+(w.Volts.Length-1)*w.Interval);
		if (t1 <= t0)
			t1=t0+waves[0].Interval;
		double low=waves.Min(w => w.Volts.Min()), high=waves.Max(w => w.Volts.Max());
		double margin=Math.Max((high-low)*0.12, 0.01);
		low-=margin;
		high+=margin;
		for (int i=0; i <= 8; i++)

		{

			double v=high-(high-low)*i/8;
			g.DrawString(Engineering(v, "V"), Font, text, 2, r.Top+r.Height*i/8f-8);

		}

		for (int i=0; i <= 4; i++)

		{

			string label=Engineering(t0+(t1-t0)*i/4, "s");
			SizeF size=g.MeasureString(label, Font);
			float labelX=Math.Clamp(r.Left+r.Width*i/4f-size.Width/2, 0, Math.Max(0, Width-size.Width-4));
			g.DrawString(label, Font, text, labelX, r.Bottom+10);

		}

		g.SetClip(r);
		foreach (Waveform w in waves)

		{

			using Pen line=new(w.Channel == 1 ? Color.FromArgb(255, 220, 50) : Color.FromArgb(80, 225, 230), 1.25f);
			float X(int i) => (float)(r.Left+(w.Start+i*w.Interval-t0)/(t1-t0)*r.Width);
			float Y(double v) => (float)(r.Bottom-(v-low)/(high-low)*r.Height);
			if (w.Volts.Length <= r.Width*2)

			{

				PointF[] points=w.Volts.Select((v, i) => new PointF(X(i), Y(v))).ToArray();
				if (points.Length > 1)
					g.DrawLines(line, points);
				else
					g.DrawEllipse(line, points[0].X-1, points[0].Y-1, 2, 2);

			}

			else

			{

				// Min/max per display column preserves narrow peaks; export always uses original samples.
				for (int x=0; x < r.Width; x++)

				{

					double begin=t0+(t1-t0)*x/r.Width;
					double end=t0+(t1-t0)*(x+1)/r.Width;
					int first=Math.Max(0, (int)Math.Ceiling((begin-w.Start)/w.Interval));
					int last=Math.Min(w.Volts.Length, (int)Math.Ceiling((end-w.Start)/w.Interval));
					if (last <= first)
						continue;
					double min=w.Volts[first], max=min;
					for (int i=first+1; i < last; i++)

					{

						min=Math.Min(min, w.Volts[i]);
						max=Math.Max(max, w.Volts[i]);

					}

					g.DrawLine(line, r.Left+x, Y(min), r.Left+x, Y(max)+0.5f);

				}


			}


		}

		g.ResetClip();
		g.DrawString("Napięcie", Font, text, r.Left, 5);
		g.DrawString("Czas", Font, text, r.Right-40, r.Bottom+34);
		if (cursor is Point p && r.Contains(p))

		{

			using Pen cross=new(Color.FromArgb(160, 160, 160))

			{

				DashStyle=DashStyle.Dot

			};
			g.DrawLine(cross, p.X, r.Top, p.X, r.Bottom);
			double time=t0+(p.X-r.Left)*(t1-t0)/r.Width;
			string reading="t="+Engineering(time, "s");
			foreach (Waveform w in waves)

			{

				int i=(int)Math.Round((time-w.Start)/w.Interval);
				if (i >= 0 && i < w.Volts.Length)
					reading+=$"   CH{w.Channel}="+Engineering(w.Volts[i], "V");

			}

			g.DrawString(reading, Font, text, r.Left, r.Bottom+34);

		}

		if (Stale)

		{

			using Brush badge=new SolidBrush(Color.DarkRed);
			g.FillRectangle(badge, r.Left+10, r.Top+10, 260, 25);
			g.DrawString("DANE NIEAKTUALNE / OFFLINE", Font, text, r.Left+15, r.Top+14);

		}


	}

	private static string Engineering(double n, string unit)

	{

		double a=Math.Abs(n);
		(double factor, string prefix)=a switch

		{

			>=1e6 => (1e6, "M"),
			>=1e3 => (1e3, "k"),
			>=1 => (1, ""),
			>=1e-3 => (1e-3, "m"),
			>=1e-6 => (1e-6, "µ"),
			>0 => (1e-9, "n"),
			_ => (1, "")

		};
		return (n/factor).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)+" "+prefix+unit;

	}


}
