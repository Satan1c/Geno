# -*- coding: utf-8 -*-

from datetime import datetime

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


class Queue:
    def __init__(self, video):
        self.req = str(video.req['id'])
        self.url = str(video.video_url)
        self.title = str(video.title)

    def get_dict(self) -> dict:
        data = {
            "req": self.req,
            "url": self.url,
            "title": self.title
        }
        return data


class NowPlaying:
    def __init__(self, video):
        self.req = video.req['id']
        self.url = str(video.video_url)
        self.title = str(video.title),
        self.start_at = datetime.now()
        self.duration = video.duration
        self.thumb_url = str(video.thumbnail)
        self.channel_url = str(video.uploader['url'])
        self.channel_icon_url = str(video.uploader['icon'])
        self.channel_name = str(video.uploader['name'])

    def get_dict(self) -> dict:
        data = {
            "req": self.req,
            "url": self.url,
            "title": self.title,
            "start_at": self.start_at,
            "duration": self.duration,
            "thumb_url": self.thumb_url,
            "channel_url": self.channel_url,
            "channel_icon_url": self.channel_icon_url,
            "channel_name": self.channel_name
        }
        return data
