using System;
using Microsoft.Xrm.Sdk;

/// <summary>
/// PLUGIN: Account Creation Automation
/// 
/// PURPOSE:
/// This plugin automatically executes business processes when a new Account is created in Dynamics 365/CRM.
/// It runs after account creation (post-operation) and performs the following automated actions:
/// 
/// 1. Creates a default primary contact for the new account
/// 2. Sets default values and business rules on the account record
/// 3. Creates a follow-up task for the sales team
/// 4. Processes parent account relationships if specified
/// 
/// DEPLOYMENT:
/// Should be registered as a synchronous post-operation plugin on the Create message of the Account entity
/// Requires PostImage registered as "PostImage" containing the created account data
/// </summary>
public class AccountCreationAutomation : IPlugin
{
    /// <summary>
    /// Main plugin execution method - called by CRM when plugin is triggered
    /// </summary>
    /// <param name="serviceProvider">CRM service provider for accessing context and services</param>
    public void Execute(IServiceProvider serviceProvider)
    {
        // Initialize tracing service for logging and debugging
        ITracingService tracer = (ITracingService)serviceProvider.GetService(typeof(ITracingService));

        try
        {
            tracer.Trace("=== ACCOUNT CREATION AUTOMATION STARTED ===");

            // Get context and service references from service provider
            IPluginExecutionContext context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));
            IOrganizationServiceFactory serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            // Check if PostImage is available (contains the created account data)
            if (context.PostEntityImages.Contains("PostImage"))
            {
                // Retrieve the newly created account from PostImage
                Entity account = context.PostEntityImages["PostImage"];
                string accountName = account.GetAttributeValue<string>("name") ?? "Unknown";
                Guid accountId = account.Id;

                tracer.Trace($"New account created: {accountName} (ID: {accountId})");

                // =============================================
                // BUSINESS AUTOMATION PROCESSES:
                // =============================================

                // 1. CREATE DEFAULT CONTACT - Create primary contact for new account
                CreateDefaultContact(service, accountId, accountName, tracer);

                // 2. SET ACCOUNT DEFAULTS - Apply standard business rules and values
                SetAccountDefaults(service, accountId, tracer);

                // 3. CREATE FOLLOW-UP TASK - Generate task for sales team follow-up
                CreateFollowUpTask(service, accountId, accountName, tracer);

                // 4. PROCESS PARENT ACCOUNT - Handle parent-child account relationships
                ProcessParentAccount(service, account, tracer);

                tracer.Trace("=== ACCOUNT AUTOMATION COMPLETED SUCCESSFULLY ===");
            }
            else
            {
                tracer.Trace("⚠️ No PostImage found - automation skipped");
            }
        }
        catch (Exception ex)
        {
            // Log errors but don't fail the account creation transaction
            tracer.Trace($"❌ AUTOMATION ERROR: {ex.Message}");
            tracer.Trace($"Stack Trace: {ex.StackTrace}");
            // Log but don't fail the account creation
        }
    }

    /// <summary>
    /// Creates a default primary contact for new accounts
    /// </summary>
    /// <param name="service">Organization service for data operations</param>
    /// <param name="accountId">GUID of the newly created account</param>
    /// <param name="accountName">Name of the new account</param>
    /// <param name="tracer">Tracing service for logging</param>
    private void CreateDefaultContact(IOrganizationService service, Guid accountId, string accountName, ITracingService tracer)
    {
        try
        {
            // Create new contact entity
            Entity contact = new Entity("contact");
            contact["firstname"] = "Primary";
            contact["lastname"] = "Contact for " + accountName;
            contact["parentcustomerid"] = new EntityReference("account", accountId); // Link to account
            contact["emailaddress1"] = GenerateContactEmail(accountName); // Generate formatted email
            contact["jobtitle"] = "Primary Contact";

            // Create contact record in CRM
            Guid contactId = service.Create(contact);
            tracer.Trace($"✅ Created default contact: {contactId}");
        }
        catch (Exception ex)
        {
            tracer.Trace($"⚠️ Contact creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Sets default business rules and values for new accounts
    /// </summary>
    /// <param name="service">Organization service for data operations</param>
    /// <param name="accountId">GUID of the account to update</param>
    /// <param name="tracer">Tracing service for logging</param>
    private void SetAccountDefaults(IOrganizationService service, Guid accountId, ITracingService tracer)
    {
        try
        {
            // Create update entity for the account
            Entity accountUpdate = new Entity("account", accountId);
            accountUpdate["creditonhold"] = false; // Default to not on credit hold
            accountUpdate["address1_shippingmethodcode"] = new OptionSetValue(1); // Airborne
            accountUpdate["customertypecode"] = new OptionSetValue(1); // Default customer type
            accountUpdate["statuscode"] = new OptionSetValue(1); // Active status

            // Apply the updates to the account
            service.Update(accountUpdate);
            tracer.Trace("✅ Account defaults applied successfully");
        }
        catch (Exception ex)
        {
            tracer.Trace($"⚠️ Account defaults failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates automatic follow-up task for sales team
    /// </summary>
    /// <param name="service">Organization service for data operations</param>
    /// <param name="accountId">GUID of the related account</param>
    /// <param name="accountName">Name of the account for task subject</param>
    /// <param name="tracer">Tracing service for logging</param>
    private void CreateFollowUpTask(IOrganizationService service, Guid accountId, string accountName, ITracingService tracer)
    {
        try
        {
            // Create new task entity
            Entity task = new Entity("task");
            task["subject"] = $"Welcome follow-up: {accountName}";
            task["description"] = $"New account '{accountName}' was created. Please contact them within 24 hours to welcome and gather additional information.";
            task["scheduledstart"] = DateTime.Now.AddDays(1); // Schedule for tomorrow
            task["scheduledend"] = DateTime.Now.AddDays(1).AddHours(1); // 1 hour duration
            task["prioritycode"] = new OptionSetValue(2); // Normal priority
            task["category"] = "Account Follow-up";
            task["regardingobjectid"] = new EntityReference("account", accountId); // Link to account

            // Create task record in CRM
            Guid taskId = service.Create(task);
            tracer.Trace($"✅ Follow-up task created: {taskId}");
        }
        catch (Exception ex)
        {
            tracer.Trace($"⚠️ Task creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes parent account relationships if specified
    /// </summary>
    /// <param name="service">Organization service for data operations</param>
    /// <param name="account">The account entity being processed</param>
    /// <param name="tracer">Tracing service for logging</param>
    private void ProcessParentAccount(IOrganizationService service, Entity account, ITracingService tracer)
    {
        try
        {
            // Check if account has a parent account specified
            EntityReference parentAccountRef = account.GetAttributeValue<EntityReference>("parentaccountid");
            if (parentAccountRef != null)
            {
                // Could update child count, notify parent account owner, etc.
                tracer.Trace($"✅ Parent account processed: {parentAccountRef.Name}");

                // Example: Update last child added date on parent account
                Entity parentUpdate = new Entity("account", parentAccountRef.Id);
                parentUpdate["description"] = $"Last child account added: {DateTime.Now:MM/dd/yyyy}";
                service.Update(parentUpdate);
            }
        }
        catch (Exception ex)
        {
            tracer.Trace($"⚠️ Parent account processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates email address for default contact based on account name
    /// </summary>
    /// <param name="accountName">Name of the account to generate email from</param>
    /// <returns>Formatted email address string</returns>
    private string GenerateContactEmail(string accountName)
    {
        // Clean account name by removing spaces and converting to lowercase
        string cleanName = accountName.Replace(" ", "").ToLower();
        return $"info@{cleanName}.com";
    }
}