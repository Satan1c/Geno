using SkiaSharp;

namespace DemotivatorService;

public readonly struct TextData
{
	public float TextSize { get; init; }
	public SKPaint Paint { get; init; }
	public float TextX { get; init; }
	public float TextY { get; init; }
	public SKFont Font { get; init; }

	public TextData(SKPaint paint, float position, SKSize canvasSize)
	{
		Paint = paint;
		TextX = canvasSize.Width / 2f;
		TextY = canvasSize.Height * position + Paint.FontMetrics.CapHeight;
		Font = GetFont(TextSize);
	}
	public TextData(float fontSize, float size, float position, SKSize canvasSize, int maxSize)
	{
		TextSize = GetTextSize(fontSize, canvasSize.Width, size, maxSize);
		Paint = GetPaint(TextSize);
		TextX = canvasSize.Width / 2f;
		TextY = canvasSize.Height * position + Paint.FontMetrics.CapHeight;
		Font = GetFont(TextSize);
	}

	public static float GetTextSize(float fontSize, float width, float size, float maxSize)
	{
		return Math.Max(fontSize * width / maxSize, size);
	}

	public static SKPaint GetPaint(float textSize)
	{
		return new SKPaint
		{
			Color = SKColors.White,
			TextSize = textSize,
			TextAlign = SKTextAlign.Center, IsAntialias = true
		};
	}

	public static SKFont GetFont(float textSize)
	{
		return new SKFont(SKTypeface.FromFamilyName("Arial"), textSize);
	}

	public void Dispose()
	{
		Paint.Dispose();
		Font.Dispose();
	}
}