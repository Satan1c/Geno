using Discord.Interactions;

namespace Geno.Utils.Types;

public class DemotivatorTextModal : IModal
{
	[InputLabel("Upper text")]
	[ModalTextInput("upper", minLength: 0, maxLength: 126)]
	public string Upper { get; set; }

	[InputLabel("Lower text")]
	[ModalTextInput("lower", minLength: 0, maxLength: 126)]
	public string Lower { get; set; }

	public string Title => "Demotivator text";
}