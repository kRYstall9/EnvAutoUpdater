# EnvAutoUpdater

A lightweight .NET 9 background service that automatically keeps your local `.env` files in sync with their corresponding versions hosted in remote Git repositories (e.g., GitHub).

---

## 🚀 Features

- 🔄 Automatically detects outdated local `.env` files by comparing variable names with the remote version
- ➕ Adds missing environment variables found in the repository `.env` to the local file
- 💬 Preserves and syncs comments associated with each variable
- 🧩 Supports multi-line variable values (e.g., JSON strings)
- 💾 Optional backup of the original `.env` file before any update (`.env.bak_TIMESTAMP`)
- 🔗 Supports multiple services, each with its own repository URL and local file path
- 🌐 Automatically converts standard GitHub URLs to raw content URLs

---

## 📋 Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

---

## ⚙️ Configuration

The service is configured via a `.env` file. Below is an example configuration:

### Configuration Fields

| Field | Type | Description | Default |
|---|---|---|---|
| `SAVE_BACKUP_FILE` | `bool` | If `true`, a backup of the local `.env` is created before updating | `true` |
| `TZ` | `string` | the timezone for logging (e.g., UTC, America/New_York)| `UTC` |
| `CHECK_INTERVAL` | `int` | Interval between checks for updates (in seconds) | `3600` |
| `LOG_LEVEL` | `string` | Set the log level (e.g., debug, info, warning, error) | `info` |
| `SERVICE1_ENV_PATH` | `string` | The path of the local env file of the specified service | `none` |
| `SERVICE1_ENV_REPO_URL`| `string` | The raw/standard url pointing to the remote .env file | `none`|

> [!IMPORTANT]
> For each service, specify a local and remote url/path to the .env file

For example:</br>
`SERVICE1_ENV_PATH`=/path/to/local/env</br>
`SERVICE1_ENV_REPO_URL`=https://raw.githubusercontent.com/user/repo/refs/heads/main/.env

`SERVICE2_ENV_PATH`=/path/to/local/env</br>
`SERVICE2_ENV_REPO_URL`=https://raw.githubusercontent.com/user/repo/refs/heads/main/.env


> **Note:** Both standard GitHub URLs (`https://github.com/...`) and raw URLs (`https://raw.githubusercontent.com/...`) are supported. Standard URLs are automatically converted to raw format.

---

## 🔍 How It Works

1. **Read configuration** — loads the list of services to monitor from `.env`.
2. **Read local `.env`** — reads the local file line by line via `ReadLocalEnvFile`.
3. **Fetch remote `.env`** — downloads the latest `.env` from the configured repository URL via `ReadUpdatedEnvFileFromRepository`.
4. **Extract variable names** — parses both files via `ReadEnvVariables` to extract environment variable names (lines containing `=`).
5. **Compare** — checks via `IsFileAlreadyUpdated` if all remote variable names are already present in the local file.
6. **Update** — if differences are found, `UpdateEnvFile`:
   - Appends a clearly marked section at the bottom of the local file.
   - Adds any missing variables (commented out, preserving their original values and associated comments).
   - Updates inline comments for variables already present locally.
7. **Backup** *(optional)* — saves a `.env.bak_TIMESTAMP` copy before writing changes.

---

## 📝 Behavior Details

### Variable Parsing

- Lines that are **empty**, do not contain `=`, or have **spaces in the variable name** are skipped.
- **Commented-out variables** (prefixed with `#`) are recognized and their names are extracted by stripping the `#` prefix.
- Duplicate variable names across the file are automatically **deduplicated**.

### Multi-line Values

Variables whose values span multiple lines (e.g., embedded JSON strings) are fully captured. The parser continues reading lines until it encounters an empty line, a comment, or a new variable assignment.

### Added Variables Format

New variables found in the remote `.env` are appended **commented out** at the bottom of the local file, under a clearly identifiable marker:

### Comment Syncing

If a variable already exists locally but its associated comments in the repository have changed, `EnvAutoUpdater` will:
- Remove the outdated comments above the existing variable line.
- Insert the updated comments from the repository in their place.

### Backup Files

Backup files are created in the **same directory** as the original `.env` file and follow the naming convention: `.env.bak_20250101120000`

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.