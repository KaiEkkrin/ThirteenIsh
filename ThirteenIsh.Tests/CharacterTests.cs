using Shouldly;
using ThirteenIsh.Entities;

namespace ThirteenIsh.Tests;

public class CharacterTests
{
    public static TheoryData<ulong> UserIdData => new()
    {
        0UL, ulong.MaxValue, 0x1234567812345678UL, 0xa0a0a0a0a0a0a0a0UL, 0x8a8a8a8a8a8a8a8aUL, 0xfedcba9876543210UL
    };

    /// <summary>
    /// This is sheer paranoia -- making sure the cast doesn't do any magic conversion
    /// that would break bit-equality between long and ulong and cause some IDs to not
    /// write properly to the database
    /// </summary>
    [Theory]
    [MemberData(nameof(UserIdData))]
    public void UserIdIsConvertedCorrectly(ulong userId)
    {
        Character character = new() { UserId = Character.ToDatabaseUserId(userId) };
        var extractedId = character.NativeUserId;
        extractedId.ShouldBe(userId, $"{userId} converted to {character.UserId}");
    }
}
