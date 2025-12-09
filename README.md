# MemosToBarkOrTelegram

A simple ASP.NET Core Web API that forwards webhook events from [usememos](https://github.com/usememos/memos) to [Bark](https://github.com/Finb/Bark) or Telegram.

## Features

- Based usememos 0.25.3
- Forwards to both Bark and Telegram
- Delivery to multiple devices / chats
- Do not deliver a user's own messages back to themselves
- Customizable message templates

## Quick Start

### 1. Configuration

Edit `appsettings.json`:

```json
{
  "Memos": {
    "BaseUrl": "https://your.usememos.url",
    "UserNameMap": {
      "1": "User1",
      "2": "User2"
    },
    "MessageTemplates": {
      "memos.memo.created": {
        "title": "{creator} posted a new memo",
        "body": "{content}"
      },
      "memos.memo.updated": {
        "title": "{creator} updated a memo",
        "body": "{content}"
      }
    }
  },
  "Bark": {
    "Enabled": true,
    "ServerUrl": "https://api.day.app",
    "DeviceKeys": {
      "*": [ "global-device-key" ],
      "1": [ "user1-device-key-1", "user1-device-key-2" ],
      "2": [ "user2-device-key" ]
    },
    "Icon": "https://usememos.com/logo.png",
    "Group": "Memos",
    "Sound": "newmail",
    "Auth": {
      "Username": "your-username",
      "Password": "your-password"
    }
  },
  "Telegram": {
    "Enabled": true,
    "BotToken": "your-bot-token",
    "ChatIds": {
      "*": [ "-1001234567890" ],
      "1": [ "123456789" ],
      "2": [ "987654321" ]
    },
    "ParseMode": "HTML"
  }
}
```

### 2. Run the project

```bash
dotnet run
```

### 3. Configure the Memos webhook

Set the webhook URL in Memos to:

```
http://your-server:5237/api/webhook/memos
```

## Configuration details

### Memos

| Field | Description | Example |
|-------|-------------|---------|
| `BaseUrl` | Base URL of your Memos instance | `http://127.0.0.1:5230` |
| `UserNameMap` | Map from Memos user ID to display name | `{ "1": "Admin" }` |
| `MessageTemplates` | Optional message templates | See below |

#### Template placeholders

- `{creator}` — display name
- `{content}` — memo content
- `{createTime}` — creation time
- `{updateTime}` — update time
- `{visibility}` — visibility (PRIVATE/PROTECTED/PUBLIC)

### Bark

| Field | Description | Required |
|-------|-------------|----------|
| `Enabled` | Enable Bark notifications | Yes |
| `ServerUrl` | Bark server base URL | Yes |
| `DeviceKeys` | Mapping from user ID to device keys | Yes |
| `Icon` | Notification icon URL | No |
| `Group` | Notification group | No |
| `Sound` | Notification sound | No |
| `Auth` | HTTP Basic Auth credentials (optional) | No |

#### `DeviceKeys` format

`DeviceKeys` is a dictionary mapping user IDs to arrays of device keys:

```json
{
  "DeviceKeys": {
    "*": [ "global-device-1", "global-device-2" ],
    "1": [ "user1-device-1", "user1-device-2" ],
    "2": [ "user2-device" ]
  }
}
```

- `"*"` denotes global devices that receive notifications for all users
- `"1"`, `"2"` etc. denote devices associated with specific Memos user IDs

**Filtering rule:** when a memo is created by user `X`, devices listed under key `X` will be excluded so that users do not receive their own notifications. Devices under `"*"` and other users' devices will receive the notification.

#### HTTP Auth

If your Bark server requires HTTP Basic Authentication, configure credentials:

```json
{
  "Bark": {
    "Auth": {
      "Username": "your-username",
      "Password": "your-password"
    }
  }
}
```

If no authentication is required, set `Auth` to `null` or omit it.

### Telegram

| Field | Description | Required |
|-------|-------------|----------|
| `Enabled` | Enable Telegram notifications | Yes |
| `BotToken` | Telegram Bot token | Yes |
| `ChatIds` | Mapping from user ID to chat IDs | Yes |
| `ParseMode` | Message parse mode (e.g. HTML) | No (default: HTML) |

#### `ChatIds` format

`ChatIds` is a dictionary mapping user IDs to arrays of chat IDs:

```json
{
  "ChatIds": {
    "*": [ "-1001234567890" ],
    "1": [ "123456789", "111111111" ],
    "2": [ "987654321" ]
  }
}
```

- `"*"` denotes global chats (groups or channels) that receive notifications for all users
- Specific user keys denote chats associated with those users

**Filtering rule:** when a memo is created by user `X`, chat IDs under key `X` will be excluded so that the creator does not receive their own notification.

#### Obtaining ChatId

1. Talk to your bot
2. Call `https://api.telegram.org/bot<YourBotToken>/getUpdates`
3. Find `chat.id` in the returned JSON

Supported chat ID types:
- Private chat: positive integer (e.g. `123456789`)
- Group: negative integer (e.g. `-987654321`)
- Channel: negative integer (e.g. `-1001234567890`)

## Usage examples

### Example 1 — shared group

```json
{
  "Memos": { "UserNameMap": { "1": "Alice", "2": "Bob" } },
  "Telegram": { "ChatIds": { "*": [ "-1001234567890" ] } }
}
```

- Alice (ID=1) posts → group receives notification
- Bob (ID=2) posts → group receives notification

### Example 2 — personal DMs

```json
{
  "Telegram": {
    "ChatIds": {
      "*": [],
      "1": [ "123456789" ],
      "2": [ "987654321" ]
    }
  }
}
```

- Alice posts → message sent to Bob only
- Bob posts → message sent to Alice only

### Example 3 — mixed

```json
{
  "Bark": {
    "DeviceKeys": {
      "*": [ "family-device" ],
      "1": [ "alice-phone", "alice-ipad" ],
      "2": [ "bob-phone" ]
    }
  }
}
```

- Alice posts → `family-device` and `bob-phone` receive, Alice's devices excluded
- Bob posts → `family-device` and Alice's devices receive, Bob's device excluded

## API Endpoints

### POST /api/webhook/memos

Receives Memos webhook events.

**Request example:**

```json
{
  "url": "https://memos.example.com",
  "activityType": "memo.created",
  "creator": "users/1",
  "memo": {
    "id": 123,
    "name": "memos/123",
    "content": "This is a new memo #important",
    "createTime": { "seconds": 1735459200 },
    "updateTime": { "seconds": 1735459200 },
    "visibility": 0
  }
}
```

**Response example:**

```json
{
  "success": true
}
```

### GET /api/webhook/health

Health check.

**Response example:**

```json
{
  "status": "g2g",
  "timestamp": "2024-01-01T00:00:00Z"
}
```

## Logging

The app uses the built-in ASP.NET Core logging system and logs:

- Webhook receipts
- Delivery success/failure counts
- Filtered recipient details
- Error details

Log level is configurable via `appsettings.json`.

## Tech stack

- .NET 10
- ASP.NET Core Web API
- System.Text.Json

## License

WTFPL - Do What The F*ck You Want To Public License