using SkiaSharp;

namespace DemotivatorService;

public readonly struct TextData
{
	public SKPaint Paint { get; init; }
	public float TextX { get; init; }
	public float TextY { get; init; }

	public TextData(SKPaint paint, float position, SKSize canvasSize)
	{
		Paint = paint;
		TextX = canvasSize.Width / 2f;
		TextY = canvasSize.Height * position + Paint.FontMetrics.CapHeight;
	}
}