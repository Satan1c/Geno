# -*- coding: utf-8 -*-

from discord import Guild, Member


class Server:
    def __init__(self, guild: Guild):
        self._id = f"{guild.id}"
        self.prefix = "-"
        self.music = {
            "skip_vote": False,
            "volume": 0.5,
            "volume_max": 250,
            "vote_skip_ratio": 0.5,
            "skip_votes": 0,
            "now_playing": {},
            "queue": [],
            "playlist": [],
            "last": {}
        }

    def get_dict(self) -> dict:
        return {
            "_id": self._id,
            "prefix": self.prefix,
            "music": self.music
        }


class User:
    def __init__(self, member: Member):
        self._id = f"{member.guild.id}"
        self.id = f"{member.id}"
        self.messages = 0,
        self.mute_time = None

    def get_dict(self) -> dict:
        return {
            "sid": self._id,
            "uid": self.id,
            "messages": 0,
            "mute_time": self.mute_time
        }