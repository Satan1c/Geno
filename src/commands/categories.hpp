#pragma once

#include <dpp/dpp.h>
#include <string>
#include <unordered_map>
#include <functional>
#include <cstdio>

namespace geno::categories {
	extern std::vector<dpp::slashcommand> commands;
	extern std::unordered_map<std::string, std::function<void(const dpp::slashcommand_t &event)>> main;

	void execute(const dpp::slashcommand_t &event);

	std::vector<dpp::snowflake>
	get_commands_to_delete(const std::unordered_map<dpp::snowflake, dpp::slashcommand> &cmds);

	std::vector<dpp::slashcommand>
	get_commands_to_create(const std::unordered_map<dpp::snowflake, dpp::slashcommand> &cmds);

	void reg();

	void reg(const std::string &name,
			 const std::function<void(const dpp::slashcommand_t &event)> &&callback);

	void reg(const std::string &name,
			 const std::string &sub_name,
			 const std::function<void(const dpp::slashcommand_t &event)> &&callback);

	void reg(const std::string &name,
			 const std::string &sub_name,
			 const std::string &inner_name,
			 const std::function<void(const dpp::slashcommand_t &event)> &&callback);
}