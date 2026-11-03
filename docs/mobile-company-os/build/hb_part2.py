"""Handbook part 2: stages 3-7 (technology, design, workspace, backend, mobile)."""
from hb_lib import *

def stage3():
    g = glance("3 - 5 days", "Tech lead, PM", "Diagram tool, document editor", "CTO (Gate 3)")
    arch = snippet("""  [ Phone app (Flutter) ] --HTTPS--> [ API server (NestJS) ] --> [ PostgreSQL database ]
     |   local SQLite DB             |        |  \\--> [ File storage: photos, PDFs ]
     |   (offline data + outbox)     |        \\----> [ Push service: FCM ]
     \\--> works offline, syncs later  \\--> [ Logs + monitoring ]
  [ Admin web dashboard ] --HTTPS--> same API server""")
    st = [("Re-read the frozen requirements", "<p>List what really drives the technology: offline needed? photos? payments? how many users? which countries?</p>"),
          ("Ask: do we even need a mobile app?", "<p>If a responsive website or PWA can do the job, recommend it. Mobile apps cost more to build, publish and maintain.</p>"),
          ("Compare the options with the scoring table", "<p>Score each option 1-5 per criterion, multiply by weights, add up (example in OS §5). Highest score wins <i>unless</i> the team cannot support it.</p>" + plain("Flutter / native", "<b>Native</b> = separate Android (Kotlin) and iOS (Swift) apps - best device access, double cost. <b>Flutter</b> = one codebase for both - faster and cheaper, most features available.")),
          ("Draw the system diagram", arch + "<p>Each box is one thing you must build, host or buy. Each arrow is a connection that must be secure.</p>"),
          ("Design the data (ER diagram)", "<p>List the things the system remembers (Organisation, User, Site, Worker, Report, Attendance, Photo) and how they connect (a Site has many Reports; a Report has many Attendance rows). Draw boxes and lines.</p>"),
          ("List the API endpoints", table(["Method + path", "Purpose"], [["POST /v1/auth/otp/verify", "Log in"], ["GET /v1/sites", "List sites"], ["POST /v1/reports/{id}/attendance", "Save attendance"], ["POST /v1/uploads", "Upload photo"], ["POST /v1/reports/{id}/submit", "Submit day"]]) + plain("API", "A menu of requests the server understands. The phone asks, the server answers with data.")),
          ("Decide login and offline rules", "<p>Login method (OTP/password), how long sessions last, which data may be changed offline, and what happens when two devices change the same thing (conflict rule). Write each rule in one sentence.</p>"),
          ("Do a basic security think-through", checklist(["What data is sensitive?", "Where is it stored on the phone? (secure storage for tokens)", "Who can see whose data? (every request is checked on the server)", "What if the phone is lost?", "Which outside SDKs will we add, and what do they collect?"])),
          ("Write the Decision Record and get Gate 3", form("ADR-001 - Framework decision", [["Context", "Offline required, 6-person team, Android + iOS"], ["Options", "Flutter, Native, React Native, PWA"], ["Decision", "Flutter + NestJS + PostgreSQL"], ["Because", "Score table, team skills, cost"], ["Risks / revisit when", "Heavy device features needed later"], ["Approved by / date", "CTO ____"]]))]
    return stage("s3", 3, "Technology & architecture", "design", "Choose the tools and draw the blueprint before building", g,
                 plain("architecture", "The blueprint of the system: what parts exist, how they connect, and how data flows - like drawings before constructing a building.") + "<h3>Steps</h3>" + steps(st, "design") +
                 output("Decision Record (ADR) · system diagram · ER diagram · API endpoint list · login/offline/security notes · Gate 3") + "<h3>Quality check</h3>" +
                 checklist(["Options compared with scores", "Web/PWA alternative considered", "Diagram shows every component and connection", "Data model reviewed by backend and mobile developers", "Security questions answered"]) +
                 "<h3>Common mistakes</h3>" + ul(["Choosing a tool because it is popular", "No offline rule, then surprise duplicate data", "Skipping the diagram and 'keeping it in head'"]))

def stage4():
    g = glance("2 - 4 weeks", "Designer, PM, 5 test users", "Figma (or similar), prototype link", "PM + customer (Gate 4)")
    st = [("Map the user journeys", "<p>For each role draw the path from opening the app to finishing the main task. Mark where things can go wrong.</p>"),
          ("Draw the screen list and navigation", "<p>Use the same screen IDs as the requirements. Decide bottom tabs, menus and back behaviour.</p>"),
          ("Sketch low-fidelity wireframes", "<p>Black-and-white boxes only. Focus on <b>what is on the screen and where</b>, not colours. Review with the PM.</p>"),
          ("Create the design system (tokens)", table(["Token", "Example"], [["Colours", "Primary #0B5FFF, Success #0F7B45, Error #B3261E, Text #17202A"], ["Type scale", "Title 22, Body 16, Caption 12"], ["Spacing", "4 / 8 / 16 / 24 px"], ["Radius", "8 px cards, 24 px buttons"]]) + tip("Define once, reuse everywhere. Never type a colour by hand in a screen.")),
          ("Design the high-fidelity screens", "<p>Use Material guidance for Android and Human Interface guidance for iOS. Touch targets at least 48 dp (Android) / 44 pt (iOS). Text contrast at least 4.5:1.</p>"),
          ("Design every state", table(["State", "Show"], [["Loading", "Skeleton or spinner"], ["Empty", "Explain + one action button"], ["Error", "What happened + Retry"], ["Offline", "Banner + 'pending' badge"], ["Permission", "Why camera/location is needed before the system popup"]])),
          ("Make a clickable prototype", "<p>Link the screens for the 5 main journeys. A non-designer should be able to click through without help.</p>"),
          ("Test with 5 real users", "<p>Give a task ('Record today's attendance'). Watch silently. Note where they hesitate. Calculate task success rate. Fix major problems.</p>"),
          ("Hand off to developers", "<p>Share the Figma link, exported icons, tokens, and notes. Walk the developers through the prototype in a 30-minute meeting.</p>")]
    return stage("s4", 4, "UI / UX design", "design", "Decide how the app looks and feels - and test it with real users", g,
                 plain("UI vs UX", "<b>UI</b> = how it looks (colours, buttons). <b>UX</b> = how easy it is to use (steps, clarity).") + "<h3>Steps</h3>" + steps(st, "design") +
                 output("Figma file link · clickable prototype · design tokens · component list · usability test notes · Gate 4") + "<h3>Quality check</h3>" +
                 checklist(["Every PRD screen is designed in every state", "Accessibility checked (contrast, size, labels)", "5 users tested; ≥80% task success", "Original design - nothing copied from competitors", "Developers acknowledged the hand-off"]) +
                 "<h3>Common mistakes</h3>" + ul(["Designing only the happy path", "Tiny text and buttons", "Hand-off by chat message instead of a walkthrough"]))

def stage5():
    g = glance("1 - 2 days", "DevOps / tech lead", "GitHub, Git, terminal, Flutter, Node", "Tech lead (Gate 5)")
    gi = snippet("""# .gitignore (key lines)
.env
*.jks
*.keystore
key.properties
node_modules/
build/
.dart_tool/
coverage/
*.p12
*.mobileprovision""")
    envx = snippet("""# .env.example  (names only - real values live in the secret manager)
DATABASE_URL=
JWT_SIGNING_KEY=
FCM_SERVICE_ACCOUNT=
EMAIL_API_KEY=
STORAGE_BUCKET=""")
    st = [("Create the repository", "<p>GitHub → Organisation → <b>New repository</b> → name <code>sitediary</code> → <b>Private</b> → add README. Never make client code public.</p>"),
          ("Clone it", cmd("cd ~/VEL-Software/01-Projects/PRJ-2026-001-sitediary", "git clone git@github.com:<org>/sitediary.git 05-Source", "cd 05-Source")),
          ("Create the mobile and backend projects", cmd("flutter create --org in.velsoftwaresolutions --project-name sitediary mobile", "npm install -g @nestjs/cli", "nest new backend") +
              tip("<code>--org</code> becomes your Android/iOS app ID. Use a domain the company owns, reversed. It cannot be changed after publishing.")),
          ("Create the standard folders", cmd("cd mobile/lib && mkdir -p core config services models repositories features widgets navigation state && cd ../..", "mkdir -p admin-dashboard docs/{requirements,architecture,api,database,ui-ux,deployment,testing} infra .github/workflows")),
          ("Add .gitignore and .env.example", gi + envx + dont("Never commit a real password, key, keystore or customer file. Git remembers forever, even after deleting.")),
          ("Protect the main branch", "<p>GitHub → Settings → Branches (or Rulesets) → add rule for <code>main</code>: require pull request · require 1 approval · require status checks · block force-push.</p>"),
          ("Add automatic checks (CI)", "<p>Create <code>.github/workflows/ci.yml</code> (sample in the OS document §7). It runs analysis, tests and build on every pull request. Red = do not merge.</p>"),
          ("Write README and CONTRIBUTING", "<p>README: what it is, how to run mobile and backend (exact commands). CONTRIBUTING: branch names, commit style, review rules.</p>"),
          ("Create three environments", "<p>dev, staging, production - each with its own database, API URL and keys. Ask the DevOps engineer; record URLs in <code>docs/deployment</code> (no secrets).</p>"),
          ("First commit and the onboarding test", cmd("git checkout -b chore/SD-1-project-setup", "git add .", 'git commit -m "chore: initial project structure"', "git push -u origin chore/SD-1-project-setup") + "<p>Open a pull request. A teammate must be able to clone fresh and run everything in under 30 minutes.</p>")]
    git = table(["Command", "What it does"], [["git status", "See what changed"], ["git checkout -b feature/SD-14-attendance", "Make a new branch for your task"], ["git add .", "Select all changes for the next save"], ['git commit -m "feat(S-014): attendance form"', "Save a snapshot with a message"],
                   ["git push -u origin <branch>", "Upload your branch to GitHub"], ["git pull", "Download teammates' changes"], ["git log --oneline", "See history"]])
    return stage("s5", 5, "Workspace setup", "design", "Create the repository, folders, checks and environments", g,
                 plain("branch", "A private copy of the code where you work on one task. When finished you propose it back through a <b>pull request</b>.") + "<h3>Steps</h3>" + steps(st, "design") + "<h3>The 8 Git commands you will use every day</h3>" + git +
                 output("Private GitHub repo · folder structure · protected main · CI green · three environments listed · README/CONTRIBUTING · Gate 5") + "<h3>Quality check</h3>" +
                 checklist(["Fresh clone runs in &lt; 30 min", "No secrets in repo (run the secret scan)", "main is protected", "CI runs on pull requests", "Environments are separate"]) +
                 "<h3>Common mistakes</h3>" + ul(["Working directly on main", "Committing .env or keystore", "One huge commit with 'changes' as the message"]))

def stage6():
    g = glance("3 - 6 weeks (with Stage 7 in parallel)", "Backend dev, DB engineer, tech lead", "VS Code, Node, PostgreSQL, Postman/Bruno", "Reviewed at Gate 6")
    sql = snippet("""-- migrations/0004_create_attendance.sql
CREATE TABLE attendance (
  id         UUID PRIMARY KEY,
  report_id  UUID NOT NULL REFERENCES reports(id),
  worker_id  UUID NOT NULL REFERENCES workers(id),
  status     TEXT NOT NULL CHECK (status IN ('present','absent','half')),
  hours      NUMERIC(3,1) CHECK (hours BETWEEN 0 AND 16),
  note       TEXT,
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (report_id, worker_id)
);""")
    dto = snippet("""// backend/src/reports/dto/create-attendance.dto.ts
import { IsIn, IsNumber, IsOptional, IsString, IsUUID, Max, MaxLength, Min } from 'class-validator';
export class CreateAttendanceDto {
  @IsUUID() workerId: string;
  @IsIn(['present', 'absent', 'half']) status: string;
  @IsOptional() @IsNumber() @Min(0) @Max(16) hours?: number;
  @IsOptional() @IsString() @MaxLength(200) note?: string;
}""")
    ctl = snippet("""// backend/src/reports/reports.controller.ts
@Controller('v1/reports')
export class ReportsController {
  constructor(private readonly service: ReportsService) {}

  @Post(':id/attendance')
  addAttendance(@Param('id') id: string, @Body() dto: CreateAttendanceDto) {
    return this.service.addAttendance(id, dto);   // business logic lives in the service
  }
}
// main.ts: app.useGlobalPipes(new ValidationPipe({ whitelist: true }));""")
    test = snippet("""// backend/tests/attendance.e2e-spec.ts (idea)
it('rejects hours above 16', async () => {
  await request(app.getHttpServer())
    .post('/v1/reports/R1/attendance')
    .send({ workerId: W1, status: 'present', hours: 17 })
    .expect(400);
});""")
    st = [("Run PostgreSQL on your laptop", cmd("docker run --name pg-dev -e POSTGRES_PASSWORD=devpass -p 5432:5432 -d postgres:16") + tip("This password is for your laptop only. Never reuse it anywhere else.")),
          ("Write database migrations", "<p>One numbered SQL file per change. <b>Never edit a migration after it is merged</b> - add a new one.</p>" + sql + plain("migration", "A recorded step that changes the database structure, so every environment can be upgraded the same way.")),
          ("Create a module per feature", cmd("cd backend", "nest g resource reports", "npm install class-validator class-transformer") + "<p>Modules: auth, sites, workers, reports, files, notifications.</p>"),
          ("Write the endpoint with validation", dto + ctl + "<p>Controller = receives request. DTO = checks the input. Service = business logic. Repository = database access. Keep them separate.</p>"),
          ("Return errors in one standard shape", snippet('{ "code": "E-4101", "message": "Hours must be between 0 and 16", "request_id": "..." }') + dont("Never show stack traces or database errors to the app.")),
          ("Add login and permission checks", "<p>Every endpoint must check <i>who is calling</i> and <i>whether they may see this data</i> (for example only their organisation's sites). Test by trying another user's ID.</p>"),
          ("Support offline syncing", "<p>Accept an <code>Idempotency-Key</code> header so a retry from the phone never creates a duplicate. Provide <code>GET …?updated_after=</code> so the phone downloads only changes.</p>"),
          ("Write tests as you go", test + "<p>Test the good case, the bad input and the forbidden case for every endpoint.</p>"),
          ("Document the API", "<p>Generate OpenAPI (Swagger) and save it in <code>docs/api</code>. Mobile developers use it as the contract.</p>"),
          ("Log safely", "<p>Log request id, user id, route, time and error. Never log passwords, tokens or personal details.</p>"),
          ("Open pull requests, one per task", "<p>Use the pull request template: what changed, ticket, how tested, screenshots/logs.</p>")]
    return stage("s6", 6, "Backend & database", "build", "Build the server, the database and the API the app will talk to", g,
                 plain("backend", "The part users never see: it stores data, checks who may do what, and answers the phone app's requests.") + "<h3>Steps</h3>" + steps(st, "build") +
                 output("Working API on dev server · migrations · OpenAPI document · automated tests passing · merged pull requests") + "<h3>Quality check</h3>" +
                 checklist(["Every endpoint validates input", "Every endpoint checks permission", "Error format is standard", "Migrations run from empty database to latest", "Tests cover success, invalid and forbidden cases", "No secrets or personal data in logs"]) +
                 "<h3>Common mistakes</h3>" + ul(["Trusting the app to validate (the server must always validate)", "Editing old migrations", "Business rules inside controllers", "Forgetting duplicate protection on retries"]))

def stage7():
    g = glance("4 - 8 weeks (in 2-week sprints)", "Mobile devs, designer, backend dev, QA", "VS Code / Android Studio, Flutter, emulator, real phone", "CTO + PM (Gate 6)")
    val = snippet("""// mobile/lib/features/reports/validators.dart
String? validateHours(String? v) {
  if (v == null || v.isEmpty) return 'Hours required';
  final h = double.tryParse(v);
  if (h == null || h < 0 || h > 16) return 'Hours must be between 0 and 16';
  return null;
}""")
    t = snippet("""// mobile/test/validators_test.dart
import 'package:flutter_test/flutter_test.dart';
import 'package:sitediary/features/reports/validators.dart';

void main() {
  test('rejects hours above 16', () => expect(validateHours('17'), isNotNull));
  test('accepts 8 hours', () => expect(validateHours('8'), isNull));
}""")
    outbox = table(["Outbox column", "Meaning"], [["id", "Unique ID created on the phone (UUID)"], ["entity / operation", "e.g. attendance / upsert"], ["payload", "The data to send"], ["created_at", "When the user saved it"], ["attempts", "How many times sending failed"], ["status", "pending / sent / needs attention"]])
    st = [("Run the app on an emulator and a real phone", cmd("flutter devices", "flutter run") + "<p>Enable <i>Developer options → USB debugging</i> on an Android phone to test on a real device.</p>"),
          ("Take one ticket and make a branch", cmd("git checkout main && git pull", "git checkout -b feature/SD-14-attendance-form") + "<p>Read the Screen Sheet S-014 and the Figma screen. Ask questions before coding.</p>"),
          ("Build the screen from the design", "<p>Use the shared widgets and design tokens. Never copy-paste colours. Build the loading, empty, error and offline states too.</p>"),
          ("Add input validation exactly as the sheet says", val + "<p>Validation messages must match the Screen Sheet text.</p>"),
          ("Write a test for it", t + cmd("flutter test") + "<p>The test fails first, then passes after the code is right.</p>"),
          ("Connect to the API through a repository", "<p>The screen never calls the internet directly. It asks a <b>repository</b>, which decides: local database or server. This makes offline possible.</p>"),
          ("Make it work offline", plain("outbox", "A to-do list stored on the phone. Every save is written locally first, then sent when internet returns; a unique ID prevents duplicates.") + outbox),
          ("Handle every failure", "<p>No internet, timeout, 401 (login again), 5xx (retry with waiting), permission denied. Never lose what the user typed.</p>"),
          ("Run the checks before pushing", cmd("flutter analyze", "flutter test") + "<p>Fix every warning. Format with <code>dart format .</code></p>"),
          ("Open a pull request and answer review comments", "<p>Describe what and why, link the ticket, attach a screenshot/short video. Reply to each comment: fix it, or explain politely.</p>"),
          ("Demo at the end of the sprint", "<p>Show real working screens on a phone to the PM. Not slides.</p>")]
    dod = checklist(["Matches the Screen Sheet and the design", "Validation + error messages as written", "Works offline where required", "Unit/widget test added and passing", "flutter analyze has no warnings", "Reviewed and approved by someone else", "Tested on a real device", "Ticket updated and demo-able"])
    return stage("s7", 7, "Mobile app development", "build", "Build the screens people will use - one ticket at a time", g,
                 plain("frontend / mobile", "Everything the user sees and touches on the phone. It talks to the backend through the API.") + "<h3>Steps</h3>" + steps(st, "build") + "<h3>Definition of Done (a ticket is finished only when all are ticked)</h3>" + dod +
                 output("All frozen screens working · tests passing · demo recording · merged pull requests · Gate 6 review sign-off") +
                 "<h3>Common mistakes</h3>" + ul(["Building screens that are not in the requirements", "Calling the server directly from the screen", "Testing only on the emulator", "Huge pull requests nobody can review"]))
