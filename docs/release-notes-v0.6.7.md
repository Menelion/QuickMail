# QuickMail v0.6.7 Release Notes

## New Features

### Mark as Read (Ctrl+Q)

A new **Mark as Read** command, bound to **Ctrl+Q** by default, marks the current selection as read without opening it. The command is context-sensitive:

| Context | What gets marked |
|---------|-----------------|
| A single message is selected | That message |
| A group header is selected (Conversations, By Sender, or By Recipient view) | Every message in the group |
| The folder tree has focus | Every message loaded in the current folder or virtual folder |

The shortcut is remappable via **File → Settings → Keyboard Shortcuts** and appears in the Command Palette (**Ctrl+Shift+P**).

---

### Action-first log format

The **Advanced** tab in **File → Settings** now includes a **Log Format** option controlling how lines are written to `quickmail.log`.

**Action first** (the new default) puts the message before the timestamp:

```
Sync completed: 42 messages  [2024-11-15 14:23:45.123]
```

**Time first** preserves the original format with the timestamp at the start:

```
2024-11-15 14:23:45.123  Sync completed: 42 messages
```

Because the log is already in chronological order, the timestamp at the start added no navigation value — it just pushed the information you actually wanted to the right. Action first makes each line readable from the beginning, which is significantly better when reviewing the log with a screen reader. The setting takes effect immediately when you save; no restart is needed.
