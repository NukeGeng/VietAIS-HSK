using VietAisHsk.Api.Modules.Identity;

namespace VietAisHsk.Api.Tests;

public sealed class IdentityValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("00:00")]
    [InlineData("20:30")]
    [InlineData("23:59")]
    public void Study_time_accepts_optional_24_hour_time(string? value)
    {
        Assert.Null(IdentityValidation.ValidateProfile(new(null, null, null, new(20, value))));
    }

    [Theory]
    [InlineData("")]
    [InlineData("24:00")]
    [InlineData("20:60")]
    [InlineData("8:00")]
    [InlineData("20:00:00")]
    [InlineData("tomorrow")]
    [InlineData(" 20:00 ")]
    public void Study_time_rejects_malformed_values(string value)
    {
        Assert.NotNull(IdentityValidation.ValidateProfile(new(null, null, null, new(20, value))));
    }

    [Fact]
    public void ValidateLearningTarget_rejects_unknown_hsk_reference()
    {
        var result = IdentityValidation.ValidateLearningTarget(new SetLearningTargetRequest("HSK99", "HSK3"));

        Assert.Equal("PreferredHskLevelId không tồn tại trong danh sách HSK hiện có.", result);
    }

    [Fact]
    public void ValidateProfile_rejects_unreasonable_daily_minutes()
    {
        var result = IdentityValidation.ValidateProfile(new UpdateLearnerProfileRequest(
            "Học viên",
            null,
            null,
            new StudyPreferences(241)));

        Assert.Equal("DailyMinutes phải nằm trong khoảng 5 đến 240 phút.", result);
    }
}
