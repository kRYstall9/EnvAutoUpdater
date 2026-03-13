# EnvAutoUpdater

A lightweight .NET 9 background service that automatically keeps your local `.env` files in sync with their corresponding versions hosted in remote Git repositories (e.g., GitHub).

---

## 📑 Table of Contents

- [Features](#-features)
- [Requirements](#-requirements)
- [Configuration](#-configuration)
  - [Configuration Fields](#configuration-fields)
  - [Service Configuration](#service-configuration)
  - [Full Configuration Example](#full-configuration-example)
- [Deployment](#-deployment)
  - [Docker Compose](#docker-compose)
  - [Volume Mapping](#volume-mapping)
  - [Running Without Docker](#running-without-docker)
- [How It Works](#-how-it-works)
- [Behavior Details](#-behavior-details)
  - [Variable Parsing](#variable-parsing)
  - [Multi-line Values](#multi-line-values)
  - [Added Variables Format](#added-variables-format)
  - [Comment Syncing](#comment-syncing)
  - [Backup Files](#backup-files)
- [Troubleshooting](#-troubleshooting)
- [License](#-license)

---

## 🚀 Features

- 🔄 Automatically detects outdated local `.env` files by comparing variable names with the remote version
- ➕ Adds missing environment variables found in the repository `.env` to the local file
- 💬 Preserves and syncs comments associated with each variable
- 🧩 Supports multi-line variable values (e.g., JSON strings)
- 💾 Optional backup of the original `.env` file before any update (`.env.bak_TIMESTAMP`)
- 🔗 Supports multiple services, each with its own repository URL and local file path
- 🌐 Automatically converts standard GitHub URLs to raw content URLs
- 🐳 Docker-ready with simple volume-based configuration

---

## 📋 Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for local development/run)
- [Docker](https://docs.docker.com/get-docker/) and [Docker Compose](https://docs.docker.com/compose/install/) (for containerized deployment)

---

## ⚙️ Configuration

The service is configured entirely via a `config.json` file located in the application's root directory. This file defines which services to monitor, how often to check for updates, and general behavior settings.

### Configuration Fields

| Field | Type | Description | Default |
|---|---|---|---|
| `checkInterval` | `int` | Interval in **seconds** between each sync check | `3600` |
| `saveBackupFile` | `bool` | If `true`, a `.env.bak_TIMESTAMP` backup is created before any update | `true` |
| `timezone` | `string` | Timezone used for logging timestamps (e.g., `UTC`, `America/New_York`, `Europe/Rome`) | `"UTC"` |
| `services` | `array` | List of service objects to monitor (see below) | `[]` |

### Service Configuration

Each entry in the `services` array represents a single `.env` file to keep in sync:

| Field | Type | Required | Description |
|---|---|---|---|
| `name` | `string` | ✅ | A friendly name to identify the service in logs |
| `repositoryEnvUrl` | `string` | ✅ | The URL pointing to the remote `.env` file in the Git repository |
| `localEnvPath` | `string` | ✅ | The **absolute path** to the local `.env` file to keep in sync |

> [!NOTE]
> Both standard GitHub URLs (`https://github.com/user/repo/blob/main/.env`) and raw URLs (`https://raw.githubusercontent.com/user/repo/refs/heads/main/.env`) are supported. Standard URLs are **automatically converted** to raw format at runtime.

### Full Configuration Example

Below is a complete `config.json` example that monitors three different services:

```
{ 
	"services": 
		[ 
			{ 
			
				"name": "AuthService", 
				"repositoryEnvUrl": "https://raw.githubusercontent.com/myorg/auth-service/refs/heads/main/.env-sample", 
				"localEnvPath": "/app/envs/auth-service/.env" 
				
			}, 
			{ 
				"name": "PaymentGateway", 
				"repositoryEnvUrl": "https://github.com/myorg/payment-gateway/blob/main/.env-sample", 
				"localEnvPath": "/app/envs/payment-gateway/.env" 
			}, 
			{ 
				"name": "NotificationService", 
				"repositoryEnvUrl": "https://raw.githubusercontent.com/myorg/notifications/refs/heads/develop/.env-sample", 
				"localEnvPath": "/app/envs/notifications/.env" 
			} 
		], 
	"checkInterval": 1800, 
	"saveBackupFile": true, 
	"timezone": "Europe/Rome" 
}
```

> [!IMPORTANT]
> When running inside Docker, the `localEnvPath` values must reference paths **inside the container**. These paths must be mapped to the actual host directories via Docker volumes. See the [Deployment](#-deployment) section for details.

---

## 🐳 Deployment

### Docker Compose

The recommended way to deploy EnvAutoUpdater is via Docker Compose. Create a `docker-compose.yml` file:

```
services: 
	env-auto-updater: 
	image: envautoupdater:latest 
	container_name: env-auto-updater 
	restart: unless-stopped 
	volumes: 
		# Mount the configuration file (read-only) 
		- ./config.json:/app/config.json:ro
		# Mount the local .env directories for each monitored service.
		# The container needs read/write access to update the .env files.
		- /home/user/projects/auth-service:/app/envs/auth-service
		- /home/user/projects/payment-gateway:/app/envs/payment-gateway
		- /home/user/projects/notifications:/app/envs/notifications
```

Then, start the service:

```
docker-compose up -d
```

This will download the latest image, create the container, and start the EnvAutoUpdater service in detached mode. The logs can be viewed with:

```
docker-compose logs env-auto-updater -f
```

### Volume Mapping

Ensure that the host directories (e.g., `/home/user/projects/auth-service`) exist and contain the `.env` files you want to monitor. The container will map these directories to `/app/envs/...` paths.

> [!CAUTION]
> If a volume is not correctly mapped, the container will not be able to read or write the `.env` file. The service will log an error and skip that service until the next check cycle.

> [!TIP]
> Mount `config.json` as **read-only** (`:ro`) since the service only reads it. Do **not** mount `.env` directories as read-only — the service needs write access to update them.

### Running Without Docker

For development or testing, you can run the service directly on your machine (assuming .NET 9 SDK is installed):

1. **Clone the repository**:
```
git clone https://github.com/kRYstall9/EnvAutoUpdater.git
cd EnvAutoUpdater
```

2. **Configure the service**:
   - Edit `config.json` to your needs.

3. **Run the service**:
```
dotnet run
```

---

## 🔍 How It Works

1. **Read configuration** — loads the list of services to monitor from `config.json` at each iteration (*The config could be changed with no need to restart the container. It's updated content will be read at the next iteration*).
2. **Read local `.env`** — reads the local file line by line via `ReadLocalEnvFile`.
3. **Fetch remote `.env`** — downloads the latest `.env` from the configured repository URL via `ReadUpdatedEnvFileFromRepository`.
4. **Extract variable names** — parses both files via `ReadEnvVariables` to extract environment variable names (lines containing `=`).
5. **Compare** — checks via `IsFileAlreadyUpdated` if all remote variable names are already present in the local file.
6. **Update** — if differences are found, `UpdateEnvFile`:
   - Appends a clearly marked section at the bottom of the local file.
   - Adds any missing variables (commented out, preserving their original values and associated comments).
   - Updates inline comments for variables already present locally.
7. **Backup** *(optional)* — saves a `.env.bak_TIMESTAMP` copy before writing changes.
8. **Wait** — sleeps for `checkInterval` seconds, then repeats from step 1.

---

## 📝 Behavior Details

### Variable Parsing

- Lines that are **empty**, do not contain `=`, or have **spaces in the variable name** are skipped.
- **Commented-out variables** (prefixed with `#`) are recognized and their names are extracted by stripping the `#` prefix.
- Duplicate variable names across the file are automatically **deduplicated**.

### Multi-line Values

Variables whose values span multiple lines (e.g., embedded JSON strings) are fully captured. The parser continues reading lines until it encounters an empty line, a comment, or a new variable assignment.

### Added Variables Format

New variables found in the remote `.env` are appended **commented out** at the bottom of the local file, under a clearly identifiable marker section. This ensures your existing configuration is never overwritten — you review and uncomment the new variables manually.

### Comment Syncing

If a variable already exists locally but its associated comments in the repository have changed, EnvAutoUpdater will:
- Remove the outdated comments above the existing variable line.
- Insert the updated comments from the repository in their place.

### Backup Files

- If `saveBackupFile` is enabled, a backup of the original `.env` file is saved as `.env.bak_TIMESTAMP` before any updates are applied. The timestamp format is `yyyyMMddTHHmmss` (e.g., `.env.bak_20230101T120000`).

---

## ❓ Troubleshooting

| Problem | Possible Cause | Solution |
|---|---|---|
| Service skips a service entry | `localEnvPath` points to a nonexistent file or unmapped volume | Verify the volume mapping in `docker-compose.yml` and ensure the `.env` file exists |
| No updates detected | Local file already contains all remote variables | Check logs for detailed comparison output |
| Permission denied errors | Docker container lacks write access to the mounted volume | Ensure the volume is **not** mounted as read-only (`:ro`) for `.env` directories |
| GitHub rate limiting | Too many requests to raw.githubusercontent.com | Increase `checkInterval` to reduce request frequency |
| Wrong timestamps in logs | Incorrect timezone configuration | Set `timezone` in `config.json` to your local timezone (e.g., `Europe/Rome`) |

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---