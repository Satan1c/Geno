using Discord.Interactions;

namespace Geno.Utils.Types;

public class DemotivatorTextModal : IModal
{
	[InputLabel("Upper text")]
	[ModalTextInput("upper", placeholder: "Text", minLength: 0, maxLength: 126)]
	public string Upper { get; set; } = "Text";

	[InputLabel("Lower text")]
	[ModalTextInput("lower", placeholder: "Also text, but lower", minLength: 0, maxLength: 126)]
	public string Lower { get; set; } = "Also text, but lower";

	public string Title => "Demotivator text";
}