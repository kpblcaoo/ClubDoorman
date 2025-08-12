using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ClubDoorman.Services.Captcha;
using ClubDoorman.Models;
using ClubDoorman.Models.Requests;

namespace ClubDoorman.Tests.TestKit2.Fakes;

public sealed class FakeCaptchaService : ICaptchaService
{
    private readonly Queue<CaptchaInfo?> _createResults = new();
    private readonly Queue<bool> _validateResults = new();
    private readonly Queue<Exception> _exceptions = new();
    
    public void EnqueueCreateResult(CaptchaInfo? result) => _createResults.Enqueue(result);
    public void EnqueueValidateResult(bool result) => _validateResults.Enqueue(result);
    public void EnqueueException(Exception exception) => _exceptions.Enqueue(exception);
    
    public async Task<CaptchaInfo?> CreateCaptchaAsync(CreateCaptchaRequest request)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        if (_createResults.Count > 0)
            return _createResults.Dequeue();
            
        return null; // Default: no captcha created
    }
    
    public string GenerateKey(long chatId, long userId)
    {
        return $"fake_captcha_{chatId}_{userId}";
    }
    
    public CaptchaInfo? GetCaptchaInfo(string key)
    {
        return null; // Default: no captcha info
    }
    
    public bool RemoveCaptcha(string key)
    {
        return true; // Default: successfully removed
    }
    
    public async Task<bool> ValidateCaptchaAsync(string key, int answer)
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        if (_validateResults.Count > 0)
            return _validateResults.Dequeue();
            
        return true; // Default: valid
    }
    
    public async Task BanExpiredCaptchaUsersAsync()
    {
        if (_exceptions.Count > 0)
            throw _exceptions.Dequeue();
            
        // Default: do nothing
        await Task.CompletedTask;
    }
    
    public void Reset()
    {
        _createResults.Clear();
        _validateResults.Clear();
        _exceptions.Clear();
    }
}
