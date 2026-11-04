# automation

`ui/` browser tests · `api/` API tests · `page-objects/` locators + actions (no assertions) · `fixtures/` shared helpers · `reports/` generated.
`pytest.ini` (one level up) adds `page-objects` and `fixtures` to the import path. Mark known defects with `@known("DEF-nnn: …")` (strict xfail).
Rules: independent tests, unique synthetic data, no sleeps, no secrets, code review before merge.
