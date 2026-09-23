# Speaking Frontend Module

Visible name: `Nói - đối thoại`.

## Screens
- mode/topic selection;
- active conversation;
- session summary/history.

## Active conversation
- chủ đề/HSK;
- transcript/message flow;
- microphone/recording state;
- AI response audio/text;
- end session.

Không hiển thị kỹ thuật `STT/TTS/LLM` cho learner.

## Summary
- Phát âm;
- Độ trôi chảy;
- Ngữ pháp;
- Từ vựng;
- Lỗi cần chú ý;
- nội dung nên luyện.

## Computer Use tests
- [ ] permission microphone — transport microphone chưa được chốt trong backend;
- [x] session/loading/error states;
- [x] transcript conversation render và responsive layout;
- [x] end session;
- [x] provider failure graceful.
