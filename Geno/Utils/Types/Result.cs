using Discord;
using Discord.Interactions;

namespace Geno.Utils.Types;

public class Result : RuntimeResult
{
	public Result(
		EmbedBuilder builder, bool isSuccess = false, bool isEphemeral = true, bool isDefered = false,
		InteractionCommandError error = InteractionCommandError.Unsuccessful, string errorReason = "") : base(error, errorReason)
	{
		IsSuccess = isSuccess;
		Builder = builder;
		IsEphemeral = isEphemeral;
		IsDefered = isDefered;
	}
	public new bool IsSuccess { get; }
	public EmbedBuilder Builder { get; }
	public bool IsEphemeral { get; }
	public bool IsDefered { get; }

	public static Task<RuntimeResult> GetTaskFor(EmbedBuilder builder, bool isSuccess = false, bool isEphemeral = true, bool isDefered = false,
		InteractionCommandError error = InteractionCommandError.Unsuccessful, string errorReason = "")
	{
		var result = new Result(builder, isSuccess, isEphemeral, isDefered, error, errorReason);
		return Task.FromResult<RuntimeResult>(result);
	}
}