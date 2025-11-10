# 🔄 Updating Clients

There are **two main ways** to update clients in your Pulsar setup, depending on what kind of changes were made.

---

## 🧩 Method 1: Standard Update (for simple updates) (MOST LIKELY YOU WILL NOT BE USING THIS (but it's always worth a try locally to test))

Use this method when:
- You only changed something small (like a setting, minor feature, or internal logic).
- No networking, packet structure, or communication changes were made.

### Steps:
1. Open **Client Management** in the Pulsar server interface.  
2. Click on **Update**.  
3. Press **Browse** and select the **new client file** (the updated executable) from your computer.  
4. Confirm the update and Pulsar will automatically send the new build to connected clients.

✅ **Tip:** This is the easiest and fastest way to update clients without changing any server configuration or ports.

---

## ⚙️ Method 2: Full Migration (for networking or packet changes)

Use this method when:
- You’ve modified how clients and the server communicate.
- You’ve added or removed packets.
- You’ve changed anything related to network handling or the MUTEX system.

In these cases, you need to **migrate clients to a new Pulsar instance**.

### Steps:
1. **Build a new client**:
   - Use a **different MUTEX** than the previous one.
   - Set a **new port** that the new Pulsar server will use.

2. **Run a new Pulsar server**:
   - Launch Pulsar again, but this time configure it to run on the **new port** you selected.
   - You’ll now have **two servers running** (one old, one new).

3. **Connect new clients**:
   - Deploy or run the new client build.
   - These clients will automatically connect to the **new Pulsar server** on the new port.

4. **Migrate and clean up**:
   - Once you’ve confirmed the new clients are connected and working properly, you can:
     - Disconnect or uninstall the **old clients**.
     - Close down the **old Pulsar server**.

✅ **Tip:** Always test the new server and client connection before removing the old one to avoid losing access to existing systems.

---

## 🧠 Notes & Recommendations

- Keep a backup of your old Pulsar configuration before migrating.  
- Document which ports and MUTEX values are in use to avoid overlap.  
- If clients fail to connect after migration, double-check that the **server port and client configuration match** exactly.  
