using System.Text;
using Discord;
using SkiaSharp;

namespace DemotivatorService;

public class DemotivatorGenerator : IDisposable
{
	private const int m_cMaxSize = 512;
	private const int m_cMinSize = 200;
	private const float m_cBorderThickness = 600 * 0.007f;
	private SKCanvas m_canvas = null!;
	private SKSize m_canvasSize;
	private SKRect m_imageRect;
	private TextData m_lowerTextData;

	private SKSurface m_surface = null!;

	private TextData m_upperTextData;

	public DemotivatorGenerator(string url, string? upperText = null, string? lowerText = null)
	{
		var stream = new HttpClient().GetAsync(url).GetAwaiter().GetResult().Content.ReadAsStream();
		var sourceBitmap = SKBitmap.Decode(stream);
		Draw(sourceBitmap, upperText, lowerText);
		stream.Close();
		stream.Dispose();
	}

	public void Dispose()
	{
		m_lowerTextData.Dispose();
		m_upperTextData.Dispose();
		m_surface.Dispose();
		m_canvas.Dispose();
	}

	public FileAttachment GetResult()
	{
		var file = m_surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
		return new FileAttachment(file, "demotivator.png");
	}

	public FileAttachment OverDraw(string? upperText = null, string? lowerText = null)
	{
		Draw(SKBitmap.FromImage(m_surface.Snapshot()), upperText, lowerText);
		return GetResult();
	}

	private void Draw(SKBitmap sourceBitmap, string? upperText = null, string? lowerText = null)
	{
		sourceBitmap = sourceBitmap.ResizeImage(m_cMaxSize, m_cMinSize);
		var width = MathF.Round(sourceBitmap.Width * 1.3f, 0);
		var textHeight = 0;

		var upper = upperText?.Split('\n') ?? null;
		var lower = lowerText?.Split('\n') ?? null;
    
		var upperPaint = TextData.GetPaint(TextData.GetTextSize(32, width, 4, m_cMaxSize));
		var lowerPaint = TextData.GetPaint(TextData.GetTextSize(16, width, 3, m_cMaxSize));

		if (upperText != null)
		{
			upper = WrapText(upperText, upperPaint, width * .87f);
			textHeight += upper.Length - 1;
		}

		if (lowerText != null)
		{
			lower = WrapText(lowerText, lowerPaint, width * .87f);
			textHeight += lower.Length - 1;
		}

		var heightAmplifier = .27f + .09f * textHeight;
		var height = MathF.Round(sourceBitmap.Height * (1 + heightAmplifier), 0);

		m_canvasSize = new SKSize(width, height);
		m_surface = m_canvasSize.CreateBlank();
		m_canvas = m_surface.Canvas;
		m_imageRect = m_canvas.AddImage(sourceBitmap);

		var heightPosition = sourceBitmap.Height * 1.105f;
		var upperPosition = heightPosition / height;
		var lowerPosition = (heightPosition + 32 * (upper?.Length ?? 1) * 1.3f) / height;
		
		m_upperTextData = new TextData(upperPaint, upperPosition, m_canvasSize);
		m_lowerTextData = new TextData(lowerPaint, lowerPosition, m_canvasSize);

		AddBorder();
		AddText(upper, lower);
	}

	private void AddText(string[]? upperText = null, string[]? lowerText = null)
	{
		if (upperText != null) DrawText(m_upperTextData, upperText);
		if (lowerText != null) DrawText(m_lowerTextData, lowerText);
	}

	private static string[] WrapText(string text, SKPaint paint, float width)
	{
		var lines = new LinkedList<string>();
		if (string.IsNullOrWhiteSpace(text)) return lines.ToArray();

		var words = text.Split(' ');
		var currentLine = new StringBuilder();
		foreach (var word in words)
		{
			var current = currentLine.ToString();
			if (currentLine.Length > 0 && paint.MeasureText(current + ' ' + word) > width)
			{
				lines.AddLast(current);
				currentLine.Clear();
			}

			currentLine.Append(word).Append(' ');
		}

		lines.AddLast(currentLine.ToString());
		return lines.ToArray();
	}

	private void AddBorder()
	{
		var borderPaint = new SKPaint
		{
			Color = SKColors.White, StrokeWidth = m_cBorderThickness, IsAntialias = true, Style = SKPaintStyle.Stroke
		};
		var borderRect = SKRect.Create(m_imageRect.Location, m_imageRect.Size);
		m_canvas.DrawRect(borderRect, borderPaint);
	}

	private void DrawText(TextData data, string[] text)
	{
		var y = data.TextY;
		var x = data.TextX;
		var paint = data.Paint;
		var lineHeight = paint.FontSpacing;
		var maxWidth = m_canvasSize.Width * .87f;

		foreach (var line in text)
		{
			var words = line.Split(' ');
			var currentLine = new StringBuilder();
			foreach (var word in words)
			{
				var current = currentLine.ToString();
				var width = paint.MeasureText(current + ' ' + word);
				if (width > maxWidth)
				{
					m_canvas.DrawText(current, x, y, paint);
					y += lineHeight;
					currentLine.Clear();
				}

				currentLine.Append(word).Append(' ');
			}

			m_canvas.DrawText(currentLine.ToString(), x, y, paint);
			y += lineHeight;
		}
	}
}