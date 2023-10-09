#include "commands/categories.hpp"

namespace geno::categories {
	std::unordered_map<std::string_view, bool> cat_names = {};
	std::vector<dpp::slashcommand> commands;
	std::unordered_map<std::string, std::function<void(const dpp::slashcommand_t &event)>> main = {};

	void reg(const std::string &name,
			 const std::function<void(const dpp::slashcommand_t &event)> &&callback) {
		cat_names[name] = true;
		main[name] = callback;
	}

	void reg(const std::string &name,
			 const std::string &sub_name,
			 const std::function<void(const dpp::slashcommand_t &event)> &&callback) {
		cat_names[name] = true;
		main[name + " " + sub_name] = callback;
	}

	void reg(const std::string &name,
			 const std::string &sub_name,
			 const std::string &inner_name,
			 const std::function<void(const dpp::slashcommand_t &event)> &&callback) {
		cat_names[name] = true;
		main[name + " " + sub_name + " " + inner_name] = callback;
	}

	std::vector<dpp::snowflake>
	get_commands_to_delete(const std::unordered_map<dpp::snowflake, dpp::slashcommand> &cmds) {
		std::vector<dpp::snowflake> c;

		for (auto const &[id, cmd]: cmds) {
			if (cat_names.find(cmd.name) != cat_names.end()) c.push_back(id);
		}

		return c;
	}

	std::vector<dpp::slashcommand>
	get_commands_to_create(const std::unordered_map<dpp::snowflake, dpp::slashcommand> &cmds) {
		return commands;
	}

	void execute(const dpp::slashcommand_t &event) {
		std::function<void(const dpp::slashcommand_t &event)> *command = nullptr;

		dpp::interaction interaction = event.command;
		dpp::command_interaction cmd_data = interaction.get_command_interaction();

		std::string name = interaction.get_command_name();

		if (main.find(name) != main.end()) {
			command = &main[name];
		} else {
			std::string sub_name = cmd_data.options.empty()
								   ? ""
								   : name + " " + cmd_data.options[0].name;

			if (main.find(sub_name) != main.end()) {
				command = &main[sub_name];
			} else {
				std::string inner_name = cmd_data.options.empty() || cmd_data.options[0].options.empty()
										 ? ""
										 : sub_name + " " + cmd_data.options[0].options[0].name;

				if (main.find(inner_name) != main.end()) {
					command = &main[inner_name];
				}
			}
		}

		if (command != nullptr)
			(*command)(event);
	}
}