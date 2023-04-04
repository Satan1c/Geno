namespace Database.Types;

public readonly struct GenshinIds
{
	public GenshinIds()
	{
	}

	public uint Eu { get; init; } = default;
	public uint Na { get; init; } = default;
	public uint As { get; init; } = default;
}