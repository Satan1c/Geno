using Discord;
using Discord.Interactions;

namespace Geno.Utils.Types;

public class RegisterModal : IModal
{
	private const string m_title = "Hoyo registration";

	[InputLabel("HoYoLab cookies")]
	[ModalTextInput("cookies", TextInputStyle.Paragraph,
		"mi18nLang=...; ltoken=...; ltuid=...; account_id=...; cookie_token=...")]
	public string Cookies { get; set; }

	public string Title => m_title;
}