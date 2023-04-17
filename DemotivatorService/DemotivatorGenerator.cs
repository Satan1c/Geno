using Discord;
using SkiaSharp;
using static DemotivatorService.Extensions;

namespace DemotivatorService;

public class DemotivatorGenerator : IDisposable
{
	private SKCanvas? m_canvas;
	private SKSurface? m_surface;
	
	private SKSize m_canvasSize;
	private SKRect m_imageRect;
	
	private TextData m_lowerTextData;
	private TextData m_upperTextData;

	public DemotivatorGenerator(string url, string? upperText = null, string? lowerText = null)
	{
		var stream = new HttpClient().GetAsync(url).GetAwaiter().GetResult().Content.ReadAsStream();

		var sourceBitmap = SKBitmap.Decode(stream);
		Draw(sourceBitmap, upperText, lowerText);
		
		stream.Close();
		stream.Dispose();
	}

	public virtual void Dispose()
	{
		m_surface?.Dispose();
		m_canvas?.Dispose();
		m_surface = null;
		m_canvas = null;
	}

	public FileAttachment GetResult()
	{
		var file = m_surface!.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
		return new FileAttachment(file, "demotivator.png");
	}

	public FileAttachment OverDraw(string? upperText = null, string? lowerText = null)
	{
		Draw(SKBitmap.FromImage(m_surface!.Snapshot()), upperText, lowerText);
		return GetResult();
	}

	private void Draw(SKBitmap sourceBitmap, string? upperText = null, string? lowerText = null)
	{
		upperText = upperText?.Trim();
		lowerText = lowerText?.Trim();
		sourceBitmap = sourceBitmap.ResizeImage(MaxSize, MinSize);
		var width = MathF.Round(sourceBitmap.Width * 1.3f, 0);
		var textHeight = 0;

		string[]? upper = null;
		string[]? lower = null;
    
		if (upperText != null)
		{
			upper = WrapText(upperText, UpperPaint, width * .87f);
			textHeight += upper.Length - 1;
		}

		if (lowerText != null)
		{
			lower = WrapText(lowerText, LowerPaint, width * .87f);
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
		var lowerPosition = (heightPosition + UpperSize * (upper?.Length ?? 1) * 1.105f) / height;
		
		m_upperTextData = new TextData(UpperPaint, upperPosition, m_canvasSize);
		m_lowerTextData = new TextData(LowerPaint, lowerPosition, m_canvasSize);

		m_canvas.AddBorder(ref m_imageRect);
		m_canvas.AddText(ref m_canvasSize, ref m_upperTextData, ref m_lowerTextData, upper, lower);
	}
}