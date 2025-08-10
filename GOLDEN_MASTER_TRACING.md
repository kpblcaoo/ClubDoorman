# Golden Master & Event Tracing Documentation

## Overview

ClubDoorman now includes Golden Master recording and event tracing capabilities to provide behavior safety during code changes and enable detailed debugging of message processing flows.

## Configuration

Add the following configuration section to your `appsettings.json`:

```json
{
  "LoggingFlags": {
    "TraceEnabled": true,
    "GoldenMasterEnabled": true,
    "GoldenSampleRate": 0.1
  }
}
```

### Configuration Options

- **TraceEnabled**: Enable/disable event tracing (default: false)
- **GoldenMasterEnabled**: Enable/disable Golden Master recording (default: false)
- **GoldenSampleRate**: Sampling rate for Golden Master recording (0.0-1.0, default: 0.1)

## Golden Master

### What is Golden Master?

Golden Master records input and output data from the main message handler to create a baseline of expected behavior. This helps detect regressions when making code changes.

### How it Works

1. Records input (Telegram updates) and output (processing results) for `MessageHandler.HandleAsync()`
2. Data is canonicalized to remove temporal data (timestamps, GUIDs) and mask PII
3. Files are saved to `golden/<YYYY-MM-DD>/<handler>/<msgId>.json`
4. Sampling controls how frequently recordings are made (e.g., 0.1 = 10% of messages)

### File Structure

```
golden/
├── 2024-08-10/
│   └── MessageHandler/
│       ├── 123.json    # Message ID 123
│       ├── 456.json    # Message ID 456
│       └── ...
└── 2024-08-11/
    └── MessageHandler/
        └── ...
```

### Data Canonicalization

- **Timestamps & GUIDs**: Removed
- **Collections**: Sorted alphabetically
- **Numbers**: Rounded to 3 decimal places
- **PII Masking**:
  - Phone numbers → `***PHONE***`
  - Usernames starting with @ → `***USERNAME***`
  - Long tokens → `***TOKEN***`

### Usage

1. Enable Golden Master recording in production/staging
2. Collect baseline data for a representative period
3. Before deploying changes, run the same scenarios and compare outputs
4. Use the integration test pattern to verify behavior matches expectations

## Event Tracing

### What is Event Tracing?

Event tracing provides minimal, structured logging of key decision points in the message processing pipeline.

### Trace Points

The system logs the following events:
- `MessageHandler->Entry`: Message received
- `Routed->Command`: Routed to command handler
- `Routed->NewMembers`: Routed to new member handler
- `Moderation->Start`: Starting moderation analysis
- `Moderation->Done`: Moderation completed (with action and confidence)
- `AI->Start`: Starting AI profile analysis
- `AI->Done`: AI analysis completed
- `Decision->Kept`: Message allowed
- `Decision->Deleted`: Message deleted
- `Decision->Banned`: User banned
- `Decision->Reported`: Message reported to admins
- `Decision->ManualReview`: Requires manual review
- `Decision->AiAnalysis`: Requires AI analysis
- `MessageHandler->Success`: Processing completed successfully
- `MessageHandler->Error`: Processing failed with error

### Log Format

Traces are written to `logs/trace-.json` in compact JSON format:

```json
{"@t":"2024-08-10T10:30:00.000Z","@l":"Debug","@m":"TRACE: MessageHandler->Entry","msgId":123,"chatId":-1001234567890,"requestId":"abc12345"}
{"@t":"2024-08-10T10:30:00.100Z","@l":"Debug","@m":"TRACE: Moderation->Start","msgId":123,"chatId":-1001234567890,"requestId":"abc12345"}
{"@t":"2024-08-10T10:30:00.200Z","@l":"Debug","@m":"TRACE: Moderation->Done {\"action\":\"Allow\",\"confidence\":0.95}","msgId":123,"chatId":-1001234567890,"requestId":"abc12345"}
```

### Correlation

All log entries include correlation fields:
- `msgId`: Telegram message ID
- `chatId`: Telegram chat ID  
- `requestId`: Unique request identifier for this processing session

## Cleaning Up Data

### Golden Master Files

```bash
# Remove old Golden Master files (older than 7 days)
find golden/ -name "*.json" -mtime +7 -delete

# Remove empty directories
find golden/ -type d -empty -delete
```

### Trace Logs

Trace logs follow the same retention policy as other log files (configured in `appsettings.json`).

## Integration Testing

The system includes integration tests that verify Golden Master functionality:

```bash
# Run Golden Master tests
dotnet test --filter "Category=golden-master"
```

See `ClubDoorman.Test/Integration/GoldenMasterIntegrationTests.cs` for examples of how to:
- Verify Golden Master files are created
- Test data canonicalization
- Validate trace logging

## Performance Considerations

- **Golden Master**: Minimal impact when disabled; small I/O overhead when enabled
- **Tracing**: Minimal impact when disabled; small logging overhead when enabled
- **Sampling**: Use `GoldenSampleRate` to reduce storage requirements in high-volume environments
- **Log levels**: Traces require `Debug` level to be enabled for the logger

## Security

- PII (personally identifiable information) is automatically masked in Golden Master files
- No secrets or sensitive data should appear in trace logs
- Golden Master files may contain message content and should be treated as confidential