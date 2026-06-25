# Graph Report - ClubDoorman  (2026-06-24)

## Corpus Check
- 222 files · ~89,982 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1291 nodes · 3185 edges · 110 communities (61 shown, 49 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 5 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- [[_COMMUNITY_User|User]]
- [[_COMMUNITY_Task|Task]]
- [[_COMMUNITY_Callback Query Handler|Callback Query Handler]]
- [[_COMMUNITY_Notification Data|Notification Data]]
- [[_COMMUNITY_I Statistics Service|I Statistics Service]]
- [[_COMMUNITY_Ai Checks|Ai Checks]]
- [[_COMMUNITY_Spam Ham Classifier|Spam Ham Classifier]]
- [[_COMMUNITY_Moderation Result|Moderation Result]]
- [[_COMMUNITY_List|List]]
- [[_COMMUNITY_Golden Master Recorder|Golden Master Recorder]]
- [[_COMMUNITY_I Telegram Bot Client Wrapper|I Telegram Bot Client Wrapper]]
- [[_COMMUNITY_Suspicious Users Storage|Suspicious Users Storage]]
- [[_COMMUNITY_Worker|Worker]]
- [[_COMMUNITY_User Ban Service|User Ban Service]]
- [[_COMMUNITY_Configuration Helper|Configuration Helper]]
- [[_COMMUNITY_Message Context|Message Context]]
- [[_COMMUNITY_I Message Service|I Message Service]]
- [[_COMMUNITY_Message|Message]]
- [[_COMMUNITY_Club Doorman csproj|Club Doorman csproj]]
- [[_COMMUNITY_Global Approval Mode|Global Approval Mode]]
- [[_COMMUNITY_Moderation Effects Builder|Moderation Effects Builder]]
- [[_COMMUNITY_Telegram Bot Client Wrapper|Telegram Bot Client Wrapper]]
- [[_COMMUNITY_Logging Configuration Service|Logging Configuration Service]]
- [[_COMMUNITY_I Logger|I Logger]]
- [[_COMMUNITY_Message Handler|Message Handler]]
- [[_COMMUNITY_Notification Service|Notification Service]]
- [[_COMMUNITY_Moderation Policy|Moderation Policy]]
- [[_COMMUNITY_I Moderation Service|I Moderation Service]]
- [[_COMMUNITY_Bot Permissions Service|Bot Permissions Service]]
- [[_COMMUNITY_Approved Users Storage|Approved Users Storage]]
- [[_COMMUNITY_I Spam Ham Classifier|I Spam Ham Classifier]]
- [[_COMMUNITY_I Command Router|I Command Router]]
- [[_COMMUNITY_I App Config|I App Config]]
- [[_COMMUNITY_User Manager|User Manager]]
- [[_COMMUNITY_I Message Step|I Message Step]]
- [[_COMMUNITY_Chat Member Handler|Chat Member Handler]]
- [[_COMMUNITY_Buttons Service|Buttons Service]]
- [[_COMMUNITY_User Join Facade|User Join Facade]]
- [[_COMMUNITY_Stats Command Handler|Stats Command Handler]]
- [[_COMMUNITY_I Moderation Event Publisher|I Moderation Event Publisher]]
- [[_COMMUNITY_Moderation Service Adapter|Moderation Service Adapter]]
- [[_COMMUNITY_Club Doorman|Club Doorman]]
- [[_COMMUNITY_I User Cleanup Service|I User Cleanup Service]]
- [[_COMMUNITY_Channel Moderation Service|Channel Moderation Service]]
- [[_COMMUNITY_Text Processor|Text Processor]]
- [[_COMMUNITY_Intro Flow Service|Intro Flow Service]]
- [[_COMMUNITY_Send User Notification With Reply|Send User Notification With Reply]]
- [[_COMMUNITY_Dictionary|Dictionary]]
- [[_COMMUNITY_Message Templates|Message Templates]]
- [[_COMMUNITY_I User Index|I User Index]]
- [[_COMMUNITY_Open Ai Extensions|Open Ai Extensions]]
- [[_COMMUNITY_Chat Settings Manager|Chat Settings Manager]]
- [[_COMMUNITY_Club Doorman Exception|Club Doorman Exception]]
- [[_COMMUNITY_I Golden Master Recorder|I Golden Master Recorder]]
- [[_COMMUNITY_Start Command Handler|Start Command Handler]]
- [[_COMMUNITY_Suspicious Command Handler|Suspicious Command Handler]]
- [[_COMMUNITY_Utils|Utils]]
- [[_COMMUNITY_Forwarding Service|Forwarding Service]]
- [[_COMMUNITY_Say Command Handler|Say Command Handler]]
- [[_COMMUNITY_Channel Moderation Effects Builder|Channel Moderation Effects Builder]]
- [[_COMMUNITY_Approval Stats|Approval Stats]]
- [[_COMMUNITY_I Service Collection|I Service Collection]]
- [[_COMMUNITY_Null Moderation Event Publisher|Null Moderation Event Publisher]]
- [[_COMMUNITY_Logging Configuration cs|Logging Configuration cs]]
- [[_COMMUNITY_Log Chat|Log Chat]]
- [[_COMMUNITY_Anti spam Filters|Anti spam Filters]]
- [[_COMMUNITY_Captcha Result|Captcha Result]]
- [[_COMMUNITY_Program|Program]]
- [[_COMMUNITY_Admin Ops Feature|Admin Ops Feature]]
- [[_COMMUNITY_Commands Module|Commands Module]]
- [[_COMMUNITY_A I Module|A I Module]]
- [[_COMMUNITY_Bad Message Module|Bad Message Module]]
- [[_COMMUNITY_Captcha Module|Captcha Module]]
- [[_COMMUNITY_Channel Moderation Module|Channel Moderation Module]]
- [[_COMMUNITY_Configuration Module|Configuration Module]]
- [[_COMMUNITY_Dispatcher Module|Dispatcher Module]]
- [[_COMMUNITY_Handlers Module|Handlers Module]]
- [[_COMMUNITY_Link Formatting Module|Link Formatting Module]]
- [[_COMMUNITY_Messaging Module|Messaging Module]]
- [[_COMMUNITY_Moderation Feature|Moderation Feature]]
- [[_COMMUNITY_Moderation Module|Moderation Module]]
- [[_COMMUNITY_Statistics Module|Statistics Module]]
- [[_COMMUNITY_Suspicious Users Module|Suspicious Users Module]]
- [[_COMMUNITY_Telegram Module|Telegram Module]]
- [[_COMMUNITY_Text Processing Module|Text Processing Module]]
- [[_COMMUNITY_User Ban Module|User Ban Module]]
- [[_COMMUNITY_User Flow Module|User Flow Module]]
- [[_COMMUNITY_User Join Feature|User Join Feature]]
- [[_COMMUNITY_User Join Module|User Join Module]]
- [[_COMMUNITY_User Management Module|User Management Module]]
- [[_COMMUNITY_Violation Module|Violation Module]]
- [[_COMMUNITY_Ai Profile Analysis Step|Ai Profile Analysis Step]]
- [[_COMMUNITY_Banlist Check Step|Banlist Check Step]]
- [[_COMMUNITY_First Message Log Step|First Message Log Step]]
- [[_COMMUNITY_Left Member Cleanup Step|Left Member Cleanup Step]]
- [[_COMMUNITY_New Members Step|New Members Step]]
- [[_COMMUNITY_04 Services cs|04 Services cs]]

## God Nodes (most connected - your core abstractions)
1. `User` - 135 edges
2. `ITelegramBotClientWrapper` - 60 edges
3. `UserBanService` - 38 edges
4. `Worker` - 38 edges
5. `IAppConfig` - 37 edges
6. `ModerationPolicy` - 35 edges
7. `NotificationData` - 34 edges
8. `IMessageService` - 33 edges
9. `IUserFlowLogger` - 31 edges
10. `CallbackQueryHandler` - 30 edges

## Surprising Connections (you probably didn't know these)
- `approved-users.txt Approved Users List` --shares_data_with--> `approved_users.json`  [AMBIGUOUS]
  data/approved-users.txt → APPROVAL_SYSTEM.md
- `RequireAiAnalysisEffect` --references--> `IAiCascadeService`  [EXTRACTED]
  Effects/AiAnalysis/RequireAiAnalysisEffect.cs → Services/AI/IAiCascadeService.cs
- `RequireAiAnalysisEffect` --references--> `User`  [EXTRACTED]
  Effects/AiAnalysis/RequireAiAnalysisEffect.cs → Services/UserManagement/UserManager.cs
- `AllowMessageEffect` --references--> `IModerationPolicy`  [EXTRACTED]
  Effects/Allow/AllowMessageEffect.cs → Features/Moderation/05-Contracts.cs
- `AllowMessageEffect` --references--> `User`  [EXTRACTED]
  Effects/Allow/AllowMessageEffect.cs → Services/UserManagement/UserManager.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Approval Modes and Data Files** — clubdoorman_approval_system_global_approval_mode, clubdoorman_approval_system_group_approval_mode, clubdoorman_approval_system_approved_users_json, clubdoorman_approval_system_approved_users_groups_json, clubdoorman_approval_system_doorman_group_approval_mode [EXTRACTED 1.00]
- **Media Filtering Policy Components** — clubdoorman_media_filtering_doorman_disable_media_filtering, clubdoorman_media_filtering_doorman_media_filtering_disabled_chats, clubdoorman_media_filtering_photo_video_filtering, clubdoorman_media_filtering_sticker_document_blocking, clubdoorman_media_filtering_caption_stop_word_checking [EXTRACTED 1.00]
- **Moderation Data Inputs** — data_stop_words_stop_words_list, data_spam_ham_spam_ham_dataset, data_bad_messages_bad_message_hashes, clubdoorman_media_filtering_anti_spam_filters [INFERRED 0.85]

## Communities (110 total, 49 thin omitted)

### Community 0 - "User"
Cohesion: 0.08
Nodes (5): Chat, Exception, IUserFlowLogger, UserFlowLogger, User

### Community 1 - "Task"
Cohesion: 0.07
Nodes (5): CancellationToken, IChannelModerationService, ChatMemberUpdated, Stream, Task

### Community 2 - "Callback Query Handler"
Cohesion: 0.07
Nodes (10): CallbackQuery, CaptchaService, ICaptchaService, CaptchaInfo, CallbackQueryHandler, ICallbackQueryHandler, CreateCaptchaRequest, IViolationTracker (+2 more)

### Community 3 - "Notification Data"
Cohesion: 0.13
Nodes (17): InlineKeyboardMarkup, IServiceChatDispatcher, ServiceChatDispatcher, AiDetectNotificationData, AiProfileAnalysisData, AutoBanNotificationData, CaptchaWelcomeNotificationData, ChannelMessageNotificationData (+9 more)

### Community 4 - "I Statistics Service"
Cohesion: 0.06
Nodes (9): ConcurrentDictionary, IDictionary, ChatLinkFormatter, IChatLinkFormatter, ChatStats, IStatisticsService, StatisticsService, IJoinedUserFlags (+1 more)

### Community 5 - "Ai Checks"
Cohesion: 0.09
Nodes (11): AiChecks, SpamProbability, IAiChecks, Func, JsonSerializerOptions, OpenAiClient, ResiliencePipeline, SpamPhotoBio (+3 more)

### Community 6 - "Spam Ham Classifier"
Cohesion: 0.07
Nodes (16): MessageData, MessagePrediction, SpamHamClassifier, BadMessageManager, ByteArrayComparer, IBadMessageManager, IComparer, IDisposable (+8 more)

### Community 7 - "Moderation Result"
Cohesion: 0.08
Nodes (4): ModerationFacade, IModerationFacade, IModerationPolicy, ModerationResult

### Community 8 - "List"
Cohesion: 0.10
Nodes (7): IMimicryClassifier, MimicryClassifier, Captcha, ClubDoorman.Infrastructure, List, SimpleFilters, UserId

### Community 9 - "Golden Master Recorder"
Cohesion: 0.07
Nodes (12): action, AppConfig, double, Consts, int, IOptions, JToken, GoldenMasterRecorder (+4 more)

### Community 10 - "I Telegram Bot Client Wrapper"
Cohesion: 0.14
Nodes (7): ChatId, ChatPermissions, DateTime, ParseMode, ReplyMarkup, ReplyParameters, ITelegramBotClientWrapper

### Community 11 - "Suspicious Users Storage"
Cohesion: 0.11
Nodes (7): GroupsCount, object, SuspiciousUserInfo, ISuspiciousUsersStorage, SuspiciousUsersStorage, TotalSuspicious, WithAiDetect

### Community 12 - "Worker"
Cohesion: 0.09
Nodes (8): BackgroundService, UpdateDispatcher, PeriodicTimer, IUpdateDispatcher, ChatStat, GlobalStatsManager, StatsRoot, Worker

### Community 13 - "User Ban Service"
Cohesion: 0.15
Nodes (4): BanTypeEnum, duration, reason, UserBanService

### Community 14 - "Configuration Helper"
Cohesion: 0.09
Nodes (9): AiOptions, AutoBanOptions, ChatAccessOptions, ChatFilteringOptions, ConfigurationHelper, CoreOptions, FeatureToggleOptions, TestHarnessOptions (+1 more)

### Community 15 - "Message Context"
Cohesion: 0.11
Nodes (8): IReadOnlyList, IMessagePipeline, MessageContext, MessagePipeline, Continue(), Fail(), StopOk(), StepResult

### Community 16 - "I Message Service"
Cohesion: 0.11
Nodes (6): AdminNotificationType, LogNotificationType, IMessageService, MessageService, SendCaptchaMessageRequest, SendErrorNotificationRequest

### Community 17 - "Message"
Cohesion: 0.14
Nodes (8): AllowMessageEffect, DeleteToLogEffect, DeleteWithReportEffect, TrackViolationEffect, Message, INotificationService, string, IUserBanService

### Community 18 - "Club Doorman csproj"
Cohesion: 0.10
Nodes (20): net9.0, CsvHelper (33.0.1), DotNetEnv (2.6.0), Microsoft.Extensions.Caching.Hybrid (8.0.0), Microsoft.Extensions.Hosting (8.0.0), Microsoft.ML (4.0.2), Microsoft.VisualStudio.Azure.Containers.Tools.Targets (1.21.0), Polly (8.5.2) (+12 more)

### Community 19 - "Global Approval Mode"
Cohesion: 0.13
Nodes (21): Admin Approval Command, Approval Check Logic, approved_users_groups.json, approved_users.json, Autoapproval, DOORMAN_GROUP_APPROVAL_MODE, Global Approval Mode, Group Approval Mode (+13 more)

### Community 20 - "Moderation Effects Builder"
Cohesion: 0.11
Nodes (8): EffectsMonitoringService, HybridModerationEffectsBuilder, EffectsConfiguration, IServiceProvider, HybridModerationEffectsBuilder, IModerationEffectsBuilder, ModerationEffectsBuilder, ModerationAction

### Community 21 - "Telegram Bot Client Wrapper"
Cohesion: 0.13
Nodes (4): ChatFullInfo, DeleteMessageResult, TelegramBotClientWrapper, TelegramBotClient

### Community 22 - "Logging Configuration Service"
Cohesion: 0.15
Nodes (4): LoggingConfiguration, ILoggingConfigurationService, LoggingConfigurationService, NotificationDestination

### Community 23 - "I Logger"
Cohesion: 0.19
Nodes (13): RequireAiAnalysisEffect, BanUserEffect, bool, ChannelAllowEffect, ChannelBanEffect, ChannelDeleteMessageEffect, ChannelReportMessageEffect, ChannelUnknownActionEffect (+5 more)

### Community 24 - "Message Handler"
Cohesion: 0.12
Nodes (4): MessageHandler, IEnumerable, Update, UpdateType

### Community 25 - "Notification Service"
Cohesion: 0.16
Nodes (3): ILogChatService, LogChatService, NotificationService

### Community 27 - "I Moderation Service"
Cohesion: 0.12
Nodes (3): AiCascadeService, IAiCascadeService, IModerationService

### Community 28 - "Bot Permissions Service"
Cohesion: 0.16
Nodes (4): ChatMember, BotPermissionsService, MemoryCache, IBotPermissionsService

### Community 30 - "I Spam Ham Classifier"
Cohesion: 0.18
Nodes (3): CheckCommandHandler, SpamCommandHandler, ISpamHamClassifier

### Community 31 - "I Command Router"
Cohesion: 0.17
Nodes (6): AdminOpsFacade, IAdminOpsFacade, CommandProcessingService, CommandRouter, ICommandProcessingService, ICommandRouter

### Community 32 - "I App Config"
Cohesion: 0.18
Nodes (3): HamCommandHandler, IAppConfig, FolderInviteService

### Community 33 - "User Manager"
Cohesion: 0.18
Nodes (5): HttpClient, ClubByTgIdResponse, Data, Error, UserManager

### Community 34 - "I Message Step"
Cohesion: 0.17
Nodes (6): IMessageStep, AlreadyApprovedStep, BaseModerationStep, ChannelMessageStep, CommandStep, FinalModerationActionStep

### Community 35 - "Chat Member Handler"
Cohesion: 0.18
Nodes (4): ChatMemberHandler, IUpdateHandler, LoggingFlagsOptions, IFolderInviteService

### Community 37 - "User Join Facade"
Cohesion: 0.20
Nodes (3): UserJoinFacade, IUserJoinFacade, IUserJoinPolicy

### Community 38 - "Stats Command Handler"
Cohesion: 0.24
Nodes (4): ICommandHandler, ISuspiciousCommandHandler, StatsAliasCommandHandler, StatsCommandHandler

### Community 39 - "I Moderation Event Publisher"
Cohesion: 0.20
Nodes (5): IModerationEventPublisher, CaptchaPendingStep, ClubMemberSkipStep, PrivateSkipStep, SystemOrBotMessageStep

### Community 41 - "Club Doorman"
Cohesion: 0.20
Nodes (9): commandName, dotnetRunMessages, environmentVariables, commandName, DOTNET_ENVIRONMENT, profiles, ClubDoorman, Container (Dockerfile) (+1 more)

### Community 43 - "Channel Moderation Service"
Cohesion: 0.33
Nodes (3): ChannelModerationService, EffectBus, IEffectBus

### Community 44 - "Text Processor"
Cohesion: 0.36
Nodes (3): GeneratedRegex, Regex, TextProcessor

### Community 47 - "Dictionary"
Cohesion: 0.32
Nodes (4): Dictionary, GroupApprovalInfo, groupApprovals, isGlobal

### Community 50 - "Open Ai Extensions"
Cohesion: 0.29
Nodes (4): ChatCompletionRequestMessageContentPartImageImageUrlDetail, ChatCompletionRequestSystemMessage, ChatCompletionRequestUserMessage, OpenAiExtensions

### Community 52 - "Club Doorman Exception"
Cohesion: 0.52
Nodes (6): AiServiceException, ClubDoormanException, ConfigurationException, ModerationException, TelegramApiException, UserManagementException

### Community 60 - "Approval Stats"
Cohesion: 0.60
Nodes (3): globalCount, groupCount, totalGroupApprovals

### Community 61 - "I Service Collection"
Cohesion: 0.40
Nodes (3): IConfiguration, ServiceCollectionExtensions, IServiceCollection

### Community 63 - "Logging Configuration cs"
Cohesion: 0.40
Nodes (4): CategoryLoggingSettings, FileLoggingSettings, NotificationTypeSettings, TelegramNotificationSettings

### Community 64 - "Log Chat"
Cohesion: 0.67
Nodes (4): DOORMAN_ADMIN_CHAT, DOORMAN_LOG_ADMIN_CHAT, Log Chat, Telegram Update Offset

### Community 65 - "Anti spam Filters"
Cohesion: 0.50
Nodes (4): Anti-spam Filters, Bad Message Hashes, Spam Ham Dataset, Text Label Schema

## Ambiguous Edges - Review These
- `approved_users.json` → `approved-users.txt Approved Users List`  [AMBIGUOUS]
  data/approved-users.txt · relation: shares_data_with

## Knowledge Gaps
- **43 isolated node(s):** `net9.0`, `CsvHelper (33.0.1)`, `Microsoft.Extensions.Hosting (8.0.0)`, `Microsoft.ML (4.0.2)`, `Microsoft.VisualStudio.Azure.Containers.Tools.Targets (1.21.0)` (+38 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **49 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `approved_users.json` and `approved-users.txt Approved Users List`?**
  _Edge tagged AMBIGUOUS (relation: shares_data_with) - confidence is low._
- **Why does `User` connect `User` to `Task`, `Callback Query Handler`, `Notification Data`, `Ai Checks`, `Moderation Result`, `Worker`, `User Ban Service`, `Message`, `Telegram Bot Client Wrapper`, `I Logger`, `Notification Service`, `Moderation Policy`, `I Moderation Service`, `User Manager`, `Buttons Service`, `User Join Facade`, `Moderation Service Adapter`, `Intro Flow Service`, `Send User Notification With Reply`, `Utils`?**
  _High betweenness centrality (0.065) - this node is a cross-community bridge._
- **Why does `IAppConfig` connect `I App Config` to `Callback Query Handler`, `Notification Data`, `Ai Checks`, `Golden Master Recorder`, `Worker`, `User Ban Service`, `I Message Service`, `Message Handler`, `Notification Service`, `Moderation Policy`, `I Moderation Service`, `Bot Permissions Service`, `I Spam Ham Classifier`, `User Manager`, `Chat Member Handler`, `Buttons Service`, `Stats Command Handler`, `I Moderation Event Publisher`, `Channel Moderation Service`, `Intro Flow Service`, `Message Templates`, `Start Command Handler`, `Suspicious Command Handler`, `Say Command Handler`, `New Members Step`?**
  _High betweenness centrality (0.053) - this node is a cross-community bridge._
- **Why does `ModerationPolicy` connect `Moderation Policy` to `I App Config`, `I Statistics Service`, `Ai Checks`, `Spam Ham Classifier`, `Moderation Result`, `List`, `I User Cleanup Service`, `Suspicious Users Storage`, `I Message Service`, `Message`, `I Logger`, `I Spam Ham Classifier`?**
  _High betweenness centrality (0.049) - this node is a cross-community bridge._
- **What connects `net9.0`, `CsvHelper (33.0.1)`, `Microsoft.Extensions.Hosting (8.0.0)` to the rest of the system?**
  _44 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `User` be split into smaller, more focused modules?**
  _Cohesion score 0.0798611111111111 - nodes in this community are weakly interconnected._
- **Should `Task` be split into smaller, more focused modules?**
  _Cohesion score 0.0701344243132671 - nodes in this community are weakly interconnected._