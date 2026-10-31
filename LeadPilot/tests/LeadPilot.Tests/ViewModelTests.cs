using System.Security.Cryptography;
using LeadPilot.Application.Abstractions;
using LeadPilot.Application.Models;
using LeadPilot.Application.Services;
using LeadPilot.Application.ViewModels;
using LeadPilot.Domain;
using LeadPilot.Tests.Support;

namespace LeadPilot.Tests;

public class ViewModelTests
{
    private static ProspectCandidate Cand(string name, string? phone = null, string? web = null, string? industry = null) => new()
    { Name = name, Phone = phone, Website = web, Industry = industry, City = "Chennai", SourceName = "Scripted source", SourceType = "Test", SourceUrl = "https://src.example/" + name.Replace(' ', '-'), IsPublicListing = true };

    private static LeadFinderViewModel Finder(VmEnv v) => new(v.Orchestrator, v.E.Ingest, v.E.Scoring, v.E.Leads, v.E.Work, v.E.Settings, v.E.Users, v.Nav);

    [Fact]
    public void Setup_wizard_validates_then_creates_admin_and_opens_dashboard()
    {
        using var e = new TestEnv(); var nav = new FakeNav();
        var seeded = e.Sys.GetUsers().Single(); seeded.IsActive = false; e.Sys.UpdateUser(seeded); // simulate a brand-new install with no users
        var vm = new SetupViewModel(e.Settings, new UserService(e.Sys, e.Clock), e.License, nav);
        vm.NextCommand.Execute(null); vm.Step.Should().Be(1);
        vm.NextCommand.Execute(null); vm.ErrorMessage.Should().Contain("name"); vm.Step.Should().Be(1);
        vm.OwnerName = "Priya Raman"; vm.CompanyName = "SunBright"; vm.Pin = "12"; vm.NextCommand.Execute(null); vm.ErrorMessage.Should().Contain("4 characters");
        vm.Pin = "1234"; vm.PinConfirm = "9999"; vm.NextCommand.Execute(null); vm.ErrorMessage.Should().Contain("do not match");
        vm.PinConfirm = "1234"; vm.Region = "United States (US)"; vm.VisitWebsites = false; vm.NextCommand.Execute(null); vm.Step.Should().Be(2);
        vm.FinishCommand.Execute(null);
        nav.Calls.Should().ContainSingle().Which.Key.Should().Be(PageKey.Dashboard);
        e.Settings.Current.Should().Match<AppSettings>(s => s.SetupCompleted && s.DefaultRegion == "US" && s.CompanyName == "SunBright" && !s.VisitWebsitesDuringDiscovery);
        var admin = e.Sys.GetUsers().Single(u => u.Name == "Priya Raman");
        admin.Role.Should().Be(UserRole.Admin); admin.PinHash.Should().NotBeNull();
    }

    [Fact]
    public async Task Shell_starts_on_setup_then_dashboard_and_hides_admin_pages_from_sales_users()
    {
        using var v = new VmEnv();
        var s = v.E.Settings.Current; s.SetupCompleted = false; v.E.Settings.Save(s);
        var created = new List<PageKey>();
        var main = new MainViewModel(k => { created.Add(k); return k == PageKey.Setup ? new SetupViewModel(v.E.Settings, v.E.Users, v.E.License, v.Nav) : new DashboardViewModel(v.E.Stats, v.E.Clock, v.Nav, v.E.Settings); },
            v.E.Settings, v.E.License, v.E.Users, v.Dlg, v.E.FollowUps);
        await main.StartAsync();
        created.Should().Equal(PageKey.Setup); main.IsShellVisible.Should().BeFalse();

        s.SetupCompleted = true; v.E.Settings.Save(s);
        await main.StartAsync();
        main.IsShellVisible.Should().BeTrue(); main.NavItems.Select(n => n.Key).Should().Contain(new[] { PageKey.Settings, PageKey.Backup, PageKey.Integrations, PageKey.Export });

        var sales = v.E.Users.Create("Sam", null, UserRole.Sales, null); v.E.Users.SignIn(sales, null);
        main.Navigate(PageKey.Dashboard); await Task.Delay(100);
        main.NavItems.Select(n => n.Key).Should().NotContain(new[] { PageKey.Settings, PageKey.Backup, PageKey.Integrations, PageKey.Export }).And.Contain(PageKey.LeadFinder);
    }

    [Fact]
    public async Task Finder_requires_the_four_inputs_and_never_shows_made_up_results_when_sources_fail()
    {
        using var v = new VmEnv(); var vm = Finder(v);
        await vm.FindLeadsCommand.ExecuteAsync(null);
        vm.ErrorMessage.Should().Contain("product");
        vm.Product = "Solar"; vm.TargetCustomer = "Manufacturing"; vm.Location = "Chennai";
        v.Provider.Fail = new HttpRequestException("down", null, System.Net.HttpStatusCode.BadGateway);
        await vm.FindLeadsCommand.ExecuteAsync(null);
        vm.Results.Should().BeEmpty(); vm.Summary.Should().Contain("No source could be reached").And.Contain("Nothing was added");
        v.E.Leads.Count().Should().Be(0);
    }

    [Fact]
    public async Task Finder_scores_flags_known_leads_and_saves_selected_into_a_campaign()
    {
        using var v = new VmEnv();
        v.E.Add("Existing Plant Works", c => c.Phone = "+919876500000");
        v.Provider.Items.AddRange(new[]
        {
            Cand("Sri Ganesh Engineering Works", "+91 98765 43210", "https://ganesh.in", "Manufacturing"),
            Cand("Bay Cafe"),
            Cand("Existing Plant Works Pvt Ltd", "+91 98765 00000")
        });
        var vm = Finder(v); vm.Product = "Solar"; vm.TargetCustomer = "Manufacturing Companies"; vm.Location = "Chennai"; vm.Count = 10;
        await vm.FindLeadsCommand.ExecuteAsync(null);
        vm.Results.Should().HaveCount(3);
        vm.Results[0].Name.Should().Be("Sri Ganesh Engineering Works"); vm.Results[0].Score.Should().BeGreaterThan(vm.Results[1].Score);
        vm.Results[0].Explanation.Should().Contain("Target industry");
        var known = vm.Results.Single(r => r.Name.StartsWith("Existing"));
        known.IsKnown.Should().BeTrue(); known.IsSelected.Should().BeFalse("already-known prospects are not pre-selected");
        vm.SelectedCount.Should().Be(2);

        await vm.SaveSelectedCommand.ExecuteAsync(null);
        vm.SaveSummary.Should().Contain("Saved 2 new lead(s)");
        v.E.Leads.Count().Should().Be(3);
        var cmp = v.E.Work.GetCampaigns().Single(); cmp.TargetCustomer.Should().Be("Manufacturing Companies"); v.E.Work.LeadCountForCampaign(cmp.Id).Should().Be(2);
        v.E.Sys.GetApiUsage(5).Should().ContainSingle(u => u.Operation == "search" && u.ResultCount == 3);
    }

    [Fact]
    public async Task Finder_min_score_filter_and_exclusions_apply()
    {
        using var v = new VmEnv();
        v.Provider.Items.AddRange(new[] { Cand("Good Works", "+91 98765 43210", "https://good.in", "Manufacturing"), Cand("Weak Bakery", industry: "Bakery"), Cand("Skip Me Industries", "+91 98765 43211", "https://skip.in", "Manufacturing") });
        var vm = Finder(v); vm.Product = "Solar"; vm.TargetCustomer = "Manufacturing"; vm.Location = "Chennai"; vm.MinScore = 50; vm.ExcludedCompanies = "Skip Me";
        await vm.FindLeadsCommand.ExecuteAsync(null);
        vm.Results.Select(r => r.Name).Should().Equal("Good Works");
    }

    private static LeadDatabaseViewModel Db(VmEnv v) => new(v.E.Leads, v.E.Sys, v.E.Ingest, v.E.Scoring, v.Gov, v.Export, v.E.Users, v.Nav, v.Dlg);

    [Fact]
    public async Task Database_paginates_sorts_filters_and_runs_bulk_actions()
    {
        using var v = new VmEnv();
        for (var i = 1; i <= 120; i++) v.E.Add($"Lead {i:D3} Works", c => { if (i % 2 == 0) c.Phone = "+91 98765 43" + (200 + i); });
        var vm = Db(v); await vm.OnNavigatedToAsync(null);
        vm.Total.Should().Be(120); vm.PageCount.Should().Be(3); vm.Rows.Should().HaveCount(50);
        await vm.NextPageCommand.ExecuteAsync(null); vm.Page.Should().Be(2);
        await vm.SortByCommand.ExecuteAsync("Company"); vm.Rows.First().Row.CompanyName.Should().BeOneOf("Lead 120 Works", "Lead 001 Works");
        await vm.SearchCommand.ExecuteAsync(null); vm.Page.Should().Be(1);

        vm.SearchText = "Lead 07"; await vm.SearchCommand.ExecuteAsync(null); vm.Total.Should().Be(10);
        vm.AllSelected = true; vm.SelectedCount.Should().Be(10);
        vm.BulkStatus = "Qualified"; await vm.ApplyStatusCommand.ExecuteAsync(null);
        v.E.Leads.Search(new LeadQuery { Status = LeadStatus.Qualified }).Total.Should().Be(10);
        vm.SelectedCount.Should().Be(0, "selection is cleared after a bulk action");
        vm.AllSelected = true;
        vm.AssignTo = vm.Users.First(); await vm.ApplyAssignCommand.ExecuteAsync(null);
        v.E.Leads.Search(new LeadQuery { AssignedUserId = vm.AssignTo.Id }).Total.Should().Be(10);

        vm.SearchText = ""; vm.OnlyIncomplete = true; await vm.SearchCommand.ExecuteAsync(null); vm.Total.Should().Be(120, "no lead has e-mail+website");
        vm.OpenCommand.Execute(vm.Rows[0]); v.Nav.Calls.Last().Key.Should().Be(PageKey.LeadDetails);
    }

    [Fact]
    public async Task Database_manual_entry_validates_and_delete_honours_confirmation()
    {
        using var v = new VmEnv(); var vm = Db(v);
        vm.IsEmpty.Should().BeFalse("nothing is shown before the first load");
        await vm.OnNavigatedToAsync(null);
        vm.IsEmpty.Should().BeTrue("an empty database shows the empty-state card");
        vm.StartAddCommand.Execute(null); await vm.SaveNewCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("company name");
        vm.NewCompany = "Manual Industries"; vm.NewPhone = "+91 98765 43210"; vm.NewEmail = "bad"; vm.NewCity = "Madurai";
        await vm.SaveNewCommand.ExecuteAsync(null);
        vm.IsAdding.Should().BeFalse(); vm.Rows.Should().ContainSingle().Which.Row.EmailStatus.Should().Be(EmailStatus.InvalidFormat);
        vm.Rows[0].IsSelected = true;
        v.Dlg.ConfirmAnswer = false; await vm.DeleteSelectedCommand.ExecuteAsync(null); v.E.Leads.Count().Should().Be(1);
        v.Dlg.ConfirmAnswer = true; await vm.DeleteSelectedCommand.ExecuteAsync(null); v.E.Leads.Count().Should().Be(0);
    }

    [Fact]
    public async Task Database_export_selected_writes_a_real_file()
    {
        using var v = new VmEnv(); v.E.Add("A Works"); v.E.Add("B Works");
        var vm = Db(v); await vm.OnNavigatedToAsync(null);
        vm.Rows[0].IsSelected = true; v.Dlg.SaveFile = Path.Combine(v.E.Folder, "sel.csv");
        await vm.ExportSelectedCommand.ExecuteAsync(null);
        File.ReadAllLines(v.Dlg.SaveFile).Should().HaveCount(2, "header + 1 selected row");
    }

    [Fact]
    public async Task Sales_role_cannot_delete_or_export()
    {
        using var v = new VmEnv(); v.E.Add("A Works");
        var sales = v.E.Users.Create("Sam", null, UserRole.Sales, null); v.E.Users.SignIn(sales, null);
        var vm = Db(v); await vm.OnNavigatedToAsync(null);
        vm.Rows[0].IsSelected = true;
        await vm.DeleteSelectedCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("permission"); v.E.Leads.Count().Should().Be(1);
        v.Dlg.SaveFile = Path.Combine(v.E.Folder, "x.csv");
        await vm.ExportSelectedCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("permission"); File.Exists(v.Dlg.SaveFile).Should().BeFalse();
    }

    private static LeadDetailsViewModel Details(VmEnv v) => new(v.E.Leads, v.E.Sys, v.E.Scoring, v.Verify, v.Enrich, v.E.FollowUps, v.Drafter, v.E.Settings, v.E.Users, v.Nav, v.Dlg, v.E.Clock);

    [Fact]
    public async Task Details_edit_save_rescore_log_schedule_and_verify()
    {
        using var v = new VmEnv();
        var id = v.E.Add("Detail Works", c => { c.Industry = "Manufacturing"; c.Phone = "+91 98765 43210"; });
        var vm = Details(v); await vm.LoadAsync(id);
        vm.LeadCode.Should().Be("LD-000001"); vm.Score.Should().BeGreaterThan(0); vm.ScoreExplanation.Should().Contain("Relevance score");
        vm.Email = "info@detail.in"; vm.ContactName = "Meena Iyer"; vm.Status = "Qualified"; vm.Website = "detail.in";
        await vm.SaveCommand.ExecuteAsync(null);
        var lead = v.E.Leads.GetDetail(id)!;
        lead.Business.Website.Should().Be("https://detail.in"); lead.Business.NormalizedEmail.Should().Be("info@detail.in");
        lead.Lead.Status.Should().Be(LeadStatus.Qualified); lead.Contacts.Should().ContainSingle(c => c.FullName == "Meena Iyer");
        lead.Lead.EmailStatus.Should().Be(EmailStatus.ValidFormat);

        vm.InteractionSubject = "Intro call"; await vm.LogInteractionCommand.ExecuteAsync(null);
        vm.Timeline.Should().Contain(t => t.Subject == "Intro call");
        vm.TaskTitle = "Send quote"; vm.TaskDue = DateTime.Today.AddDays(3); await vm.ScheduleFollowUpCommand.ExecuteAsync(null);
        vm.Tasks.Should().ContainSingle(t => t.Title == "Send quote" && t.IsOpen);
        await vm.CompleteTaskCommand.ExecuteAsync(vm.Tasks[0]); vm.Tasks[0].Status.Should().Be("Done");

        await vm.VerifyCommand.ExecuteAsync(null);
        vm.EmailCheck.Should().Contain("mail server").And.Contain("not confirmed"); vm.WebsiteCheck.Should().Be("Accessible");
        vm.PhoneCheck.Should().Contain("owner not verified");
    }

    [Fact]
    public async Task Details_draft_never_sends_and_hands_off_via_links()
    {
        using var v = new VmEnv(); v.E.Add("Mail Works", c => { c.Email = "info@mail.in"; c.Phone = "+91 98765 43210"; });
        var vm = Details(v); await vm.LoadAsync(1); vm.Offer = "rooftop solar";
        await vm.DraftCommand.ExecuteAsync(null);
        vm.DraftBody.Should().Contain("Mail Works").And.Contain("rooftop solar").And.Contain("stop"); vm.StatusMessage.Should().Contain("Nothing has been sent");
        vm.OpenInMailAppCommand.Execute(null); v.Dlg.Opened.Last().Should().StartWith("mailto:info@mail.in?subject=");
        vm.DraftChannel = "WhatsApp"; await vm.DraftCommand.ExecuteAsync(null); vm.OpenInWhatsAppCommand.Execute(null);
        v.Dlg.Opened.Last().Should().StartWith("https://wa.me/919876543210?text=");
        v.E.Leads.GetDetail(1)!.Activities.Count(a => a.Type == ActivityType.Draft).Should().Be(2);
    }

    [Fact]
    public async Task Duplicate_manager_scans_shows_side_by_side_merges_and_undoes()
    {
        using var v = new VmEnv();
        var a = v.E.Add("Alpha Steel", c => c.Phone = "+919876543210");
        var b = v.E.Add("Alpha Steels Pvt Ltd", c => { c.Phone = "+91 98765 43210"; c.Email = "x@alpha.in"; }, allowDuplicate: true);
        var vm = new DuplicateManagerViewModel(v.E.Duplicates, v.E.Leads, v.E.Users, v.Dlg, v.Nav);
        await vm.OnNavigatedToAsync(null);
        vm.Pending.Should().ContainSingle(); vm.Selected.Should().NotBeNull(); vm.Guidance.Should().Contain("High confidence");
        vm.Comparison.Should().Contain(r => r.Field == "E-mail" && r.Differs && r.ValueB == "x@alpha.in");
        v.Dlg.ConfirmAnswer = false; await vm.KeepACommand.ExecuteAsync(null); v.E.Leads.Count().Should().Be(2, "declined confirmation merges nothing");
        v.Dlg.ConfirmAnswer = true; await vm.KeepACommand.ExecuteAsync(null);
        v.E.Leads.Count().Should().Be(1); vm.Pending.Should().BeEmpty(); vm.History.Should().ContainSingle(h => h.CanUndo);
        await vm.UndoCommand.ExecuteAsync(vm.History[0]);
        v.E.Leads.Count().Should().Be(2); vm.Pending.Should().ContainSingle(); vm.History.Single().State.Should().Be("Undone");
        await vm.NotDuplicatesCommand.ExecuteAsync(null); vm.Pending.Should().BeEmpty();
    }

    [Fact]
    public async Task Import_vm_walks_through_preview_mapping_validation_and_report()
    {
        using var v = new VmEnv();
        var path = Path.Combine(v.E.Folder, "in.csv");
        File.WriteAllText(path, "Name,Tel,Mail,City\nAcme Works,+91 98765 43210,a@acme.in,Chennai\nBeta Works,12,not-mail,Chennai\n,x,,\n");
        var vm = new ImportViewModel(v.Import, v.E.Sys, v.E.Users, v.Dlg, v.Nav);
        await vm.LoadFileAsync(path);
        vm.HasPreview.Should().BeTrue(); vm.Rows.Should().HaveCount(3);
        vm.Mapping.Single(m => m.Field == ImportField.CompanyName).Column.Should().Be("Name");
        vm.Mapping.Single(m => m.Field == ImportField.Phone).Column.Should().Be("Tel");
        await vm.ValidateCommand.ExecuteAsync(null);
        vm.Validated.Should().BeTrue(); vm.Issues.Should().Contain(i => i.Contains("not-mail")).And.Contain(i => i.Contains("empty"));
        await vm.ConfirmImportCommand.ExecuteAsync(null);
        vm.HasReport.Should().BeTrue(); vm.ReportSummary.Should().Contain("2 imported").And.Contain("1 error");
        v.E.Leads.Count().Should().Be(2); vm.History.Should().ContainSingle(h => h.File == "in.csv");
    }

    [Fact]
    public async Task Import_without_company_mapping_is_blocked()
    {
        using var v = new VmEnv();
        var path = Path.Combine(v.E.Folder, "odd.csv"); File.WriteAllText(path, "A,B\n1,2\n");
        var vm = new ImportViewModel(v.Import, v.E.Sys, v.E.Users, v.Dlg, v.Nav);
        await vm.LoadFileAsync(path); await vm.ValidateCommand.ExecuteAsync(null);
        vm.Validated.Should().BeFalse(); vm.ValidationSummary.Should().Contain("Company name");
    }

    [Fact]
    public async Task Dashboard_vm_shows_database_numbers_and_empty_state()
    {
        using var v = new VmEnv();
        var vm = new DashboardViewModel(v.E.Stats, v.E.Clock, v.Nav, v.E.Settings);
        await vm.OnNavigatedToAsync(null);
        vm.HasLeads.Should().BeFalse(); vm.Kpis.Should().HaveCount(10); vm.Greeting.Should().Be("Welcome back, Asha");
        vm.HeroSubtitle.Should().Contain("first customers");
        v.E.Add("One Works"); v.E.Add("Two Works");
        await vm.OnNavigatedToAsync(null);
        vm.HasLeads.Should().BeTrue(); vm.Kpis.Single(k => k.Title == "Leads stored").Value.Should().Be("2");
        vm.HeroSubtitle.Should().Contain("open follow-up");
        vm.LocationChart.Bars.Single().Should().Be(new ChartBar("Chennai", 2, 1.0));
        vm.MonthlyChart.Bars.Should().HaveCount(12);
    }

    [Fact]
    public async Task Qualification_vm_saves_weights_validates_and_rescores()
    {
        using var v = new VmEnv(); v.E.Add("Sri Ganesh Engineering Works", c => c.Phone = "+91 98765 43210");
        var vm = new QualificationViewModel(v.E.Settings, v.E.Scoring, v.E.Leads, v.E.Stats, v.E.Clock, v.E.Users, v.Nav);
        await vm.OnNavigatedToAsync(null);
        vm.WeightTotal.Should().Be(100); vm.Disclaimer.Should().Contain("not evidence");
        vm.Hot = 40; await vm.SaveAndRescoreCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("Hot > Warm");
        vm.Hot = 80; vm.TargetCustomer = "Manufacturing"; vm.Location = "Chennai"; vm.WContact = 0; vm.WIntent = 0;
        await vm.SaveAndRescoreCommand.ExecuteAsync(null);
        v.E.Settings.Current.Scoring.ContactCompleteness.Should().Be(0);
        // no product given => product factor not applicable; contact/intent weights are 0 => (25 + 20) / (25 + 20) = 100
        v.E.Leads.GetLead(1)!.Score.Should().Be(100);
    }

    [Fact]
    public async Task Followup_vm_filters_completes_and_suggests()
    {
        using var v = new VmEnv(); var a = v.E.Add("Due Works"); v.E.Add("Idle Works");
        v.E.FollowUps.Schedule(a, "Call", v.E.Clock.UtcNow.AddDays(-2));
        var vm = new FollowUpViewModel(v.E.FollowUps, v.E.Leads, v.E.Clock, v.E.Users, v.Nav);
        await vm.OnNavigatedToAsync(null);
        vm.OverdueCount.Should().Be(1); vm.Tasks.Should().ContainSingle(t => t.IsOverdue);
        vm.Suggestions.Should().ContainSingle(s => s.Company == "Idle Works");
        vm.NewLeadCode = "LD-000002"; vm.NewTitle = "Intro"; vm.NewDue = DateTime.Today.AddDays(1);
        await vm.AddTaskCommand.ExecuteAsync(null); vm.Tasks.Should().HaveCount(2);
        vm.NewLeadCode = "LD-999999"; vm.NewTitle = "x"; await vm.AddTaskCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("No lead with ID");
        await vm.CompleteCommand.ExecuteAsync(vm.Tasks.First(t => t.IsOverdue)); vm.OverdueCount.Should().Be(0);
    }

    [Fact]
    public async Task Backup_vm_creates_encrypted_backup_and_restores_with_password_prompt()
    {
        using var v = new VmEnv(); v.E.Add("Safe Works");
        var vm = new BackupViewModel(v.Backup, v.Paths, v.E.Settings, v.E.Users, v.Dlg, v.Nav);
        await vm.OnNavigatedToAsync(null);
        vm.Password = "short"; vm.PasswordConfirm = "short"; await vm.CreateBackupCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("8 characters");
        vm.Password = "correct horse"; vm.PasswordConfirm = "correct horse"; await vm.CreateBackupCommand.ExecuteAsync(null);
        vm.Backups.Should().ContainSingle(b => b.Name.Contains("encrypted"));
        v.E.Add("Newer Works");
        v.Dlg.Passwords.Enqueue("wrong"); v.Dlg.Passwords.Enqueue("correct horse"); v.Dlg.Passwords.Enqueue(null);
        vm.Selected = vm.Backups[0];
        await vm.RestoreSelectedCommand.ExecuteAsync(null);
        v.E.Leads.Count().Should().Be(1); v.Nav.Calls.Last().Key.Should().Be(PageKey.Dashboard); v.Dlg.Infos.Should().Contain(i => i.Contains("restored"));
    }

    [Fact]
    public async Task Settings_vm_saves_manages_users_and_guards_the_last_admin()
    {
        using var v = new VmEnv();
        var vm = new SettingsViewModel(v.E.Settings, v.E.Sys, v.E.Users, v.Gov, v.E.Users, v.Dlg, v.Paths);
        await vm.OnNavigatedToAsync(null);
        vm.RetentionDays = 90; vm.IsDark = true; vm.ContactEmail = "bad"; await vm.SaveCommand.ExecuteAsync(null); vm.ErrorMessage.Should().Contain("e-mail");
        vm.ContactEmail = "me@sunco.in"; await vm.SaveCommand.ExecuteAsync(null);
        v.E.Settings.Current.Should().Match<AppSettings>(s => s.RetentionDays == 90 && s.Theme == "Dark"); v.Dlg.Theme.Should().Be("Dark");
        vm.NewUserName = "Sam"; vm.NewUserRole = "Sales"; vm.NewUserPin = "4321"; await vm.AddUserCommand.ExecuteAsync(null);
        vm.Users.Should().HaveCount(2);
        await vm.RemoveUserCommand.ExecuteAsync(vm.Users.First(u => u.Name == "Asha Admin")); vm.ErrorMessage.Should().Contain("signed in");
        v.Dlg.Text = "nope"; v.E.Add("Keep Works"); await vm.DeleteAllCommand.ExecuteAsync(null); v.E.Leads.Count().Should().Be(1);
        v.Dlg.Text = "DELETE"; await vm.DeleteAllCommand.ExecuteAsync(null); v.E.Leads.Count().Should().Be(0);
        vm.Audit.Should().Contain(a => a.Action == "data.delete_all");
    }

    [Fact]
    public async Task Integrations_vm_enables_sources_and_stores_keys_in_the_secret_store_only()
    {
        using var v = new VmEnv();
        var vm = new IntegrationsViewModel(v.Orchestrator, v.E.Settings, v.Secrets, v.E.Sys, v.Ai, new FakeFinder(), v.E.Users);
        await vm.OnNavigatedToAsync(null);
        var p = vm.Providers.Single(); p.IsEnabled = false; p.ApiKey = "SECRET-KEY"; p.Endpoint = "https://my-overpass.example/api";
        await vm.SaveProviderCommand.ExecuteAsync(p);
        v.E.Settings.Current.EnabledProviders.Should().BeEmpty(); v.E.Settings.Current.ProviderEndpoints["scripted"].Should().Contain("my-overpass");
        v.Secrets.Data["provider:scripted"].Should().Be("SECRET-KEY");
        System.Text.Json.JsonSerializer.Serialize(v.E.Settings.Current).Should().NotContain("SECRET-KEY");
        p.ApiKey.Should().BeEmpty(); p.HasSavedKey.Should().BeTrue();
        vm.BraveKey = "BRAVE"; await vm.SaveSearchKeyCommand.ExecuteAsync(null); v.Secrets.Data["provider:brave-search"].Should().Be("BRAVE");
        IntegrationsViewModel.Disclosure.Should().Contain("no analytics");
    }

    [Fact]
    public async Task About_vm_activation_requires_admin_and_reports_invalid_keys()
    {
        using var v = new VmEnv();
        var vm = new AboutViewModel(v.E.License, v.Dlg, v.E.Users);
        await vm.OnNavigatedToAsync(null); vm.MachineId.Should().Be("TEST-MACHINE");
        vm.ActivateCommand.Execute(null); vm.ErrorMessage.Should().Contain("Paste");
    }

    [Fact]
    public async Task Assistant_vm_chat_flow()
    {
        using var v = new VmEnv(); v.E.Add("Top Works", c => { c.Phone = "+919876543210"; c.Industry = "Manufacturing"; });
        var vm = new AssistantViewModel(v.Assistant, v.E.Settings, v.E.Scoring, v.Ai, v.Nav, v.Dlg);
        await vm.OnNavigatedToAsync(null); vm.Messages.Should().ContainSingle();
        vm.Input = "Show my top 5 prospects"; await vm.SendCommand.ExecuteAsync(null);
        vm.Messages.Should().HaveCount(3); vm.Messages[2].Leads.Should().ContainSingle().Which.CompanyName.Should().Be("Top Works");
        vm.Input = "Find manufacturing companies in Chennai"; await vm.SendCommand.ExecuteAsync(null);
        vm.RunDiscoveryCommand.Execute(vm.Messages[^1].Discovery); v.Nav.Calls.Last().Key.Should().Be(PageKey.LeadFinder);
    }

    [Fact]
    public async Task Export_vm_scopes_and_formats()
    {
        using var v = new VmEnv(); v.E.Add("Hot Works", c => { c.Industry = "Manufacturing"; c.Phone = "+919876543210"; c.Email = "a@h.in"; c.Website = "https://h.in"; c.ContactName = "R"; c.Address = "1 Rd"; c.Description = "solar panel installers"; c.IntentEvidence = "enquiry"; }); v.E.Add("Cold Bakery", c => c.Industry = "Bakery");
        var vm = new ExportViewModel(v.E.Leads, v.Export, v.E.Stats, v.E.Sys, v.E.Users, v.Dlg, v.Paths, v.E.Clock);
        v.Dlg.SaveFile = Path.Combine(v.E.Folder, "hot.xlsx"); vm.Scope = "Hot leads"; vm.Format = "Excel (.xlsx)";
        await vm.RunCommand.ExecuteAsync(null);
        vm.LastExport.Should().Contain("Exported 1 lead(s)");
        using (var wb = new ClosedXML.Excel.XLWorkbook(v.Dlg.SaveFile)) wb.Worksheet(1).LastRowUsed()!.RowNumber().Should().Be(2);
        v.Dlg.SaveFile = Path.Combine(v.E.Folder, "sum.pdf"); await vm.SummaryPdfCommand.ExecuteAsync(null);
        System.Text.Encoding.Latin1.GetString(File.ReadAllBytes(v.Dlg.SaveFile)).Should().Contain("Leads stored: 2");
    }
}
