// Global using statements for ClubDoorman.Test project
// This file eliminates the need to add these using statements in every test file

global using Moq;
global using FluentAssertions;
global using System.Threading;
global using System.Threading.Tasks;
global using Telegram.Bot.Types;
global using Telegram.Bot.Types.Enums;
global using Telegram.Bot.Types.ReplyMarkups;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Logging.Abstractions;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Options;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using ClubDoorman.Test.TestData;
global using ClubDoorman.Test.TestKit;
global using ClubDoorman.Test.TestInfrastructure;
global using static ClubDoorman.Test.TestKit.TestKit;
global using TK = ClubDoorman.Test.TestKit.TestKit;
global using ClubDoorman.Services.Handlers;
global using ClubDoorman.Features.UserJoin;
global using ClubDoorman.Features.Moderation;
global using ClubDoorman.Features.AdminOps;
// SpecFlow & NUnit attributes used in step definition classes
global using TechTalk.SpecFlow;
global using NUnit.Framework;
// Test infrastructure commonly used across step definitions
global using ClubDoorman.TestInfrastructure;
global using ClubDoorman.Services.UserManagement;
global using ClubDoorman.Services.AI;
global using ClubDoorman.Services.Captcha;
global using ClubDoorman.Models;
global using ClubDoorman.Services; // SimpleFilters
global using ClubDoorman.Services.TextProcessing; // TextProcessor
global using ClubDoorman.Services.Telegram; // TelegramBotClientWrapper
global using Telegram.Bot; // TelegramBotClient
global using ClubDoorman.Models.Requests; // CreateCaptchaRequest
global using ClubDoorman.Services.Violation; // IViolationTracker
global using ClubDoorman.Services.UserBan; // IUserBanService
global using ClubDoorman.Services.Messaging; // IMessageService