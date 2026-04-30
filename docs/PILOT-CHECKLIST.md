# Pilot Checklist

## Before endpoint install

- Azure App Service is deployed and reachable.
- Azure SQL migrations are applied.
- App Service Authentication is configured with Microsoft Entra.
- Admin group `Marshall Korbyt Display` resolves to object ID `f1a3237e-1402-4f30-82a7-bfdb0c70c1aa`.
- Allowed domains are `*.usc.edu` and `usc.korbyt.com`.
- URL profiles include the pilot Korbyt URL.

## Pilot display: JFFVW-DSP-01

- Confirm Chrome exists at `C:\Program Files\Google\Chrome\Application\chrome.exe`.
- Confirm kiosk user profile exists at `C:\Users\axistvuser`.
- Install the agent from an elevated PowerShell prompt.
- Confirm device registers and receives a token.
- In the admin UI, confirm `JFFVW-DSP-01` appears and is enabled.
- Assign the pilot URL profile.
- Confirm Chrome launches in the interactive kiosk session.
- Change the assigned URL and confirm the agent relaunches Chrome.
- Queue `RestartChrome` and confirm the command is picked up and completed.
- Disconnect network briefly and confirm cached last-known-good URL still launches.

## Exit criteria

- Display status becomes Healthy after check-in.
- URL assignment changes are audited.
- Enable/disable changes are audited.
- Restart command lifecycle shows Queued, PickedUp, and Completed.
- No old Startup-folder Chrome shortcut is launching a competing kiosk instance.
