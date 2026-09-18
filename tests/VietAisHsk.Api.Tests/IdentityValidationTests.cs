using VietAisHsk.Api.Modules.Identity;

namespace VietAisHsk.Api.Tests;

public sealed class IdentityValidationTests
{
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
