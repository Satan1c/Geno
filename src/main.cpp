#include <dpp/dpp.h>

int main() {
	dpp::cluster bot(getenv("TEST"));

	bot.on_log(dpp::utility::cout_logger());

	bot.on_slashcommand([&bot](const dpp::slashcommand_t &event) {
		if (event.command.get_command_name() == "blep") {
			std::string animal = std::get<std::string>(event.get_parameter("animal"));
			event.reply(std::string("Blep! You chose") + animal);
		}
	});

	bot.on_ready([&bot](const dpp::ready_t &event) {
		printf("Loged %s %u\n", bot.me.username.c_str(), event.shard_id);
		if (dpp::run_once<struct register_bot_commands>()) {
			dpp::slashcommand newcommand("blep", "Send a random adorable animal photo", bot.me.id);
			newcommand.add_option(
					dpp::command_option(dpp::co_string, "animal", "The type of animal", true).
							add_choice(dpp::command_option_choice("Dog", std::string("animal_dog"))).
							add_choice(dpp::command_option_choice("Cat", std::string("animal_cat"))).
							add_choice(dpp::command_option_choice("Penguin", std::string("animal_penguin")
									   )
					)
			);

			bot.global_command_create(newcommand);
		}
	});

	bot.start(dpp::st_wait);

	return 0;
}