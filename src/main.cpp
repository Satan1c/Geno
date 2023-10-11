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

dpp::cluster bot("");

int main() {
	bot.intents = dpp::intents::i_all_intents;
	bot.on_log(dpp::utility::cout_logger());

	//bot.on_slashcommand(geno::categories::execute);
	bot.on_slashcommand([](const dpp::slashcommand_t &event) {
		if (event.command.get_command_name() != "transfer") return;
		event.reply(dpp::message("Processing").set_flags(dpp::m_ephemeral));
		for (const auto &id : dpp::find_guild(from_guild_id)->channels) {
			auto channel = *(dpp::find_channel(id));
			(channel.is_category() ? from_categories : from_channels).push_back(channel);
		}

		auto a1 = std::async(std::launch::async, [](){
			printf("a1\n");
			for (const auto &id: dpp::find_guild(to_guild_id)->channels) {
				try {
					bot.channel_delete(id);
				}catch(const std::exception&){}
			}
			printf("a1\n");
		});

		auto a2 = std::async(std::launch::async, [](){
			printf("a2\n");
			for (const auto &fromCategory: from_categories) {
				dpp::channel category{};
				category.set_guild_id	(to_guild_id)
						.set_name		(fromCategory.name)
						.set_type		(fromCategory.get_type());

				bot.channel_create(category, [fromCategory](const dpp::confirmation_callback_t &callback){
					printf("category create\n");
					to_categories[fromCategory.id] = callback.get<dpp::channel>();
					const auto a = to_categories;
				});
			}
			printf("a2\n");
		});

		auto a3 = std::async(std::launch::async, [](){
			
			printf("a3\n");

			while (to_categories.size() < from_categories.size()){}

			for (const auto &from_channel: from_categories) {
				to_categories[from_channel.id].set_position(from_channel.position);
				bot.channel_edit(to_categories[from_channel.id]);
			}
			printf("a3\n");
		});

		auto a4 = std::async(std::launch::async, [](){
			printf("a4\n");
			while (to_categories.size() < from_categories.size()){}

			for (const auto &from_channel: from_channels) {
				dpp::channel channel{};
				channel	.set_guild_id(to_guild_id)
						.set_name				(from_channel.name)
						.set_bitrate			(from_channel.bitrate)
						.set_topic				(from_channel.topic)
						.set_user_limit			(from_channel.user_limit)
						.set_rate_limit_per_user(from_channel.user_limit)
						.set_parent_id			(to_categories[from_channel.parent_id].id)
						.set_nsfw				(from_channel.is_nsfw())
						.set_type				(from_channel.get_type());

				bot.channel_create(channel, [from_channel](const dpp::confirmation_callback_t &callback){
					printf("channel create\n");
					to_channels[from_channel.id] = callback.get<dpp::channel>();
				});
			}
			printf("a4\n");
		});

		auto a5 = std::async(std::launch::async, [](){
			printf("a5\n");

			while (to_channels.size() < from_channels.size()){}

			for (const auto &from_channel: from_channels) {
				to_channels[from_channel.id].set_position(from_channel.position);
				bot.channel_edit(to_channels[from_channel.id]);
			}
			printf("a5\n");
		});
	});

	bot.on_ready([](const dpp::ready_t &event) {
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