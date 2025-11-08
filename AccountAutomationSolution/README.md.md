# Account Creation Automation Plugin for Dynamics 365 (v9.1.40.8)

## Overview
**Plugin Name:** AccountCreationAutomation  
**Assembly:** AccountAutomationSolution  
**Dynamics 365 Version:** 9.1.40.8  
**Type:** On-Premises (Active Directory Authentication)

This plugin automates several post-creation actions for Accounts in Microsoft Dynamics 365 Customer Engagement (on-premises).

---

## 🧩 Features
When a new Account is created, this plugin automatically performs:

1. **Creates a default primary contact** for the new Account.  
2. **Sets default business rules and field values** (e.g., Credit Hold = No, Status = Active).  
3. **Creates a follow-up task** for the sales team to contact the new account.  
4. **Processes parent account relationships**, updating the parent account’s description if applicable.

---

## ⚙️ Technical Details

| Attribute | Value |
|------------|--------|
| **Plugin Class** | `AccountCreationAutomation` |
| **Implements Interface** | `IPlugin` |
| **Trigger Message** | `Create` |
| **Primary Entity** | `account` |
| **Execution Pipeline Stage** | Post-Operation |
| **Execution Mode** | Synchronous |
| **Image Name / Alias** | PostImage |
| **Image Type** | Post Image |
| **Deployment** | Server-side (on-premises) |

---

## 🧰 Prerequisites

Ensure the following NuGet packages are installed in your Visual Studio project:

```powershell
Install-Package Microsoft.CrmSdk.CoreAssemblies
Install-Package Microsoft.CrmSdk.Deployment
Install-Package Microsoft.CrmSdk.Workflow
```

---

## 🚀 Deployment Steps

### 1. Build and Register Plugin Assembly

1. Open the **Plugin Registration Tool**.
2. Click **Create New Connection** → select **On-premises**.
   - Server: `localhost`
   - Port: `5555`
   - Authentication Source: `Active Directory`
   - Check “Sign in as current user” and “Display list of available organizations”.
3. Click **Login** and select your organization.
4. Click **Register → Register New Assembly**.
   - Browse and select the compiled `.dll` file for `AccountAutomationSolution`.
   - Select **Database** storage.

### 2. Register Plugin Step

| Setting | Value |
|----------|--------|
| **Message** | Create |
| **Primary Entity** | account |
| **Execution Pipeline Stage** | Post-Operation |
| **Execution Mode** | Synchronous |
| **Event Handler** | AccountCreationAutomation |

### 3. Register Post Image

| Setting | Value |
|----------|--------|
| **Name / Alias** | PostImage |
| **Image Type** | Post Image |
| **Attributes** | All Attributes or specific fields needed |

---

## 🧪 Testing the Plugin

### 1. Create a Test Account
1. Navigate to **Sales → Accounts** in Dynamics 365.  
2. Click **New** and fill required fields:
   - **Account Name:** `Test Company XYZ`
3. Click **Save**.

### 2. Verify Plugin Execution

**Check Contact:**  
- Go to **Contacts**.  
- Verify **"Primary Contact for Test Company XYZ"** exists and is linked to the new account.

**Check Account Updates:**  
- Open the test account and verify defaults:
  - Credit Hold: No  
  - Status: Active  
  - Customer Type: Default value

**Check Follow-up Task:**  
- Go to **Activities → Tasks**.  
- Look for **"Welcome follow-up: Test Company XYZ"**.  
- Verify it’s scheduled for tomorrow.

**Check Parent Account Processing:**  
- If a parent account is set, verify that its **description** was updated.

---

## 🧾 Tracing and Logging
This plugin uses `ITracingService` for internal logs and debug tracing during runtime.  
Check the Plugin Trace Log in Dynamics 365 to review plugin execution details and messages.

---

## 📂 Project Structure

```
AccountAutomationSolution/
│
├── AccountCreationAutomation.cs    # Main plugin logic
├── Properties/
│   └── AssemblyInfo.cs
└── README.md
```

---

## ✅ Example Trace Output

```
=== ACCOUNT CREATION AUTOMATION STARTED ===
New account created: Test Company XYZ (ID: {GUID})
✅ Created default contact: {GUID}
✅ Account defaults applied successfully
✅ Follow-up task created: {GUID}
✅ Parent account processed: Parent Account Name
=== ACCOUNT AUTOMATION COMPLETED SUCCESSFULLY ===
```

---

## 🧑‍💻 Author
Developed for Microsoft Dynamics 365 CE (on-premises) v9.1.40.8  
**Purpose:** Automate new account setup and ensure consistent data and process flow.
