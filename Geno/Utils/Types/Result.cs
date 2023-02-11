using Discord;
using Discord.Interactions;

namespace Geno.Utils.Types;

public class Result : IResult
{
	public Result(bool isSuccess, EmbedBuilder builder, bool isEphemeral, bool isDefered, InteractionCommandError? error = null, string? errorReason = null)
	{
		IsSuccess = isSuccess;
		Builder = builder;
		IsEphemeral = isEphemeral;
		IsDefered = isDefered;

		Error = error;
		ErrorReason = errorReason ?? string.Empty;
	}

	public InteractionCommandError? Error { get; }
	public string ErrorReason { get; }
	public bool IsSuccess { get; }
	public EmbedBuilder Builder { get; }
	public bool IsEphemeral { get; }
	public bool IsDefered { get; }
}