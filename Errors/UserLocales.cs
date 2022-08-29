using System.Runtime.Serialization;

namespace Geno.Errors;

public enum UserLocales
{
    [EnumMember(Value = "en-GB")]
    English = 0,
    
    [EnumMember(Value = "da")]
    Danish
}