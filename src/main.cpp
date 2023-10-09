#include <iostream>
#include <mutex>
#include <string_view>
#include <syncstream>
#include <thread>

#include <dpp/dpp.h>

//#include "commands/categories.hpp"

std::mutex m;
std::mutex m2;
dpp::channel_map to_categories{};
dpp::channel_map to_channels{};

std::vector<dpp::channel> from_categories{};
std::vector<dpp::channel> from_channels{};

dpp::channel_map from_guild_channels;

const dpp::snowflake from_guild_id = 1154071384880853033;
const dpp::snowflake to_guild_id = 914900019021246494;

int main() {
	//dpp::cluster bot(getenv("Namix"));
	char * value;
	size_t size;
	_dupenv_s(&value, &size, "Namix") ;
	dpp::cluster bot(value);
	bot.intents = dpp::intents::i_all_intents;
	bot.on_log(dpp::utility::cout_logger());

	//bot.on_slashcommand(geno::categories::execute);
	bot.on_slashcommand([&bot](const dpp::slashcommand_t &event) {
		if (event.command.get_command_name() != "transfer") return;

		event.reply(dpp::message("Processing").set_flags(dpp::m_ephemeral));

		bot.channels_get(from_guild_id, [&](const dpp::confirmation_callback_t &callback) {
			if (callback.is_error()) {
				printf("bot.channels_get from_guild_id error %s\n", callback.get_error().message.c_str());
				return;
			}

			from_guild_channels = std::get<dpp::channel_map>(callback.value);

			bot.channels_get(to_guild_id, [&](const dpp::confirmation_callback_t &callback) {
				if (callback.is_error()) {
					printf("bot.channels_get to_guild_id error %s\n", callback.get_error().message.c_str());
					return;
				}

				std::jthread t1([&](){
					std::lock_guard lock(m);

					for (const auto &[id, channel]: std::get<dpp::channel_map>(callback.value)) {
						try {
							bot.channel_delete(id);
						}catch(const std::exception&){}
					}
				});

				t1.join();

				std::jthread t2([&](){
					std::lock_guard lock(m);

					for (const auto &[id, channel] : from_guild_channels) {
						(channel.is_category() ? from_categories : from_channels).push_back(channel);
					}
				});

				t2.join();

				std::jthread t3([&](){
					std::lock_guard lock(m);

					for (const auto &fromCategory: from_categories) {
						dpp::channel category{};
						category.set_guild_id	(to_guild_id)
								.set_name		(fromCategory.name)
								.set_type		(fromCategory.get_type());

						bot.channel_create(category, [&](const dpp::confirmation_callback_t &callback){
							std::lock_guard lock(m2);
							to_categories[fromCategory.id] = callback.get<dpp::channel>();
						});
					}
				});

				t3.join();

				std::jthread t4([&](){
					std::lock_guard lock(m);

					for (const auto &from_channel: from_categories) {
						to_categories[from_channel.id].set_position(from_channel.position);
						bot.channel_edit(to_categories[from_channel.id]);
					}
				});

				t4.join();

				std::jthread t5([&](){
					std::lock_guard lock(m);

					for (const auto &fromChannel: from_channels) {
						dpp::channel channel{};
						channel	.set_guild_id(to_guild_id)
								.set_name				(fromChannel.name)
								.set_bitrate			(fromChannel.bitrate)
								.set_topic				(fromChannel.topic)
								.set_user_limit			(fromChannel.user_limit)
								.set_rate_limit_per_user(fromChannel.user_limit)
								.set_parent_id			(to_categories[fromChannel.parent_id].id)
								.set_nsfw				(fromChannel.is_nsfw())
								.set_type				(fromChannel.get_type());

						bot.channel_create(channel, [&](const dpp::confirmation_callback_t &callback){
							std::lock_guard lock(m2);
							to_channels[fromChannel.id] = callback.get<dpp::channel>();
						});
					}
				});

				t5.join();

				std::jthread t6([&](){
					std::lock_guard lock(m);
					for (const auto &from_channel: from_channels) {
						to_channels[from_channel.id].set_position(from_channel.position);
						bot.channel_edit(to_channels[from_channel.id]);
					}
				});

				t6.join();
			});
		});
	});

	bot.on_ready([&bot](const dpp::ready_t &event) {
		if (dpp::run_once<struct register_bot_commands>()) {
			/*dpp::slashcommand transfer_command("transfer", "transfer", bot.me.id);

			bot.global_command_create(transfer_command, [](const dpp::confirmation_callback_t &) {
				printf("transfer registered\n");
			});*/


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