using Discord;
using Discord.Interactions;

namespace Geno.Utils.Types;

public class Result : RuntimeResult
{
	public Result(bool isSuccess, EmbedBuilder? builder, bool isEphemeral, bool isDefered,
		InteractionCommandError? error = null, string? errorReason = null) : base(error, errorReason)
	{
		IsSuccess = isSuccess;
		Builder = builder;
		IsEphemeral = isEphemeral;
		IsDefered = isDefered;

		Error = error;
		ErrorReason = errorReason ?? string.Empty;
	}

	public new InteractionCommandError? Error { get; }
	public new string ErrorReason { get; }
	public new bool IsSuccess { get; }
	public EmbedBuilder? Builder { get; }
	public bool IsEphemeral { get; }
	public bool IsDefered { get; }

	public static Task<RuntimeResult> GetTaskFor(bool isSuccess, EmbedBuilder? builder, bool isEphemeral,
		bool isDefered,
		InteractionCommandError? error = null, string? errorReason = null)
	{
		var result = new Result(isSuccess, builder, isEphemeral, isDefered, error, errorReason);
		return Task.FromResult<RuntimeResult>(result);
	}
}