using System.Runtime.Serialization;

namespace WaifuPicsApi.Enums;

public enum SfwCategory
{
    [EnumMember(Value = "waifu")]
    Waifu,
    [EnumMember(Value = "neko")]
    Neko,
    [EnumMember(Value = "shinobu")]
    Shinobu,
    [EnumMember(Value = "megumin")]
    Megumin,
    [EnumMember(Value = "bully")]
    Bully,
    [EnumMember(Value = "cuddle")]
    Cuddle,
    [EnumMember(Value = "cry")]
    Cry,
    [EnumMember(Value = "hug")]
    Hug,
    [EnumMember(Value = "awoo")]
    Awoo,
    [EnumMember(Value = "kiss")]
    Kiss,
    [EnumMember(Value = "lick")]
    Lick,
    [EnumMember(Value = "pat")]
    Pat,
    [EnumMember(Value = "smug")]
    Smug,
    [EnumMember(Value = "bonk")]
    Bonk,
    [EnumMember(Value = "yeet")]
    Yeet,
    [EnumMember(Value = "blush")]
    Blush,
    [EnumMember(Value = "smile")]
    Smile,
    [EnumMember(Value = "wave")]
    Wave,
    [EnumMember(Value = "highfive")]
    Highfive,
    [EnumMember(Value = "handhold")]
    Handhold,
    [EnumMember(Value = "nom")]
    Nom,
    [EnumMember(Value = "bite")]
    Bite,
    [EnumMember(Value = "glomp")]
    Glomp,
    [EnumMember(Value = "slap")]
    Slap,
    [EnumMember(Value = "kill")]
    Kill,
    [EnumMember(Value = "kick")]
    Kick,
    [EnumMember(Value = "happy")]
    Happy,
    [EnumMember(Value = "wink")]
    Wink,
    [EnumMember(Value = "poke")]
    Poke,
    [EnumMember(Value = "dance")]
    Dance,
    [EnumMember(Value = "cringe")]
    Cringe
}