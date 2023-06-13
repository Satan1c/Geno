using Discord;
using Discord.Interactions;

namespace Geno.Utils.Types;

public class RegisterModal : IModal
{
	private const string m_title = "Hoyo registration";
	public string Title => m_title;
	
	[InputLabel("HoYoLab cookies")]
	[ModalTextInput("cookies", TextInputStyle.Paragraph, placeholder: "mi18nLang=...; ltoken=...; ltuid=...; account_id=...; cookie_token=...")]
	public string Cookies { get; set; }
}