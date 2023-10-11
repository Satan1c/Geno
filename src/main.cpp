#include <iostream>
#include <mutex>
#include <string_view>
#include <syncstream>
#include <thread>
#include <chrono>

#include <dpp/dpp.h>

//#include "commands/categories.hpp"
dpp::channel_map to_categories{};
dpp::channel_map to_channels{};

std::vector<dpp::channel> from_categories{};
std::vector<dpp::channel> from_channels{};

const dpp::snowflake from_guild_id = 1154071384880853033;
const dpp::snowflake to_guild_id = 914900019021246494;

int main() {
	char * v;
	size_t sz;
	_dupenv_s(&v, &sz, "NamixTest");
	dpp::cluster bot(v);

	bot.intents = dpp::intents::i_all_intents;
	bot.on_log(dpp::utility::cout_logger());

	//bot.on_slashcommand(geno::categories::execute);
	bot.on_slashcommand([&](const dpp::slashcommand_t &event) {
		if (event.command.get_command_name() != "transfer") return;
		event.reply(dpp::message("Processing").set_flags(dpp::m_ephemeral));

		for (const auto &id : dpp::find_guild(from_guild_id)->channels) {
			auto channel = *(dpp::find_channel(id));
			(channel.is_category() ? from_categories : from_channels).push_back(channel);
		}

		for (const auto &id: dpp::find_guild(to_guild_id)->channels) {
			bot.channel_delete(id);
		}

		for (const auto &fromCategory: from_categories) {
			dpp::channel category{};
			category.set_guild_id	(to_guild_id)
					.set_name		(fromCategory.name)
					.set_type		(fromCategory.get_type());

			bot.channel_create(category, [&, fromCategory](const dpp::confirmation_callback_t &callback) {
				if (callback.is_error()) return;
				to_categories[fromCategory.id] = callback.get<dpp::channel>();
				if (to_categories.size() != from_categories.size()) return;

				for (const auto &from_category: from_categories) {
					to_categories[from_category.id].set_position(from_category.position);
					bot.channel_edit(to_categories[from_category.id]);
				}

				for (const auto &fromChannel: from_channels) {
					dpp::channel channel{};
					channel.set_guild_id(to_guild_id)
							.set_name(fromChannel.name)
							.set_bitrate(fromChannel.bitrate)
							.set_topic(fromChannel.topic)
							.set_user_limit(fromChannel.user_limit)
							.set_rate_limit_per_user(fromChannel.user_limit)
							.set_parent_id(to_categories[fromChannel.parent_id].id)
							.set_nsfw(fromChannel.is_nsfw())
							.set_type(fromChannel.get_type());

					bot.channel_create(channel, [&, fromChannel](const dpp::confirmation_callback_t &callback) {
						if (callback.is_error()) return;
						to_channels[fromChannel.id] = callback.get<dpp::channel>();
						if (to_channels.size() == from_channels.size()) return;

						for (const auto &from_channel: from_channels) {
							to_channels[from_channel.id].set_position(from_channel.position);
							bot.channel_edit(to_channels[from_channel.id]);
						}
					});
				}
			});
		}
	});

	bot.on_ready([&](const dpp::ready_t &event) {
		if (dpp::run_once<struct register_bot_commands>()) {
			dpp::slashcommand transfer_command("transfer", "transfer", bot.me.id);

			bot.guild_command_create(transfer_command, from_guild_id, [](const dpp::confirmation_callback_t &) {
				printf("transfer registered\n");
			});


			/*dpp::command_option test_cmd(dpp::co_sub_command, "test_command","test command");
			test_cmd.add_option(dpp::command_option(dpp::co_string, "just_string", "nothing", true));

			dpp::command_option test_group(dpp::co_sub_command_group, "test_sub", "test sub desc");
			test_group.add_option(test_cmd);

			dpp::slashcommand c("test_cat", "test cat desc", bot.me.id);
			c.add_option(test_group)
			.add_option(dpp::command_option(dpp::co_sub_command, "test_sub_2", "test sub 2 desc"));

			dpp::slashcommand cc("another_cat", "another cat desc", bot.me.id);
			cc.add_option(dpp::command_option(dpp::co_sub_command, "another_command","another command"));

			dpp::slashcommand ccc("just_command", "just command desc", bot.me.id);

			geno::categories::reg("just_command", [&bot](const dpp::slashcommand_t &e) {
				auto cmds = bot.global_commands_get_sync();
				std::cout << "just_command\n";
			});

			geno::categories::reg("test_cat", "test_sub_2", [](const dpp::slashcommand_t &e) {
				std::cout << "test_cat test_sub_2\n";
			});

			geno::categories::reg("test_cat", "test_sub", "test_command", [](const dpp::slashcommand_t &e) {
				std::cout << "test_cat test_sub test_command\n";
			});

			auto cmds = bot.global_commands_get_sync();
			bot.global_bulk_command_create(geno::categories::get_commands_to_create(cmds));
			for (const auto& id : geno::categories::get_commands_to_delete(cmds)) {
				bot.global_command_delete(id);
			}*/
		}

		printf("Loged %s %u\n", bot.me.username.c_str(), event.shard_id);
	});

	bot.start(dpp::st_wait);

	return 0;
}