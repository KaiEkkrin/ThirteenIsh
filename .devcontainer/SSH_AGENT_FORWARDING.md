# SSH Agent Forwarding in Dev Container

## Overview

This dev container is configured to forward your host's SSH agent into the container, enabling you to use your SSH keys for Git operations (clone, fetch, push) and other SSH connections without copying private keys into the container.

## Why This Configuration Exists

### The Problem with Podman on Fedora

VS Code Dev Containers has automatic SSH agent forwarding that works seamlessly with Docker, but this automatic forwarding doesn't work reliably with Podman on Fedora due to differences in how Podman handles:

1. Unix socket mounting
2. User namespace mapping (with `--userns=keep-id`)
3. Container runtime implementation

### The Solution

We've added explicit configuration in [devcontainer.json](devcontainer.json) to mount the SSH agent socket:

```json
{
  "runArgs": [
    "--userns=keep-id",
    "--volume=${env:SSH_AUTH_SOCK}:/ssh-agent.sock"
  ],
  "remoteEnv": {
    "SSH_AUTH_SOCK": "/ssh-agent.sock"
  }
}
```

**Why `runArgs` instead of `mounts`?**

Podman has a [known issue (#25212)](https://github.com/containers/podman/issues/25212) where mounting single files (especially Unix sockets) via the `mounts` array creates an empty socket file instead of a proper bind mount. The workaround is to use `runArgs` with the `--volume` flag, which properly bind-mounts the socket.

This configuration:
- Mounts your host's SSH agent socket (from `$SSH_AUTH_SOCK`) to `/ssh-agent.sock` in the container using `--volume` in `runArgs`
- Sets the `SSH_AUTH_SOCK` environment variable inside the container to point to the mounted socket
- Works with any SSH agent: 1Password, ssh-agent, gpg-agent, GNOME Keyring, etc.

## Supported SSH Agents

This configuration automatically works with any SSH agent that sets the `SSH_AUTH_SOCK` environment variable on your host:

- **1Password SSH Agent**: `~/.1password/agent.sock`
- **GNOME Keyring**: `/run/user/<UID>/keyring/ssh`
- **ssh-agent**: `/tmp/ssh-XXXXX/agent.XXXX` (varies)
- **gpg-agent**: `/run/user/<UID>/gnupg/S.gpg-agent.ssh`

No configuration changes are needed when switching between SSH agents - as long as `SSH_AUTH_SOCK` is set correctly on your host, it will work.

## Docker vs Podman Differences

### With Docker

Docker + VS Code typically provides automatic SSH agent forwarding without any configuration in `devcontainer.json`. The Dev Containers extension handles this transparently.

### With Podman (This Project)

Podman requires explicit configuration (as shown above) because:

1. **Different Socket Handling**: Podman's rootless mode handles Unix sockets differently than Docker
2. **User Namespace Mapping**: Our configuration uses `--userns=keep-id` which maps the host user into the container, requiring careful socket permission handling
3. **VS Code Integration**: The automatic forwarding was designed primarily for Docker, not Podman

### If Switching to Docker

If you switch from Podman to Docker, you have two options:

1. **Keep this configuration** (Recommended): It will continue to work with Docker and provides:
   - Transparency and documentation of what's happening
   - Easier debugging if issues arise
   - Compatibility for team members using different container runtimes

2. **Remove this configuration**: Docker + VS Code should automatically forward your SSH agent without these settings

## Verification

After starting the dev container, verify SSH agent forwarding is working:

```bash
# Check the environment variable is set
echo $SSH_AUTH_SOCK
# Should output: /ssh-agent.sock

# Check the socket file exists
ls -la /ssh-agent.sock
# Should show a socket file

# List SSH keys available from the agent
ssh-add -l
# Should list your SSH keys

# Test GitHub SSH authentication
ssh -T git@github.com
# Should output: Hi [your-username]! You've successfully authenticated...
```

## Troubleshooting

### Socket exists but "agent has no identities"

If you see the socket file exists (`ls -la /ssh-agent.sock` shows a socket) but `ssh-add -l` returns "The agent has no identities", this means the socket is not properly bind-mounted from the host. This is the exact issue we fixed by moving from `mounts` to `runArgs` with `--volume`.

**Solution**: Ensure you're using the `runArgs` configuration shown above, not the `mounts` array for the SSH socket.

### SSH agent not working in container

1. **Check SSH agent is running on your host**:
   ```bash
   # On host (outside container)
   echo $SSH_AUTH_SOCK
   # Should show a path to a socket file

   ls -la $SSH_AUTH_SOCK
   # Should show the socket exists

   ssh-add -l
   # Should list your keys
   ```

2. **Rebuild the dev container**:
   - Open Command Palette (Ctrl+Shift+P / Cmd+Shift+P)
   - Run: "Dev Containers: Rebuild Container"

3. **Check for SELinux issues** (Fedora with SELinux Enforcing):
   If the socket mount still doesn't work after using `runArgs`, SELinux might be blocking access.
   ```bash
   # On host
   getenforce
   # If "Enforcing", check for denials:
   sudo ausearch -m avc -ts recent | grep ssh-agent
   ```

   **SELinux Workaround** (if needed):
   If SELinux is blocking the socket mount, you can temporarily disable SELinux labeling for the container by adding to `runArgs`:
   ```json
   "runArgs": [
     "--userns=keep-id",
     "--volume=${env:SSH_AUTH_SOCK}:/ssh-agent.sock",
     "--security-opt=label=disable"
   ]
   ```
   Note: This reduces container isolation. Only use if the socket mount fails due to SELinux.

### Permission denied errors

If you get "permission denied" when trying to use SSH in the container:

1. **Verify user namespace mapping**:
   ```bash
   # Inside container
   id
   # Your UID should match your host UID
   ```

2. **Check socket permissions on host**:
   ```bash
   # On host
   ls -la $SSH_AUTH_SOCK
   # Should be owned by your user
   ```

### Using a specific SSH agent

If you want to always use a specific SSH agent (e.g., 1Password) regardless of what `SSH_AUTH_SOCK` points to, modify the mount in `devcontainer.json`:

```json
"mounts": [
  "source=${env:HOME}/.1password/agent.sock,target=/ssh-agent.sock,type=bind"
]
```

**Note**: This ties the configuration to a specific SSH agent and won't work if you switch agents.

## Security Considerations

### What Gets Forwarded

When SSH agent forwarding is enabled:
- The container can request your SSH agent to sign authentication requests
- Your private keys remain on the host and are never copied into the container
- The container never has direct access to your private keys

### Trust

You should only enable SSH agent forwarding in containers you trust, as:
- Any process in the container can use your SSH agent
- Malicious code in the container could potentially use your SSH keys to authenticate

This dev container is for development purposes and runs code you control, so this trust model is appropriate.

## Additional Resources

- [1Password SSH Agent Documentation](https://developer.1password.com/docs/ssh/agent/)
- [VS Code Dev Containers Documentation](https://code.visualstudio.com/docs/devcontainers/containers)
- [Podman Rootless Tutorial](https://github.com/containers/podman/blob/main/docs/tutorials/rootless_tutorial.md)
- [SSH Agent Forwarding Best Practices](https://developer.github.com/v3/guides/using-ssh-agent-forwarding/)
