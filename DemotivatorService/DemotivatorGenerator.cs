using System.Diagnostics;
using Discord;
using SkiaSharp;
using static DemotivatorService.Extensions;

namespace DemotivatorService;

public class DemotivatorGenerator : IDisposable
{
	private SKCanvas m_canvas = null!;
	private SKSurface m_surface = null!;
	
	private SKSize m_canvasSize;
	private SKRect m_imageRect;
	
	private TextData m_lowerTextData;
	private TextData m_upperTextData;

	public DemotivatorGenerator(string url, string? upperText = null, string? lowerText = null)
	{
		var clock = Stopwatch.StartNew();
		
		var stream = new HttpClient().GetAsync(url).GetAwaiter().GetResult().Content.ReadAsStream();
		
		clock.Stop();
		Console.WriteLine($"Image download: {clock.ElapsedMilliseconds}");
		
		clock.Restart();
		
		var sourceBitmap = SKBitmap.Decode(stream);
		Draw(sourceBitmap, upperText, lowerText);
		
		clock.Stop();
		Console.WriteLine($"Constructor draw: {clock.ElapsedMilliseconds}");
		clock.Reset();
		
		stream.Close();
		stream.Dispose();
	}

	public virtual void Dispose()
	{
		m_surface.Dispose();
		m_canvas.Dispose();
		GC.SuppressFinalize(m_surface);
		GC.SuppressFinalize(m_canvas);
		GC.SuppressFinalize(this);
	}

	public FileAttachment GetResult()
	{
		var clock = Stopwatch.StartNew();
		
		var file = m_surface.Snapshot().Encode(SKEncodedImageFormat.Png, 100).AsStream();
		var attach = new FileAttachment(file, "demotivator.png");
		
		clock.Stop();
		Console.WriteLine($"Result draw: {clock.ElapsedMilliseconds}");
		clock.Reset();
		
		return attach;
	}

	public FileAttachment OverDraw(string? upperText = null, string? lowerText = null)
	{
		Draw(SKBitmap.FromImage(m_surface.Snapshot()), upperText, lowerText);
		return GetResult();
	}

	private void Draw(SKBitmap sourceBitmap, string? upperText = null, string? lowerText = null)
	{
		var clock = Stopwatch.StartNew();
		
		sourceBitmap = sourceBitmap.ResizeImage(MaxSize, MinSize);
		var width = MathF.Round(sourceBitmap.Width * 1.3f, 0);
		var textHeight = 0;

		var upper = upperText?.Split('\n') ?? null;
		var lower = lowerText?.Split('\n') ?? null;
    
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
		var lowerPosition = (heightPosition + UpperSize * (upper?.Length ?? 1) * 1.3f) / height;
		
		m_upperTextData = new TextData(UpperPaint, upperPosition, m_canvasSize);
		m_lowerTextData = new TextData(LowerPaint, lowerPosition, m_canvasSize);

		m_canvas.AddBorder(ref m_imageRect);
		m_canvas.AddText(ref m_canvasSize, ref m_upperTextData, ref m_lowerTextData, upper, lower);
		
		clock.Stop();
		Console.WriteLine($"Draw time: {clock.ElapsedMilliseconds}");
		clock.Reset();
	}
}